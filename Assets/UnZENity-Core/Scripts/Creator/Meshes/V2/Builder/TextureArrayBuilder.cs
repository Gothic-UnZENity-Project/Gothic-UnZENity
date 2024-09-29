using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Manager;
using UnityEngine;
using ZenKit;
using Material = UnityEngine.Material;
using Texture = UnityEngine.Texture;

namespace GUZ.Core.Creator.Meshes.V2.Builder
{
    /// <summary>
    /// Create texture array for all meshes. Basically no MeshBuilder,
    /// but we inherit the abstract builder to leverage some methods.
    /// </summary>
    [Obsolete("Use >TextureArrayManager< instead.")]
    public class TextureArrayBuilder : AbstractMeshBuilder
    {
        public override GameObject Build()
        {
            throw new NotImplementedException("Use BuildAsync instead.");
        }

        public async Task BuildAsync()
        {
            TextureCache.RemoveCachedTextureArrayData();
        }

        private void PrepareVobMeshRenderer(Renderer renderer, IMultiResolutionMesh mrmData, List<TextureArrayManager.TextureArrayTypes> textureArrayTypes)
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
                // FIXME - re-enable
                // Material material = GetDefaultMaterial(texture && ((Texture2DArray)texture).format == TextureFormat.RGBA32);
                Material material = null;

                material.mainTexture = texture;
                renderer.material = material;
                finalMaterials.Add(material);
            }

            renderer.SetMaterials(finalMaterials);
        }
    }
}
