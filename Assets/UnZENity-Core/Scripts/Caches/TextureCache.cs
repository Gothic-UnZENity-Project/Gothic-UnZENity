using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using GUZ.Core.World;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit;
using Debug = UnityEngine.Debug;
using Mesh = UnityEngine.Mesh;
using Object = UnityEngine.Object;
using Texture = UnityEngine.Texture;
using TextureFormat = ZenKit.TextureFormat;

namespace GUZ.Core.Caches
{
    /// <summary>
    /// Texture Array is used for the following improvements:
    /// 1. World mesh chunks are merged into sliced with specific bound. Without the texture array, we would need to separate each small floor mesh if it has a different texture.
    /// 2. Static VOBs will merge multiple textures into one. This reduces draw calls. (e.g. various complex VOBs have multiple textures).
    ///
    /// Once world is loaded, The texture cache is released to free memory of our calculated data. Only texture array itself remains in memory.
    ///
    /// Not in Texture Array:
    /// 1. NPCs and their armors (as they alter their armaments during runtime)
    /// 2. VOB Items which spawn at a later state (e.g. Player puts an item out of inventory)
    /// </summary>
    public static class TextureCache
    {
        public const int ReferenceTextureSize = 256;
        public const int MaxTextureSize = 512;

        public static Dictionary<TextureArrayTypes, Texture> TextureArrays { get; } = new();
        public static List<(MeshRenderer Renderer, WorldContainer.SubMeshData SubmeshData)> WorldMeshRenderersForTextureArray = new();
        public static Dictionary<Mesh, VobMeshData> VobMeshesForTextureArray = new();

        private static readonly Dictionary<string, Texture2D> _texture2DCache = new();
        private static readonly Dictionary<TextureArrayTypes, List<(string PreparedKey, ZkTextureData TextureData)>> _texturesToIncludeInArray = new();

        public enum TextureArrayTypes
        {
            Opaque,
            Transparent,
            Water
        }

        public class VobMeshData
        {
            public IMultiResolutionMesh Mrm { get; set; }
            public List<TextureArrayTypes> TextureArrayTypes { get; set; }
            public List<Renderer> Renderers { get; set; } = new();

            public VobMeshData(IMultiResolutionMesh mrm, List<TextureArrayTypes> textureArrayTypes, Renderer renderer = null)
            {
                Mrm = mrm;
                TextureArrayTypes = textureArrayTypes;
                if (renderer != null)
                {
                    Renderers.Add(renderer);
                }
            }
        }

        private class ZkTextureData
        {
            public string Key { get; set; }
            public Vector2 Scale { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int MipmapCount { get; set; }
            public int AnimFrameCount { get; set; }

            public ZkTextureData(string key, ITexture zkTexture)
            {
                Key = key;
                MipmapCount = zkTexture.MipmapCount;
                Width = zkTexture.Width;
                Height = zkTexture.Height;
                Scale = new Vector2((float)zkTexture.Width / ReferenceTextureSize, (float)zkTexture.Height / ReferenceTextureSize);
            }
        }

        /// <summary>
        /// useCache - We don't use Cache for TextureArrays.
        ///            As the textures are copied and original Texture2D gets invalidated by Unity during creation of TextureArrays.
        /// </summary>
        [CanBeNull]
        public static Texture2D TryGetTexture(string key, bool useCache = true)
        {
            string preparedKey = GetPreparedKey(key);

            if (useCache)
            {
                if (_texture2DCache.TryGetValue(preparedKey, out Texture2D cachedTexture))
                {
                    return cachedTexture;
                }
            }

            return TryGetTexture(ResourceLoader.TryGetTexture(key), preparedKey, useCache);
        }

        /// <summary>
        /// useCache - We don't use Cache for TextureArrays.
        ///            As the textures are copied and original Texture2D gets invalidated by Unity during creation of TextureArrays.
        /// </summary>
        public static Texture2D TryGetTexture(ITexture zkTexture, string key, bool useCache = true)
        {
            if (zkTexture == null)
            {
                return null;
            }

            if (useCache)
            {
                if (_texture2DCache.TryGetValue(key, out Texture2D cachedTexture))
                {
                    return cachedTexture;
                }
            }

            Texture2D texture;

            string preparedKey = GetPreparedKey(key);

            // Workaround for Unity and DXT1 Mipmaps.
            if (zkTexture.Format == TextureFormat.Dxt1 && zkTexture.MipmapCount == 1)
            {
                texture = GenerateDxt1Mipmaps(zkTexture);
            }
            else
            {
                UnityEngine.TextureFormat format = zkTexture.Format.AsUnityTextureFormat();

                // Let Unity generate Mipmaps if they aren't provided by Gothic texture itself.
                bool updateMipmaps = zkTexture.MipmapCount == 1;

                // Use Gothic's mips if provided.
                texture = new Texture2D(zkTexture.Width, zkTexture.Height, format, zkTexture.MipmapCount, false);
                for (var i = 0; i < zkTexture.MipmapCount; i++)
                {
                    if (format == UnityEngine.TextureFormat.RGBA32)
                    {
                        // RGBA is uncompressed format.
                        texture.SetPixelData(zkTexture.AllMipmapsRgba[i], i);
                    }
                    else
                    {
                        // Raw means "compressed data provided by Gothic texture"
                        texture.SetPixelData(zkTexture.AllMipmapsRaw[i], i);
                    }
                }

                texture.Apply(updateMipmaps, true);
            }

            texture.filterMode = FilterMode.Trilinear;
            texture.name = key;

            if (useCache)
            {
                _texture2DCache[preparedKey] = texture;
            }

            return texture;
        }

        public static void GetTextureArrayIndex(IMaterial materialData, out TextureArrayTypes textureArrayType, out int arrayIndex, out Vector2 textureScale, out int maxMipLevel, out int animFrameCount)
        {
            string key = materialData.Texture;
            animFrameCount = 0;

            if (materialData.Group == MaterialGroup.Water)
            {
                textureArrayType = TextureArrayTypes.Water;
                if (GetAlreadyIncludedTexture(textureArrayType, key, out arrayIndex, out maxMipLevel, out textureScale, out animFrameCount))
                {
                    return;
                }
            }
            else
            {
                textureArrayType = TextureArrayTypes.Opaque;
                if (GetAlreadyIncludedTexture(textureArrayType, key, out arrayIndex, out maxMipLevel, out textureScale, out animFrameCount))
                {
                    return;
                }

                textureArrayType = TextureArrayTypes.Transparent;
                if (GetAlreadyIncludedTexture(textureArrayType, key, out arrayIndex, out maxMipLevel, out textureScale, out animFrameCount))
                {
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

            UnityEngine.TextureFormat textureFormat = zenTextureData.Format.AsUnityTextureFormat();

            if (materialData.Group != MaterialGroup.Water)
            {
                textureArrayType = textureFormat == UnityEngine.TextureFormat.DXT1
                    ? TextureArrayTypes.Opaque
                    : TextureArrayTypes.Transparent;
            }

            maxMipLevel = zenTextureData.MipmapCount - 1;

            textureScale = new Vector2((float)zenTextureData.Width / ReferenceTextureSize, (float)zenTextureData.Height / ReferenceTextureSize);
            if (!_texturesToIncludeInArray.ContainsKey(textureArrayType))
            {
                _texturesToIncludeInArray.Add(textureArrayType, new List<(string, ZkTextureData)>());
            }

            _texturesToIncludeInArray[textureArrayType].Add((key, new ZkTextureData(key, zenTextureData)));
            arrayIndex = _texturesToIncludeInArray[textureArrayType].Count - 1;

            if (materialData.TextureAnimationFps > 0)
            {
                List<(string, ZkTextureData)> animTextures = TryGetAnimTextures(key);
                animFrameCount = animTextures.Count + 1;
                _texturesToIncludeInArray[textureArrayType][arrayIndex].TextureData.AnimFrameCount = animFrameCount;
                _texturesToIncludeInArray[textureArrayType].AddRange(animTextures);
            }
        }

        public static async Task BuildTextureArrays()
        {
            // Unload the previous texture arrays.
            foreach (TextureArrayTypes item in TextureArrays.Keys)
            {
                Object.Destroy(TextureArrays[item]);
            }
            TextureArrays.Clear();

            Stopwatch stopwatch = new();
            stopwatch.Start();

            foreach (TextureArrayTypes textureArrayType in _texturesToIncludeInArray.Keys)
            {
                if (Application.isEditor)
                {
                    for (int i = 16; i <= 2048; i *= 2)
                    {
                        int resCount = _texturesToIncludeInArray[textureArrayType].Where(t => Mathf.Max(t.TextureData.Width, t.TextureData.Height) == i).Count();
                        Debug.Log($"[{nameof(TextureCache)}] {textureArrayType}: {resCount} textures ({Mathf.RoundToInt(100 * (float)resCount / _texturesToIncludeInArray[textureArrayType].Count)}%) with dimension {i}.");
                    }
                }

                // Create the texture array with the max size of the contained textures, limited by the max texture size.
                int maxSize = Mathf.Min(MaxTextureSize, _texturesToIncludeInArray[textureArrayType].Max(p => p.TextureData.Width));
                ZkTextureData textureWithMaxAllowedSize = _texturesToIncludeInArray[textureArrayType].Where(t => t.TextureData.Width == maxSize && t.TextureData.Height == maxSize).Select(t => t.TextureData).FirstOrDefault();
                // Find the max mip depth defined for the max allowed texture size.
                int maxMipDepth = 0;
                if (textureWithMaxAllowedSize == null)
                {
                    Debug.LogError($"[{nameof(TextureCache)}] No texture with size {maxSize}x{maxSize} was found to determine max allowed mip level. Falling back to texture with highest mip count.");
                    maxMipDepth = _texturesToIncludeInArray[textureArrayType].Max(t => t.TextureData.MipmapCount);
                }
                else
                {
                    maxMipDepth = textureWithMaxAllowedSize.MipmapCount;
                }

                UnityEngine.TextureFormat textureFormat = UnityEngine.TextureFormat.RGBA32;
                if (textureArrayType == TextureArrayTypes.Opaque)
                {
                    textureFormat = UnityEngine.TextureFormat.DXT1;
                }

                Texture texArray;
                if (textureArrayType != TextureArrayTypes.Water)
                {
                    texArray = new Texture2DArray(maxSize, maxSize, _texturesToIncludeInArray[textureArrayType].Count, textureFormat, maxMipDepth, false, true)
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
                        volumeDepth = _texturesToIncludeInArray[textureArrayType].Count,
                        wrapMode = TextureWrapMode.Repeat
                    };
                }

                // Copy all the textures and their mips into the array. Textures which are smaller are tiled so bilinear sampling isn't broken - this is why it's not possible to pack different textures together in the same slice.
                for (int i = 0; i < _texturesToIncludeInArray[textureArrayType].Count; i++)
                {
                    Texture2D sourceTex = TryGetTexture(_texturesToIncludeInArray[textureArrayType][i].PreparedKey, false);

                    int sourceMip = 0;
                    int sourceMaxDim = Mathf.Max(sourceTex.width, sourceTex.height);
                    int sourceWidth = sourceTex.width;
                    int sourceHeight = sourceTex.height;
                    while (sourceMaxDim > MaxTextureSize)
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

                    await FrameSkipper.TrySkipToNextFrame();
                }

                TextureArrays.Add(textureArrayType, texArray);
            }

            // Clear cached texture data to save storage.
            foreach (List<(string PreparedKey, ZkTextureData Texture)> textureList in _texturesToIncludeInArray.Values)
            {
                textureList.Clear();
                textureList.TrimExcess();
            }

            stopwatch.Stop();
            Debug.Log($"Built tex array in {stopwatch.ElapsedMilliseconds / 1000f} s");
        }

        private static bool GetAlreadyIncludedTexture(TextureArrayTypes textureArrayType, string key, out int arrayIndex, out int maxMipLevel, out Vector2 textureScale, out int animFrameCount)
        {
            if (_texturesToIncludeInArray.ContainsKey(textureArrayType))
            {
                (string, ZkTextureData) cachedTextureData = _texturesToIncludeInArray[textureArrayType].FirstOrDefault(k => k.PreparedKey == key);
                if (cachedTextureData != default)
                {
                    arrayIndex = _texturesToIncludeInArray[textureArrayType].IndexOf(cachedTextureData);
                    ZkTextureData zkData = _texturesToIncludeInArray[textureArrayType][arrayIndex].TextureData;
                    maxMipLevel = zkData.MipmapCount - 1;
                    textureScale = zkData.Scale;
                    animFrameCount = zkData.AnimFrameCount;
                    return true;
                }
            }

            arrayIndex = -1;
            maxMipLevel = -1;
            textureScale = default;
            animFrameCount = 0;
            return false;
        }

        private static List<(string, ZkTextureData)> TryGetAnimTextures(string key)
        {
            List<(string, ZkTextureData)> textures = new();
            if (!key.Contains("_A0") && !key.Contains("_a0"))
            {
                return textures;
            }
            for (int id = 1; ; id++)
            {
                // Replace the frame number in the key with the current id
                string frameKey = Regex.Replace(key, "_[Aa]0", $"_A{id}");
                frameKey = frameKey.ToUpper();
                ITexture zkTex = ResourceLoader.TryGetTexture(frameKey);
                if (zkTex == null)
                {
                    break;
                }
                textures.Add((frameKey, new ZkTextureData(frameKey, zkTex)));
            }

            return textures;
        }

        /// <summary>
        /// Unity doesn't want to create mips for DXT1 textures. Recreate them as RGB24.
        /// </summary>
        private static Texture2D GenerateDxt1Mipmaps(ITexture zkTexture)
        {
            var dxtTexture = new Texture2D(zkTexture.Width, zkTexture.Height, UnityEngine.TextureFormat.DXT1, false);
            dxtTexture.SetPixelData(zkTexture.AllMipmapsRaw[0], 0);
            dxtTexture.Apply(false);

            var texture = new Texture2D(zkTexture.Width, zkTexture.Height, UnityEngine.TextureFormat.RGB24, true);
            texture.SetPixels(dxtTexture.GetPixels());
            texture.Apply(true, true);
            Object.Destroy(dxtTexture);

            return texture;
        }

        /// <summary>
        /// Once a TextureArray is build and assigned to renderers, we can safely clear these lists to free managed memory.
        /// </summary>
        public static void RemoveCachedTextureArrayData()
        {
            _texturesToIncludeInArray.Clear();
            _texturesToIncludeInArray.TrimExcess();

            WorldMeshRenderersForTextureArray.Clear();
            WorldMeshRenderersForTextureArray.TrimExcess();
        }

        private static string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
            {
                return lowerKey;
            }

            return lowerKey.Replace(extension, "");
        }

        public static void Dispose()
        {
            _texture2DCache.Clear();

            TextureArrays.Clear();
            TextureArrays.TrimExcess();

            foreach (var textureList in _texturesToIncludeInArray.Values)
            {
                textureList.Clear();
                textureList.TrimExcess();
            }
        }
    }
}
