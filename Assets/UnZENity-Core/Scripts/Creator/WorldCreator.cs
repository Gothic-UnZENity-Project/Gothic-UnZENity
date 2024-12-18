using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using UnityEngine;
using ZenKit;
using ZenKit.Vobs;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Creator
{
    public static class WorldCreator
    {
        private static GameObject _worldGo;
        private static HashSet<IPolygon> _claimedPolygons;

        static WorldCreator()
        {
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(WorldLoaded);
        }

        public static async Task CreateAsync(DeveloperConfig config, LoadingManager loading, GameObject root)
        {
            _worldGo = root;

            var lightingEnabled = config.EnableVOBs &&
                                  (
                                      config.SpawnVOBTypes.Value.IsEmpty() ||
                                      config.SpawnVOBTypes.Value.Contains(VirtualObjectType.zCVobLight)
                                  );

            GameGlobals.SaveGame.CurrentWorldData.SubMeshes = await BuildBspTree(
                GameGlobals.SaveGame.CurrentWorldData.Mesh,
                GameGlobals.SaveGame.CurrentWorldData.BspTree,
                lightingEnabled);

            await MeshFactory.CreateWorld(GameGlobals.SaveGame.CurrentWorldData, loading, _worldGo);
            await MeshFactory.CreateTextureArray();
        }

        public static async Task<List<WorldContainer.SubMeshData>> BuildBspTree(IMesh zkMesh, IBspTree zkBspTree, bool lightingEnabled)
        {
            _claimedPolygons = new HashSet<IPolygon>();
            Dictionary<int, List<WorldContainer.SubMeshData>> subMeshesPerParentNode = new();

            Stopwatch stopwatch = new();
            stopwatch.Start();
            await ExpandBspTreeIntoMeshes(zkMesh, zkBspTree, 0, subMeshesPerParentNode, null);
            stopwatch.Stop();
            Debug.Log($"Expanding tree: {stopwatch.ElapsedMilliseconds / 1000f} s");

            // Free memory
            _claimedPolygons = null;

            stopwatch.Restart();
            // Merge the world meshes until they touch the max amount of lights per mesh.
            var mergedSubMeshesPerParentNode = subMeshesPerParentNode;
            while (true)
            {
                mergedSubMeshesPerParentNode = MergeWorldChunksByLightCount(zkBspTree, subMeshesPerParentNode, lightingEnabled);
                if (mergedSubMeshesPerParentNode.Count == subMeshesPerParentNode.Count)
                {
                    break;
                }

                subMeshesPerParentNode = mergedSubMeshesPerParentNode;
                await Task.Yield();
            }

            stopwatch.Stop();
            Debug.Log($"Merging by lights: {stopwatch.ElapsedMilliseconds / 1000f} s");

            stopwatch.Restart();
            // Merge the water until a given level in the BSP tree to it a few large chunks.
            subMeshesPerParentNode = MergeShaderTypeWorldChunksToTreeHeight(TextureCache.TextureArrayTypes.Water, 3, zkBspTree, subMeshesPerParentNode);
            stopwatch.Stop();
            Debug.Log($"Merging water: {stopwatch.ElapsedMilliseconds / 1000f} s");

            // To have easier to read code above, we reverse the arrays now at the end.
            foreach (var subMeshes in subMeshesPerParentNode.Values)
            {
                foreach (var subMesh in subMeshes)
                {
                    subMesh.Vertices.Reverse();
                    subMesh.Uvs.Reverse();
                    subMesh.Normals.Reverse();
                    subMesh.BakedLightColors.Reverse();
                    subMesh.TextureAnimations.Reverse();
                }
            }

            return subMeshesPerParentNode.Values.SelectMany(s => s).ToList();
        }

        private static void CalculateTreeHeightDescending(IBspTree tree, int nodeIndex, ref int maxHeight)
        {
            var height = 1;
            var parent = tree.Nodes[nodeIndex].ParentIndex;
            while (parent != -1)
            {
                parent = tree.Nodes[parent].ParentIndex;
                height++;
            }

            maxHeight = Mathf.Max(height, maxHeight);

            if (tree.Nodes[nodeIndex].FrontIndex != -1)
            {
                CalculateTreeHeightDescending(tree, tree.Nodes[nodeIndex].FrontIndex, ref maxHeight);
            }

            if (tree.Nodes[nodeIndex].BackIndex != -1)
            {
                CalculateTreeHeightDescending(tree, tree.Nodes[nodeIndex].BackIndex, ref maxHeight);
            }
        }

        private static int CalculateTreeHeightAscending(IBspTree tree, int nodeIndex)
        {
            var remainingTreeHeight = 0;
            var parent = tree.GetNode(nodeIndex).ParentIndex;
            while (parent != -1)
            {
                remainingTreeHeight++;
                parent = tree.GetNode(parent).ParentIndex;
            }

            return remainingTreeHeight;
        }

        /// <summary>
        /// This method recursively walks all the nodes in the bsp tree. It builds a single mesh for each node containing geometry. The bsp tree contains multiple levels of detail and the final geometry under the leaf nodes. 
        /// The meshes are the leaf geometry combined from the level that contains the first LOD, creating the largest coherent chunks possible. The larger chunks get culled less, but are more performant to render. 
        /// </summary>
        /// <returns></returns>
        private static async Task ExpandBspTreeIntoMeshes(IMesh zkMesh, IBspTree bspTree, int nodeIndex,
            Dictionary<int, List<WorldContainer.SubMeshData>> allSubmeshesPerParentNodeIndex,
            Dictionary<Shader, WorldContainer.SubMeshData> nodeSubmeshes, int submeshParentIndex = 0)
        {
            await Task.Run(async () =>
            {
                BspNode node = bspTree.GetNode(nodeIndex);

                if (node.PolygonCount > 0)
                {
                    // First node containing geometry. Start a new mesh collection. Meshes will be built for each shader in the node.
                    // We do not want to create nodeSubmeshes at the root node even if it contains polygons as it would restrict
                    // the leaves of the tree to create chunks with the same shader as the root node.
                    if (nodeSubmeshes == null && node.ParentIndex != -1)
                    {
                        nodeSubmeshes = new Dictionary<Shader, WorldContainer.SubMeshData>();
                        submeshParentIndex = node.ParentIndex;
                    }

                    if (node.FrontIndex == -1 && node.BackIndex == -1)
                    {
                        // Add the leaf node geometry.
                        for (int i = node.PolygonIndex; i < node.PolygonIndex + node.PolygonCount; i++)
                        {
                            IPolygon polygon = zkMesh.IsCached() ? zkMesh.Polygons[bspTree.PolygonIndices[i]] : zkMesh.GetPolygon(bspTree.PolygonIndices[i]);
                            if (polygon.IsPortal || _claimedPolygons.Contains(polygon))
                            {
                                continue;
                            }

                            // Different leaf nodes reference the same polygons. Manually check if polygons have been used to avoid overlapping geometry.
                            _claimedPolygons.Add(polygon);

                            // As we always use element 0 and i+1, we skip it in the loop.
                            for (int p = 1; p < polygon.PositionIndices.Count - 1; p++)
                            {
                                // Add the texture to the texture array or retrieve its existing slice.
                                IMaterial zkMaterial = zkMesh.IsCached() ? zkMesh.Materials[polygon.MaterialIndex] : zkMesh.GetMaterial(polygon.MaterialIndex);
                                TextureCache.GetTextureArrayIndex(zkMaterial, out TextureCache.TextureArrayTypes textureArrayType, out int textureArrayIndex, out Vector2 textureScale, out int maxMipLevel, out int animFrameCount);
                                if (textureArrayIndex == -1)
                                {
                                    continue;
                                }

                                // Build submeshes for each unique shader: Water, opaque, and alpha cutout.
                                Shader shader = Constants.ShaderWorldLit;
                                if (textureArrayType == TextureCache.TextureArrayTypes.Transparent)
                                {
                                    shader = Constants.ShaderLitAlphaToCoverage;
                                }
                                else if (textureArrayType == TextureCache.TextureArrayTypes.Water)
                                {
                                    shader = Constants.ShaderWater;
                                }

                                if (!nodeSubmeshes.ContainsKey(shader))
                                {
                                    nodeSubmeshes.Add(shader, new WorldContainer.SubMeshData { Material = zkMaterial, TextureArrayType = textureArrayType });
                                    if (!allSubmeshesPerParentNodeIndex.ContainsKey(submeshParentIndex))
                                    {
                                        allSubmeshesPerParentNodeIndex.Add(submeshParentIndex, new List<WorldContainer.SubMeshData>());
                                    }

                                    allSubmeshesPerParentNodeIndex[submeshParentIndex].Add(nodeSubmeshes[shader]);
                                }

                                WorldContainer.SubMeshData nodeSubmesh = nodeSubmeshes[shader];
                                // Triangle Fan - We need to add element 0 (A) before every triangle 2 elements.
                                AddEntry(zkMesh, polygon, zkMaterial, nodeSubmesh, 0, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                                AddEntry(zkMesh, polygon, zkMaterial, nodeSubmesh, p, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                                AddEntry(zkMesh, polygon, zkMaterial, nodeSubmesh, p + 1, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                            }
                        }
                    }
                }

                // Expand the child nodes. Spawn new threads if no geometry is added yet.
                if (node.FrontIndex != -1)
                {
                    await ExpandBspTreeIntoMeshes(zkMesh, bspTree, node.FrontIndex, allSubmeshesPerParentNodeIndex, nodeSubmeshes, submeshParentIndex);
                }

                if (node.BackIndex != -1)
                {
                    await ExpandBspTreeIntoMeshes(zkMesh, bspTree, node.BackIndex, allSubmeshesPerParentNodeIndex, nodeSubmeshes, submeshParentIndex);
                }
            });
        }

        private static void AddEntry(IMesh zkMesh, IPolygon polygon, IMaterial material, WorldContainer.SubMeshData currentSubMesh, int index,
            int textureArrayIndex, Vector2 scaleInTextureArray, int maxMipLevel = 16, int animFrameCount = 0)
        {
            // For every vertexIndex we store a new vertex. (i.e. no reuse of Vector3-vertices for later texture/uv attachment)
            int positionIndex = polygon.PositionIndices[index];
            currentSubMesh.Vertices.Add((zkMesh.IsCached() ? zkMesh.Positions[positionIndex] : zkMesh.GetPosition(positionIndex)).ToUnityVector());

            // This triangle (index where Vector 3 lies inside vertices, points to the newly added vertex (Vector3) as we don't reuse vertices.
            currentSubMesh.Triangles.Add(currentSubMesh.Vertices.Count - 1);

            int featureIndex = polygon.FeatureIndices[index];
            Vertex feature = zkMesh.IsCached() ? zkMesh.Features[featureIndex] : zkMesh.GetFeature(featureIndex);
            Vector2 uv = Vector2.Scale(scaleInTextureArray, feature.Texture.ToUnityVector());
            currentSubMesh.Uvs.Add(new Vector4(uv.x, uv.y, textureArrayIndex, maxMipLevel));
            currentSubMesh.Normals.Add(feature.Normal.ToUnityVector());
            currentSubMesh.BakedLightColors.Add(new Color32((byte)(feature.Light >> 16), (byte)(feature.Light >> 8), (byte)feature.Light, (byte)(feature.Light >> 24)));

            if (material.TextureAnimationMapping == AnimationMapping.Linear)
            {
                Vector2 uvAnimation = material.TextureAnimationMappingDirection.ToUnityVector();
                currentSubMesh.TextureAnimations.Add(new Vector4(uvAnimation.x, uvAnimation.y, animFrameCount, material.TextureAnimationFps));
            }
            else
            {
                currentSubMesh.TextureAnimations.Add(new Vector4(0, 0, animFrameCount, material.TextureAnimationFps));
            }
        }

        private static Dictionary<int, List<WorldContainer.SubMeshData>> MergeShaderTypeWorldChunksToTreeHeight(
            TextureCache.TextureArrayTypes textureArrayType, int treeHeightLimit, IBspTree bspTree,
            Dictionary<int, List<WorldContainer.SubMeshData>> submeshesPerParentNode)
        {
            // Group the submeshes by parent nodes until max height.
            var groupedMeshes = new Dictionary<int, List<WorldContainer.SubMeshData>>();

            foreach (var parentNodeIndex in submeshesPerParentNode.Keys)
            {
                var remainingHeight = CalculateTreeHeightAscending(bspTree, parentNodeIndex);
                var topParentIndex = parentNodeIndex;
                for (var i = 0; i < Mathf.Max(0, remainingHeight - treeHeightLimit); i++)
                {
                    topParentIndex = bspTree.GetNode(topParentIndex).ParentIndex;
                }

                if (!groupedMeshes.ContainsKey(topParentIndex))
                {
                    groupedMeshes.Add(topParentIndex, new List<WorldContainer.SubMeshData>());
                }

                groupedMeshes[topParentIndex].AddRange(submeshesPerParentNode[parentNodeIndex]
                    .Where(s => s.TextureArrayType == textureArrayType));
            }

            var mergedMeshes = new Dictionary<int, List<WorldContainer.SubMeshData>>();

            // Merge the grouped meshes.
            foreach (var topParentIndex in groupedMeshes.Keys)
            {
                if (groupedMeshes[topParentIndex].Count < 2)
                {
                    continue;
                }

                for (var i = 1; i < groupedMeshes[topParentIndex].Count; i++)
                {
                    var vertexCount = groupedMeshes[topParentIndex][0].Vertices.Count;
                    groupedMeshes[topParentIndex][0].Vertices.AddRange(groupedMeshes[topParentIndex][i].Vertices);
                    groupedMeshes[topParentIndex][0].Triangles
                        .AddRange(groupedMeshes[topParentIndex][i].Triangles.Select(v => v += vertexCount));
                    groupedMeshes[topParentIndex][0].Uvs.AddRange(groupedMeshes[topParentIndex][i].Uvs);
                    groupedMeshes[topParentIndex][0].Normals.AddRange(groupedMeshes[topParentIndex][i].Normals);
                    groupedMeshes[topParentIndex][0].BakedLightColors
                        .AddRange(groupedMeshes[topParentIndex][i].BakedLightColors);
                    groupedMeshes[topParentIndex][0].TextureAnimations
                        .AddRange(groupedMeshes[topParentIndex][i].TextureAnimations);
                }

                mergedMeshes.Add(topParentIndex, new List<WorldContainer.SubMeshData> { groupedMeshes[topParentIndex][0] });
            }

            // Add the meshes from the other shader types.
            foreach (var parentNodeIndex in submeshesPerParentNode.Keys)
            {
                if (!mergedMeshes.ContainsKey(parentNodeIndex))
                {
                    mergedMeshes.Add(parentNodeIndex,
                        submeshesPerParentNode[parentNodeIndex].Where(s => s.TextureArrayType != textureArrayType)
                            .ToList());
                }
                else
                {
                    mergedMeshes[parentNodeIndex].AddRange(submeshesPerParentNode[parentNodeIndex]
                        .Where(s => s.TextureArrayType != textureArrayType));
                }
            }

            return mergedMeshes;
        }

        private static Dictionary<int, List<WorldContainer.SubMeshData>> MergeWorldChunksByLightCount(IBspTree bspTree,
            Dictionary<int, List<WorldContainer.SubMeshData>> submeshesPerParentNode, bool lightingEnabled)
        {
            var maxLightsPerChunk = 16;

            // Workaround - if we have no lights spawned, then the merging algorithm has some issues.
            // But as this will only happen with Developer settings, we fix it here.
            if (!lightingEnabled)
            {
                maxLightsPerChunk = 0;
            }

            Dictionary<int, List<WorldContainer.SubMeshData>> mergedChunks = new();

            Parallel.ForEach(submeshesPerParentNode.Keys, parentNodeIndex =>
            {
                var grandParentNodeIndex = bspTree.GetNode(parentNodeIndex).ParentIndex;
                var intersectingLights =
                    StationaryLight.CountLightsInBounds(bspTree.GetNode(parentNodeIndex).BoundingBox.ToUnityBounds());
                // Merge the two halves of the parent node if the max light count is not exceeded.
                if (intersectingLights < maxLightsPerChunk && grandParentNodeIndex != -1)
                {
                    // Merge all shader types under the parent node.
                    foreach (TextureCache.TextureArrayTypes textureArrayType in Enum.GetValues(
                                 typeof(TextureCache.TextureArrayTypes)))
                    {
                        var meshes = submeshesPerParentNode[parentNodeIndex]
                            .Where(s => s.TextureArrayType == textureArrayType);
                        if (meshes.Count() == 0)
                        {
                            continue;
                        }

                        if (meshes.Count() == 1)
                        {
                            // Only one node under this parent. Carry it over so it can be merged in the next pass.
                            lock (mergedChunks)
                            {
                                if (!mergedChunks.ContainsKey(parentNodeIndex))
                                {
                                    mergedChunks.Add(parentNodeIndex, new List<WorldContainer.SubMeshData>());
                                }

                                mergedChunks[parentNodeIndex].Add(meshes.First());
                            }
                        }
                        else
                        {
                            // Merge the two nodes.
                            var vertexCount = meshes.First().Vertices.Count;
                            meshes.First().Vertices.AddRange(meshes.Last().Vertices);
                            meshes.First().Triangles.AddRange(meshes.Last().Triangles.Select(v => v += vertexCount));
                            meshes.First().Uvs.AddRange(meshes.Last().Uvs);
                            meshes.First().Normals.AddRange(meshes.Last().Normals);
                            meshes.First().BakedLightColors.AddRange(meshes.Last().BakedLightColors);
                            meshes.First().TextureAnimations.AddRange(meshes.Last().TextureAnimations);

                            lock (mergedChunks)
                            {
                                if (!mergedChunks.ContainsKey(grandParentNodeIndex))
                                {
                                    mergedChunks.Add(grandParentNodeIndex, new List<WorldContainer.SubMeshData>());
                                }

                                mergedChunks[grandParentNodeIndex].Add(meshes.First());
                            }
                        }
                    }
                }
                else
                {
                    lock (mergedChunks)
                    {
                        // Max light count would be exceeded. Keep the same nodes.
                        if (!mergedChunks.ContainsKey(parentNodeIndex))
                        {
                            mergedChunks.Add(parentNodeIndex, submeshesPerParentNode[parentNodeIndex]);
                        }
                        else
                        {
                            mergedChunks[parentNodeIndex].AddRange(submeshesPerParentNode[parentNodeIndex]);
                        }
                    }
                }
            });

            return mergedChunks;
        }

        private static void WorldLoaded()
        {
            GameContext.InteractionAdapter.SetTeleportationArea(_worldGo);
        }


#if UNITY_EDITOR
        /// <summary>
        /// Loads the world for occlusion culling.
        /// </summary>
        public static async void LoadEditorWorld()
        {
            throw new Exception(
                "World scenes and their OC data are now fetched from a package. We need to rework this lookup before using it.");

            // Scene worldScene = SceneManager.GetActiveScene();
            // if (Path.GetDirectoryName(worldScene.path) != "Assets\\Gothic-UnZENity-Core\\Scenes\\Worlds")
            // {
            //     Debug.LogWarning($"Open a world scene, from Assets/Gothic-UnZENity-Core/Scenes/Worlds.");
            //     return;
            // }
            //
            // LoadWorld(worldScene.name);
            //
            // await MeshFactory.CreateWorld(GameData.World, new GameObject("World"), Constants.MeshPerFrame);
        }
#endif
    }
}
