using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit;
using Debug = UnityEngine.Debug;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Object = UnityEngine.Object;
using Texture = UnityEngine.Texture;
using TextureFormat = UnityEngine.TextureFormat;

namespace GUZ.Core.Manager
{

    /// <summary>
    /// Texture Arrays are used for the following improvements:
    /// 1. World mesh chunks are merged into sliced with specific bound. Without the texture array, we would need to separate each small floor mesh if it has a different texture.
    /// 2. Static VOBs will merge multiple textures into one. This reduces draw calls. (e.g. various complex VOBs have multiple textures).
    ///
    /// Once world is loaded from cache and texture array data is applied, the texture cache is released to free memory of our calculated data.
    /// Only the created texture array itself remains in memory.
    ///
    /// Not in Texture Array:
    /// 0. Basically everything which is created dynamically at runtime:
    /// 1. NPCs and their armors (as they alter their armaments during runtime)
    /// 2. VOB Items which spawn at a later state (e.g. Player puts an item out of inventory)
    /// </summary>
    public class TextureArrayManager
    {
        private const int _referenceTextureSize = 256;
        private const int _maxTextureSize = 512;

        public readonly Dictionary<TextureArrayTypes, List<(string PreparedKey, TextureData TextureData)>> TexturesToIncludeInArray = new();
        public readonly List<(MeshRenderer Renderer, WorldData.SubMeshData SubmeshData)> WorldMeshRenderersForTextureArray = new();
        public readonly Dictionary<Mesh, VobMeshData> VobMeshesForTextureArray = new();

        /// <summary>
        /// Created Texture Arrays via BuildTextureArrays(). Will be applied immediately via AssignTextureArraysFor*() and released afterwards.
        /// </summary>
        private static Dictionary<TextureArrayTypes, Texture> _tempTextureArrays { get; } = new();


        public enum TextureArrayTypes
        {
            Opaque,
            Transparent,
            Water
        }

        [Serializable]
        public class TextureData
        {
            public string Key;
            public int Width;
            public int Height;
            public int MipmapCount;

            [NonSerialized] // Not needed inside glTF cache (excluded to save storage space)
            public Vector2 Scale;

            public TextureData(string key, ITexture zkTexture)
            {
                Key = key;
                MipmapCount = zkTexture.MipmapCount;
                Width = zkTexture.Width;
                Height = zkTexture.Height;
                Scale = new Vector2((float)zkTexture.Width / _referenceTextureSize, (float)zkTexture.Height / _referenceTextureSize);
            }
        }

        public class VobMeshData
        {
            public IMultiResolutionMesh Mrm { get; set; }
            public List<TextureArrayTypes> TextureTypes { get; set; }
            public List<Renderer> Renderers { get; set; } = new();

            public VobMeshData(IMultiResolutionMesh mrm, List<TextureArrayTypes> textureTypes, Renderer renderer = null)
            {
                Mrm = mrm;
                TextureTypes = textureTypes;
                if (renderer != null)
                {
                    Renderers.Add(renderer);
                }
            }
        }

        public void Init()
        {
            // Nothing to do for now. Can be reused later.
        }

        public void GetTextureArrayIndex(IMaterial materialData, out TextureArrayTypes textureArrayType, out int arrayIndex, out Vector2 textureScale, out int maxMipLevel)
        {
            string key = materialData.Texture;

            for (int i = 0; i < Enum.GetValues(typeof(TextureArrayTypes)).Length; i++)
            {
                if (!TexturesToIncludeInArray.ContainsKey((TextureArrayTypes)i))
                {
                    continue;
                }

                (string, TextureData) cachedTextureData = TexturesToIncludeInArray[(TextureArrayTypes)i].FirstOrDefault(k => k.PreparedKey == key);
                if (cachedTextureData != default)
                {
                    textureArrayType = (TextureArrayTypes)i;

                    arrayIndex = TexturesToIncludeInArray[textureArrayType].IndexOf(cachedTextureData);
                    TextureData data = TexturesToIncludeInArray[textureArrayType][arrayIndex].TextureData;
                    maxMipLevel = data.MipmapCount - 1;
                    textureScale = data.Scale;
                    return;
                }
            }

            ITexture zenTextureData = ResourceLoader.TryGetTexture(key);

            if (zenTextureData == null)
            {
                textureArrayType = default;
                arrayIndex = -1;
                textureScale = Vector2.zero;
                maxMipLevel = 0;
                return;
            }

            maxMipLevel = zenTextureData.MipmapCount - 1;
            TextureFormat textureFormat = zenTextureData.Format.AsUnityTextureFormat();

            if (materialData.Group == MaterialGroup.Water)
            {
                textureArrayType = TextureArrayTypes.Water;
            }
            else
            {
                textureArrayType = textureFormat == TextureFormat.DXT1
                    ? TextureArrayTypes.Opaque
                    : TextureArrayTypes.Transparent;
            }

            textureScale = new Vector2((float)zenTextureData.Width / _referenceTextureSize, (float)zenTextureData.Height / _referenceTextureSize);
            if (!TexturesToIncludeInArray.ContainsKey(textureArrayType))
            {
                TexturesToIncludeInArray.Add(textureArrayType, new List<(string, TextureData)>());
            }

            TexturesToIncludeInArray[textureArrayType].Add((key, new TextureData(key, zenTextureData)));
            arrayIndex = TexturesToIncludeInArray[textureArrayType].Count - 1;
        }

        public async Task BuildTextureArraysFromCache(StaticCacheManager.TextureArrayContainer data)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            try
            {
                foreach (var entry in data.TextureTypeEntries)
                {
                    var textureArrayType = entry.TextureType;
                    var textures = entry.Textures;

                    if (Application.isEditor)
                    {
                        for (int i = 16; i <= 2048; i *= 2)
                        {
                            int resCount = textures.Where(t => Mathf.Max(t.Width, t.Height) == i).Count();
                            Debug.Log($"[{nameof(TextureArrayManager)}] {textureArrayType}: {resCount} textures ({Mathf.RoundToInt(100 * (float)resCount / textures.Count)}%) with dimension {i}.");
                        }
                    }

                    // Create the texture array with the max size of the contained textures, limited by the max texture size.
                    int maxSize = Mathf.Min(_maxTextureSize, textures.Max(p => p.Width));
                    TextureData textureWithMaxAllowedSize = textures.Where(t => t.Width == maxSize && t.Height == maxSize).FirstOrDefault();
                    // Find the max mip depth defined for the max allowed texture size.
                    int maxMipDepth = 0;
                    if (textureWithMaxAllowedSize == null)
                    {
                        Debug.LogError($"[{nameof(TextureCache)}] No texture with size {maxSize}x{maxSize} was found to determine max allowed mip level. Falling back to texture with highest mip count.");
                        maxMipDepth = textures.Max(t => t.MipmapCount);
                    }
                    else
                    {
                        maxMipDepth = textureWithMaxAllowedSize.MipmapCount;
                    }

                    TextureFormat textureFormat = TextureFormat.RGBA32;
                    if (textureArrayType == TextureArrayTypes.Opaque)
                    {
                        textureFormat = TextureFormat.DXT1;
                    }

                    Texture texArray;
                    if (textureArrayType != TextureArrayTypes.Water)
                    {
                        texArray = new Texture2DArray(maxSize, maxSize, textures.Count, textureFormat, maxMipDepth, false, true)
                        {
                            filterMode = FilterMode.Trilinear,
                            wrapMode = TextureWrapMode.Repeat
                        };
                    }
                    else
                    {
                        texArray = new RenderTexture(maxSize, maxSize, 0, RenderTextureFormat.ARGB32, maxMipDepth)
                        {
                            dimension = TextureDimension.Tex2DArray,
                            autoGenerateMips = false,
                            filterMode = FilterMode.Trilinear,
                            useMipMap = true,
                            volumeDepth = textures.Count,
                            wrapMode = TextureWrapMode.Repeat
                        };
                    }

                    // Copy all the textures and their mips into the array. Textures which are smaller are tiled so bilinear sampling isn't broken - this is why it's not possible to pack different textures together in the same slice.
                    for (int i = 0; i < textures.Count; i++)
                    {
                        Texture2D sourceTex = TextureCache.TryGetTexture(textures[i].Key, false);

                        int sourceMip = 0;
                        int sourceMaxDim = Mathf.Max(sourceTex.width, sourceTex.height);
                        int sourceWidth = sourceTex.width;
                        int sourceHeight = sourceTex.height;
                        while (sourceMaxDim > _maxTextureSize)
                        {
                            sourceMip++;
                            sourceMaxDim /= 2;
                            sourceWidth /= 2;
                            sourceHeight /= 2;
                        }

                        for (int mip = sourceMip; mip < sourceTex.mipmapCount; mip++)
                        {
                            int targetMip = mip - sourceMip;
                            for (int x = 0; x < texArray.width / sourceWidth; x++)
                            {
                                for (int y = 0; y < texArray.height / sourceHeight; y++)
                                {
                                    if (texArray is Texture2DArray)
                                    {
                                        Graphics.CopyTexture(sourceTex, 0, mip, 0, 0, sourceTex.width >> mip,
                                            sourceTex.height >> mip, texArray, i, targetMip, (sourceTex.width >> mip) * x,
                                            (sourceTex.height >> mip) * y);
                                    }
                                    else
                                    {
                                        CommandBuffer cmd = CommandBufferPool.Get();
                                        RenderTexture rt = (RenderTexture)texArray;
                                        cmd.SetRenderTarget(new RenderTargetBinding(new RenderTargetSetup(rt.colorBuffer, rt.depthBuffer, targetMip, CubemapFace.Unknown, i)));
                                        Vector2 scale = new Vector2((float)sourceTex.width / texArray.width, (float)sourceTex.height / texArray.height);
                                        Blitter.BlitQuad(cmd, sourceTex, new Vector4(1, 1, 0, 0), new Vector4(scale.x, scale.y, scale.x * x, scale.y * y), mip, false);
                                        Graphics.ExecuteCommandBuffer(cmd);
                                        cmd.Clear();
                                        CommandBufferPool.Release(cmd);
                                    }
                                }
                            }
                        }

                        Object.Destroy(sourceTex);

                        if (i % 20 == 0)
                        {
                            await Task.Yield();
                        }
                    }

                    _tempTextureArrays.Add(textureArrayType, texArray);
                }

                stopwatch.Stop();
                Debug.Log($"Built tex array in {stopwatch.ElapsedMilliseconds / 1000f} s");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("BuildTextureArray failed. Please read exception above.");
                throw;
            }
        }

        public void AssignTextureArray(StaticCacheManager.CacheEntry entry, MeshRenderer renderer)
        {
            var finalMaterials = new List<Material>(entry.MeshData.TextureTypes.Length);

            foreach (var textureType in entry.MeshData.TextureTypes)
            {
                var texture = _tempTextureArrays[textureType];
                var material = entry.MeshData.MaterialGroup == MaterialGroup.Water
                    ? GetWaterMaterial()
                    : GetDefaultMaterial(textureType == TextureArrayTypes.Transparent);

                material.mainTexture = texture;
                finalMaterials.Add(material);
            }

            renderer.SetMaterials(finalMaterials);
        }

        private Material GetDefaultMaterial(bool isAlphaTest)
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

        private Material GetWaterMaterial()
        {
            var material = new Material(Constants.ShaderWater);
            // Manually correct the render queue for alpha test, as Unity doesn't want to do it from the shader's render queue tag.
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        /// <summary>
        /// Once a TextureArray is build and assigned to renderers, we can safely clear these lists to free managed memory.
        /// </summary>
        public void Dispose()
        {
            TexturesToIncludeInArray.ClearAndReleaseMemory();
            WorldMeshRenderersForTextureArray.ClearAndReleaseMemory();
            VobMeshesForTextureArray.ClearAndReleaseMemory();
        }
    }
}
