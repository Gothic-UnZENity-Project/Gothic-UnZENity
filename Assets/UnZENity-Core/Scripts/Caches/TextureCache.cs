using System.Collections.Generic;
using System.IO;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;
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
        public static Dictionary<TextureArrayManager.TextureArrayTypes, Texture> TextureArrays { get; } = new();
        public static Dictionary<Mesh, VobMeshData> VobMeshesForTextureArray = new();

        private static readonly Dictionary<string, Texture2D> _texture2DCache = new();

        public class VobMeshData
        {
            public IMultiResolutionMesh Mrm { get; set; }
            public List<TextureArrayManager.TextureArrayTypes> TextureArrayTypes { get; set; }
            public List<Renderer> Renderers { get; set; } = new();

            public VobMeshData(IMultiResolutionMesh mrm, List<TextureArrayManager.TextureArrayTypes> textureArrayTypes, Renderer renderer = null)
            {
                Mrm = mrm;
                TextureArrayTypes = textureArrayTypes;
                if (renderer != null)
                {
                    Renderers.Add(renderer);
                }
            }
        }

        [CanBeNull]
        public static Texture2D TryGetTexture(string key, bool includeInCache = true)
        {
            string preparedKey = GetPreparedKey(key);
            if (_texture2DCache.TryGetValue(preparedKey, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            return TryGetTexture(ResourceLoader.TryGetTexture(key), preparedKey, includeInCache);
        }

        public static Texture2D TryGetTexture(ITexture zkTexture, string key, bool includeInCache = true)
        {
            if (zkTexture == null)
            {
                return null;
            }

            if (_texture2DCache.TryGetValue(key, out Texture2D cachedTexture))
            {
                return cachedTexture;
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

            if (includeInCache)
            {
                _texture2DCache[preparedKey] = texture;
            }

            return texture;
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
            TextureArrays.Clear();
            TextureArrays.TrimExcess();
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
        }
    }
}
