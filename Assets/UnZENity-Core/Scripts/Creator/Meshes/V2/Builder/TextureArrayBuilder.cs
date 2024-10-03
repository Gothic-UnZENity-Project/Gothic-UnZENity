using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;
using TextureFormat = UnityEngine.TextureFormat;

namespace GUZ.Core.Creator.Meshes.V2.Builder
{
    /// <summary>
    /// Create texture array for all meshes. Basically no MeshBuilder,
    /// but we inherit the abstract builder to leverage some methods.
    /// </summary>
    public class TextureArrayBuilder : AbstractMeshBuilder
    {
        public override GameObject Build()
        {
            throw new NotImplementedException("Use BuildAsync instead.");
        }

        public async Task BuildAsync()
        {
            await TextureCache.BuildTextureArrays();
            AssignTextureArrays();
            TextureCache.RemoveCachedTextureArrayData();
        }

        private void AssignTextureArrays()
        {
            foreach (var rendererData in TextureCache.WorldMeshRenderersForTextureArray)
            {
                PrepareWorldMeshRenderer(rendererData.Renderer, rendererData.SubmeshData);
            }

            foreach (TextureCache.VobMeshData meshData in TextureCache.VobMeshesForTextureArray.Values)
            {
                foreach (Renderer renderer in meshData.Renderers)
                {
                    PrepareVobMeshRenderer(renderer, meshData.Mrm, meshData.TextureArrayTypes);
                }
            }
        }

        private void PrepareWorldMeshRenderer(Renderer rend, WorldData.SubMeshData subMesh)
        {
            Texture texture = TextureCache.TextureArrays[subMesh.TextureArrayType];
            Material material;
            if (subMesh.Material.Group == MaterialGroup.Water)
            {
                material = GetWaterMaterial();
            }
            else
            {
                material = GetDefaultMaterial(subMesh.TextureArrayType == TextureCache.TextureArrayTypes.Transparent);
            }

            material.mainTexture = texture;
            rend.material = material;
        }

        private void PrepareVobMeshRenderer(Renderer renderer, IMultiResolutionMesh mrmData, List<TextureCache.TextureArrayTypes> textureArrayTypes)
        {
            if (mrmData == null)
            {
                Debug.LogError("No mesh data could be added to renderer: " + renderer.transform.parent.name);
                return;
            }

            if (renderer is MeshRenderer && !renderer.GetComponent<MeshFilter>().sharedMesh)
            {
                Debug.LogError($"Null mesh on {renderer.gameObject.name}");
                return;
            }

            List<Material> finalMaterials = new List<Material>(mrmData.SubMeshes.Count);
            int submeshCount = renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount;

            for (int i = 0; i < submeshCount; i++)
            {
                Texture texture = TextureCache.TextureArrays[textureArrayTypes[i]];
                Material material = GetDefaultMaterial(texture && ((Texture2DArray)texture).format == TextureFormat.RGBA32);

                material.mainTexture = texture;
                renderer.material = material;
                finalMaterials.Add(material);
            }

            renderer.SetMaterials(finalMaterials);
        }

        protected override Material GetDefaultMaterial(bool isAlphaTest)
        {
            Shader shader = isAlphaTest ? Constants.ShaderLitAlphaToCoverage : Constants.ShaderWorldLit;
            Material material = new Material(shader);

            if (isAlphaTest)
            {
                // Manually correct the render queue for alpha test, as Unity doesn't want to do it from the shader's render queue tag.
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }

            return material;
        }
    }
}
