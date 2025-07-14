using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.UI.Menus.LoadingBars;
using GUZ.Core.Util;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit;
using Mesh = UnityEngine.Mesh;
using Material = UnityEngine.Material;

namespace GUZ.Core.Creator.Meshes.Builder
{
    public class WorldMeshBuilder : AbstractMeshBuilder
    {
        private StaticCacheManager.WorldChunkContainer _worldChunks;
        private IMesh _mesh;
        private bool _debugSpeedUpLoading;

        public class ChunkData
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<int> Triangles = new();
            public readonly List<Vector4> Uvs = new();
            public readonly List<Vector3> Normals = new();
            public readonly List<Color32> BakedLightColors = new();
            public readonly List<Vector4> TextureAnimations = new();
        }

        public void SetWorldData(StaticCacheManager.WorldChunkContainer worldChunks, IMesh mesh)
        {
            _worldChunks = worldChunks;
            _mesh = mesh;
        }

        public override GameObject Build()
        {
            throw new NotImplementedException("Use BuildAsync instead.");
        }

        public async Task BuildAsync([CanBeNull] LoadingManager loading)
        {
            _debugSpeedUpLoading = GameGlobals.Config.Dev.SpeedUpLoading;

            RootGo.isStatic = true;

            var chunksCount = _worldChunks.OpaqueChunks.Count + _worldChunks.TransparentChunks.Count + _worldChunks.WaterChunks.Count;
            
            loading?.SetPhase(nameof(WorldLoadingBarHandler.ProgressType.WorldMesh), chunksCount);

            await BuildChunkType(_worldChunks.OpaqueChunks, TextureCache.TextureArrayTypes.Opaque, loading);
            await BuildChunkType(_worldChunks.TransparentChunks, TextureCache.TextureArrayTypes.Transparent, loading);
            await BuildChunkType(_worldChunks.WaterChunks, TextureCache.TextureArrayTypes.Water, loading);
        }

        private async Task BuildChunkType(List<WorldChunkCacheCreator.WorldChunk> chunks, TextureCache.TextureArrayTypes type, [CanBeNull] LoadingManager loading)
        {
            var chunkTypeRoot = new GameObject
            {
                name = type.ToString(),
                isStatic = true
            };
            chunkTypeRoot.SetParent(RootGo);

            var loopIndex = 0;
            foreach (var chunk in chunks)
            {
                var chunkGo = new GameObject
                {
                    name = $"{type}-Entry-{loopIndex++}",
                    isStatic = true,
                    layer = type == TextureCache.TextureArrayTypes.Water ? Constants.WaterLayer : Constants.DefaultLayer,
                };
                chunkGo.SetParent(chunkTypeRoot);

                var chunkData = new ChunkData();
                foreach (var polygonId in chunk.PolygonIds)
                {
                    var polygon = _mesh.GetPolygon(polygonId);
                    var material = _mesh.GetMaterial(polygon.MaterialIndex);

                    // Defaults which will be used, if we don't use TextureArray (e.g. for OC baking and with debug textures only)
                    int textureArrayIndex = -1;
                    Vector2 textureScale = Vector2.one;
                    int maxMipLevel = 0;
                    int animFrameCount = -1;

                    if (UseTextureArray)
                    {
                        TextureCache.GetTextureArrayIndex(material, out _, out textureArrayIndex,
                            out textureScale, out maxMipLevel, out animFrameCount);
                    }

                    // As we always use element 0 and i+1, we skip it in the loop.
                    // Positions are a triangle fan. i.e. every position after 0 leads back to position 0.
                    for (var p = 1; p < polygon.PositionIndices.Count - 1; p++)
                    {
                        AddPolygonChunkEntry(polygon, chunkData, material, 0, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                        AddPolygonChunkEntry(polygon, chunkData, material, p, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                        AddPolygonChunkEntry(polygon, chunkData, material, p+1, textureArrayIndex, textureScale, maxMipLevel, animFrameCount);
                    }

                    if (!_debugSpeedUpLoading)
                    {
                        // If we have the skips here, we have a smoother loading screen for 20 seconds on loading world. Putting it at the end of each chunk, we have stutter, but save about 40%.
                        await FrameSkipper.TrySkipToNextFrame();
                    }
                }

                var meshFilter = chunkGo.AddComponent<MeshFilter>();
                var meshRenderer = chunkGo.AddComponent<MeshRenderer>();

                PrepareMeshFilter(meshFilter, chunkData, type);
                PrepareMeshRenderer(meshRenderer, type);
                PrepareMeshCollider(chunkGo, meshFilter.sharedMesh);


#if UNITY_EDITOR
                // Only needed for Occlusion Culling baking
                // Don't set transparent meshes as occluders.
                if (IsTransparentShader(type))
                {
                    GameObjectUtility.SetStaticEditorFlags(chunkGo,
                        (StaticEditorFlags)(int.MaxValue & ~(int)StaticEditorFlags.OccluderStatic));
                }
#endif


                loading?.Tick();
            }
        }

        private void AddPolygonChunkEntry(IPolygon polygon, ChunkData chunkData, IMaterial material, int index,
            int textureArrayIndex, Vector2 scaleInTextureArray, int maxMipLevel = 16, int animFrameCount = 0)
        {
            // For every vertexIndex we store a new vertex. (i.e. no reuse of Vector3-vertices for later texture/uv attachment)
            var positionIndex = polygon.PositionIndices[index];
            chunkData.Vertices.Add(_mesh.GetPosition(positionIndex).ToUnityVector());

            // This triangle (index where Vector 3 lies inside vertices, points to the newly added vertex (Vector3) as we don't reuse vertices.
            chunkData.Triangles.Add(chunkData.Vertices.Count - 1);

            var featureIndex = polygon.FeatureIndices[index];
            var feature = _mesh.GetFeature(featureIndex);
            var uv = Vector2.Scale(scaleInTextureArray, feature.Texture.ToUnityVector());
            chunkData.Uvs.Add(new Vector4(uv.x, uv.y, textureArrayIndex, maxMipLevel));
            chunkData.Normals.Add(feature.Normal.ToUnityVector());
            chunkData.BakedLightColors.Add(new Color32((byte)(feature.Light >> 16), (byte)(feature.Light >> 8), (byte)feature.Light, (byte)(feature.Light >> 24)));

            // HINT: We set animFrameCount + 1 as internally, Water.shader is leveraging a % animFrameCountValue.
            // No animation -> % 1 is always 0, which means there is always texture 0 used.
            // 1 Animation -> % 2 (animFrameCount(1) + 1 = 2) - is switching between both values.
            if (material.TextureAnimationMapping == AnimationMapping.Linear)
            {
                var uvAnimation = material.TextureAnimationMappingDirection.ToUnityVector();
                chunkData.TextureAnimations.Add(new Vector4(uvAnimation.x, uvAnimation.y, animFrameCount + 1, material.TextureAnimationFps));
            }
            else
            {
                chunkData.TextureAnimations.Add(new Vector4(0, 0, animFrameCount + 1, material.TextureAnimationFps));
            }
        }

        private void PrepareMeshFilter(MeshFilter meshFilter, ChunkData chunk, TextureCache.TextureArrayTypes textureArrayType)
        {
            // We need to reverse all data. Otherwise, meshes are visible upside down. It's a difference from rendering ZenGine data in Unity.
            // Hint: No, Triangles mustn't be reversed. Only applied data on it.
            chunk.BakedLightColors.Reverse();
            chunk.Normals.Reverse();
            chunk.TextureAnimations.Reverse();
            chunk.Uvs.Reverse();
            chunk.Vertices.Reverse();

            var mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
            mesh.SetVertices(chunk.Vertices);
            mesh.SetTriangles(chunk.Triangles, 0);
            mesh.SetUVs(0, chunk.Uvs);
            mesh.SetNormals(chunk.Normals);
            mesh.SetColors(chunk.BakedLightColors);

            if (textureArrayType == TextureCache.TextureArrayTypes.Water)
            {
                mesh.SetUVs(1, chunk.TextureAnimations);
            }
        }

        private void PrepareMeshRenderer(Renderer rend, TextureCache.TextureArrayTypes textureArrayType)
        {


            if (UseTextureArray)
            {
                var material = GetDefaultMaterial(textureArrayType);
                rend.material = material;
                var texture = TextureCache.GetTextureArrayEntry(textureArrayType);
                material.mainTexture = texture;
            }
            else
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                rend.material = material;
                // No TextureArray is only needed for Occlusion Culling in Editor mode and other dev tools. A dev texture is sufficient.
                material.mainTexture = Constants.TextureUnZENityLogoInverse;
            }
        }

        private Material GetDefaultMaterial(TextureCache.TextureArrayTypes textureArrayType)
        {
            var shader = textureArrayType switch
            {
                TextureCache.TextureArrayTypes.Opaque => Constants.ShaderWorldLit,
                TextureCache.TextureArrayTypes.Transparent => Constants.ShaderLitAlphaToCoverage,
                TextureCache.TextureArrayTypes.Water => Constants.ShaderWater,
                _ => throw new ArgumentOutOfRangeException(nameof(textureArrayType), textureArrayType, null)
            };
            var material = new Material(shader);

            if (textureArrayType == TextureCache.TextureArrayTypes.Water)
            {
                // Manually correct the render queue for alpha test, as Unity doesn't want to do it from the shader's render queue tag.
                // If we don't set it, the water will sometimes "flicker" above ground or becomes invisible.
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }

        private bool IsTransparentShader(TextureCache.TextureArrayTypes textureArrayType)
        {
            return textureArrayType != TextureCache.TextureArrayTypes.Opaque;
        }
    }
}
