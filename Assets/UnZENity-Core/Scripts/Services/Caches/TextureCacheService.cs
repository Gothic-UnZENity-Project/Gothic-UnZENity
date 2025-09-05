using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GUZ.Core.Core.Logging;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Services.StaticCache;
using GUZ.Core.Util;
using JetBrains.Annotations;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.Rendering;
using ZenKit;
using Logger = GUZ.Core.Core.Logging.Logger;
using Object = UnityEngine.Object;
using Texture = UnityEngine.Texture;
using TextureFormat = UnityEngine.TextureFormat;

namespace GUZ.Core.Services.Caches
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
    public class TextureCacheService
    {
        public const int ReferenceTextureSize = 256;

        /// <summary>
        /// In original G1, there is one element with 1024 in size. As we create a TextureArray,
        /// we would have huge empty space as the single element will create lots of empty space.
        /// Therefore we skip it and downsample this one.
        ///
        /// TODO - If we want to support 4k textures, we would need to raise MaxTextureSize and monitor Memory consumption.
        /// </summary>
        public const int MaxTextureSize = 512;
        // If we have 512 texture size, we have as highest mip of 8 (512<<6=8)
        public const int MaxMipCount = 6;


        [Inject] private readonly FrameSkipperService _frameSkipperService;
        [Inject] private readonly StaticCacheService _staticCacheService;

        public static Dictionary<TextureArrayTypes, Texture> TextureArrays { get; } = new();

        private static readonly Dictionary<string, Texture2D> _texture2DCache = new();
        private static readonly Dictionary<TextureArrayTypes, List<(string PreparedKey, ZkTextureData TextureData)>> _texturesToIncludeInArray = new();



        public enum TextureArrayTypes
        {
            Unknown,
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
        public Texture2D TryGetTexture(string key, bool useCache = true)
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
        public Texture2D TryGetTexture(ITexture zkTexture, string key, bool useCache = true)
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
            if (zkTexture.Format == ZenKit.TextureFormat.Dxt1 && zkTexture.MipmapCount == 1)
            {
                texture = GenerateDxt1Mipmaps(zkTexture);
            }
            else
            {
                TextureFormat format = zkTexture.Format.AsUnityTextureFormat();

                // Let Unity generate Mipmaps if they aren't provided by Gothic texture itself.
                bool updateMipmaps = zkTexture.MipmapCount == 1;

                // Use Gothic's mips if provided.
                texture = new Texture2D(zkTexture.Width, zkTexture.Height, format, zkTexture.MipmapCount, false);
                for (var i = 0; i < zkTexture.MipmapCount; i++)
                {
                    if (format == TextureFormat.RGBA32)
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

        public void GetTextureArrayIndex(IMaterial materialData, out TextureArrayTypes textureArrayType, out int arrayIndex, out Vector2 textureScale, out int maxMipLevel, out int animFrameCount)
        {
            var textureName = materialData.Texture;
            var texture = ResourceLoader.TryGetTexture(textureName);

            if (_staticCacheService.LoadedTextureInfoOpaque.ContainsKey(materialData.Texture))
            {
                arrayIndex = _staticCacheService.LoadedTextureInfoOpaque[materialData.Texture].Index;
                textureArrayType = TextureArrayTypes.Opaque;

                maxMipLevel = texture!.MipmapCount - 1;
                textureScale = new Vector2((float)texture.Width / ReferenceTextureSize, (float)texture.Height / ReferenceTextureSize);
                animFrameCount = _staticCacheService.LoadedTextureInfoOpaque[materialData.Texture].Data.AnimFrameC;
            }
            else if (_staticCacheService.LoadedTextureInfoTransparent.ContainsKey(materialData.Texture))
            {
                arrayIndex = _staticCacheService.LoadedTextureInfoTransparent[materialData.Texture].Index;
                textureArrayType = TextureArrayTypes.Transparent;

                maxMipLevel = texture!.MipmapCount - 1;
                textureScale = new Vector2((float)texture.Width / ReferenceTextureSize, (float)texture.Height / ReferenceTextureSize);
                animFrameCount = _staticCacheService.LoadedTextureInfoTransparent[materialData.Texture].Data.AnimFrameC;
            }
            else if (_staticCacheService.LoadedTextureInfoWater.ContainsKey(materialData.Texture))
            {
                arrayIndex = _staticCacheService.LoadedTextureInfoWater[materialData.Texture].Index;
                textureArrayType = TextureArrayTypes.Water;

                maxMipLevel = texture!.MipmapCount - 1;
                textureScale = new Vector2((float)texture.Width / ReferenceTextureSize, (float)texture.Height / ReferenceTextureSize);
                animFrameCount = _staticCacheService.LoadedTextureInfoWater[materialData.Texture].Data.AnimFrameC;
            }
            else
            {
                Logger.LogError($"Texture for Material {materialData.Name} couldn't be found in cache.", LogCat.PreCaching);
                arrayIndex = -1;
                textureArrayType = TextureArrayTypes.Unknown;
                maxMipLevel = 0;
                textureScale = Vector2.one;
                animFrameCount = 0;
            }
        }

        public async Task BuildTextureArray()
        {
            await BuildTextureArray(TextureFormat.DXT1,
                _staticCacheService.LoadedTextureInfoOpaque, TextureArrayTypes.Opaque);
            await BuildTextureArray(TextureFormat.RGBA32,
                _staticCacheService.LoadedTextureInfoTransparent, TextureArrayTypes.Transparent);
            await BuildTextureArray(TextureFormat.RGBA32,
                _staticCacheService.LoadedTextureInfoWater, TextureArrayTypes.Water);
        }

        private async Task BuildTextureArray(TextureFormat textureFormat, Dictionary<string, (int Index, StaticCacheService.TextureInfo Data)> textureInfos, TextureArrayTypes texArrType)
        {
            // It's either a Texture2DArray (solid meshes) or RenderTexture (water)
            Texture texArray;

            if (texArrType == TextureArrayTypes.Water)
            {
                texArray = new RenderTexture(MaxTextureSize, MaxTextureSize, 0, RenderTextureFormat.ARGB32, MaxMipCount)
                {
                    name = $"{textureFormat} - {texArrType}",
                    dimension = TextureDimension.Tex2DArray,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Trilinear,
                    useMipMap = true,
                    volumeDepth = textureInfos.Count,
                    wrapMode = TextureWrapMode.Repeat
                };
            }
            else
            {
                texArray = new Texture2DArray(MaxTextureSize, MaxTextureSize, textureInfos.Count, textureFormat, MaxMipCount, false, true)
                {
                    name = $"{textureFormat} - {texArrType}",
                    filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Repeat
                };
            }

            // Copy all the textures and their mips into the array. Textures which are smaller are tiled so bilinear sampling isn't broke.
            // This is why it's not possible to pack different textures together in the same slice.
            var i = -1;
            foreach (var texInfo in textureInfos)
            {
                ++i;
                var sourceTex = TryGetTexture(texInfo.Key, false);

                if (sourceTex == null)
                {
                    continue;
                }

                var skipCountOfOversizeMips = 0;
                var sourceMaxDim = Mathf.Max(sourceTex.width, sourceTex.height);
                var sourceWidth = sourceTex.width;
                var sourceHeight = sourceTex.height;

                // If a texture's size is higher than MaxTextureSize, then divide by 2 until we shrink it enough.
                while (sourceMaxDim > MaxTextureSize)
                {
                    skipCountOfOversizeMips++;
                    sourceMaxDim /= 2;
                    sourceWidth /= 2;
                    sourceHeight /= 2;
                }

                for (var mip = skipCountOfOversizeMips; mip < sourceTex.mipmapCount; mip++)
                {
                    // e.g. if we skip 1 oversizeMip, then the targetMip is always one lower than the actual one.
                    // In this example, we would _skip_ the highest mip and go with one below.
                    var targetMip = mip - skipCountOfOversizeMips;

                    // If a texture has more mips than we want to use. Skip the highest ones (== the ones with ultra-low resolution)
                    if (targetMip >= MaxMipCount)
                    {
                        break;
                    }

                    for (var x = 0; x < texArray.width / sourceWidth; x++)
                    {
                        for (var y = 0; y < texArray.height / sourceHeight; y++)
                        {

                            if (texArray is Texture2DArray)
                            {
                                Graphics.CopyTexture(sourceTex, 0, mip, 0, 0,
                                    sourceWidth >> mip, sourceHeight >> mip, texArray, i, targetMip,
                                    (sourceWidth >> mip) * x, (sourceHeight >> mip) * y);
                            }
                            // aka Water
                            else
                            {
                                var cmd = CommandBufferPool.Get();
                                var rt = (RenderTexture)texArray;
                                cmd.SetRenderTarget(new RenderTargetBinding(new RenderTargetSetup(rt.colorBuffer, rt.depthBuffer, targetMip, CubemapFace.Unknown, i)));
                                var scale = new Vector2((float)sourceTex.width / texArray.width, (float)sourceTex.height / texArray.height);
                                Blitter.BlitQuad(cmd, sourceTex, new Vector4(1, 1, 0, 0), new Vector4(scale.x, scale.y, scale.x * x, scale.y * y), mip, false);
                                Graphics.ExecuteCommandBuffer(cmd);
                                cmd.Clear();
                                CommandBufferPool.Release(cmd);
                            }
                        }
                    }
                }

                // We can save memory in Unity, as we don't need the separate Textures any longer.
                Object.Destroy(sourceTex);

                await _frameSkipperService.TrySkipToNextFrame();
            }

            // Store TextureArray in the appropriate TextureArray Cache object to assign it to objects later.
            TextureArrays.Add(texArrType, texArray);
        }

        public void GetTextureArrayEntry(string textureName, out Texture texture, out TextureArrayTypes textureType)
        {
            GetTextureArrayEntry(ResourceLoader.TryGetTexture(textureName), out texture, out textureType);
        }

        /// <summary>
        /// Returns Texture2DArry (Opaque/Transparent) or RenderTexture (Water)
        /// </summary>
        public void GetTextureArrayEntry(ITexture zkTexture, out Texture texture, out TextureArrayTypes textureType)
        {
            switch (zkTexture.Format.AsUnityTextureFormat())
            {
                case TextureFormat.DXT1:
                    textureType = TextureArrayTypes.Opaque;
                    break;
                case TextureFormat.RGBA32:
                    textureType = TextureArrayTypes.Transparent;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            texture = GetTextureArrayEntry(textureType);
        }

        public Texture GetTextureArrayEntry(TextureArrayTypes textureArrayType)
        {
            return TextureArrays[textureArrayType];
        }

        /// <summary>
        /// Unity doesn't want to create mips for DXT1 textures. Recreate them as RGB24.
        /// </summary>
        private Texture2D GenerateDxt1Mipmaps(ITexture zkTexture)
        {
            var dxtTexture = new Texture2D(zkTexture.Width, zkTexture.Height, TextureFormat.DXT1, false);
            dxtTexture.SetPixelData(zkTexture.AllMipmapsRaw[0], 0);
            dxtTexture.Apply(false);

            var texture = new Texture2D(zkTexture.Width, zkTexture.Height, TextureFormat.RGB24, true);
            texture.SetPixels(dxtTexture.GetPixels());
            texture.Apply(true, true);
            Object.Destroy(dxtTexture);

            return texture;
        }

        private string GetPreparedKey(string key)
        {
            var lowerKey = key.ToLower();
            var extension = Path.GetExtension(lowerKey);

            if (extension == string.Empty)
            {
                return lowerKey;
            }

            return lowerKey.Replace(extension, "");
        }

        public void Dispose()
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
