using System;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.World;
using UnityEditor;
using UnityEngine;
using ZenKit;
using Mesh = UnityEngine.Mesh;

namespace GUZ.Core.Creator.Meshes.V2.Builder
{
    public class WorldMeshBuilder : AbstractMeshBuilder
    {
        private WorldData _world;
        private int _meshesPerFrame;

        public void SetWorldData(WorldData world, int meshesPerFrame)
        {
            _world = world;
            _meshesPerFrame = meshesPerFrame;
        }

        public override GameObject Build()
        {
            throw new NotImplementedException("Use BuildAsync instead.");
        }

        public async Task BuildAsync(LoadingManager loading)
        {
            RootGo.isStatic = true;

            // Track the progress of each sub-mesh creation separately
            var numSubMeshes = _world.SubMeshes.Count;
            var meshesCreated = 0;

            foreach (var subMesh in _world.SubMeshes)
            {
                // No texture to add.
                // For G1 this is: material.name == [KEINE, KEINETEXTUREN, DEFAULT, BRETT2, BRETT1, SUMPFWAASER, S:PSIT01_ABODEN]
                // Removing these removes tiny slices of walls on the ground. If anyone finds them, I owe them a beer. ;-)
                if (subMesh.Material.Texture.IsEmpty() || subMesh.Triangles.IsEmpty())
                {
                    continue;
                }

                var subMeshObj = new GameObject
                {
                    name = $"{subMesh.TextureArrayType} world chunk",
                    isStatic = true
                };

                subMeshObj.SetParent(RootGo);

                var meshFilter = subMeshObj.AddComponent<MeshFilter>();
                var meshRenderer = subMeshObj.AddComponent<MeshRenderer>();

                meshRenderer.material = Constants.LoadingMaterial;
                TextureCache.WorldMeshRenderersForTextureArray.Add((meshRenderer, subMesh));
                PrepareMeshFilter(meshFilter, subMesh);
                PrepareMeshCollider(subMeshObj, meshFilter.sharedMesh, subMesh.Material);

#if UNITY_EDITOR // Only needed for Occlusion Culling baking
                // Don't set transparent meshes as occluders.
                if (IsTransparentShader(subMesh))
                {
                    GameObjectUtility.SetStaticEditorFlags(subMeshObj,
                        (StaticEditorFlags)(int.MaxValue & ~(int)StaticEditorFlags.OccluderStatic));
                }
#endif

                loading?.AddProgress(LoadingManager.LoadingProgressType.WorldMesh, 1f / numSubMeshes);

                if (++meshesCreated % _meshesPerFrame == 0)
                {
                    await Task.Yield(); // Yield to allow other operations to run in the frame
                }
            }
        }

        private void PrepareMeshFilter(MeshFilter meshFilter, WorldData.SubMeshData subMesh)
        {
            var mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
            mesh.SetVertices(subMesh.Vertices);
            mesh.SetTriangles(subMesh.Triangles, 0);
            mesh.SetUVs(0, subMesh.Uvs);
            mesh.SetNormals(subMesh.Normals);
            mesh.SetColors(subMesh.BakedLightColors);
            if (subMesh.Material.Group == MaterialGroup.Water)
            {
                mesh.SetUVs(1, subMesh.TextureAnimations);
            }
        }

        private static bool IsTransparentShader(WorldData.SubMeshData subMeshData)
        {
            if (subMeshData == null)
            {
                return false;
            }

            return subMeshData.TextureArrayType != TextureCache.TextureArrayTypes.Opaque;
        }
    }
}
