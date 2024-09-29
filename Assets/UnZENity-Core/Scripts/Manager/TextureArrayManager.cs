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

    public class TextureArrayManager
    {
        public const int ReferenceTextureSize = 256;
        public const int MaxTextureSize = 512;

        public readonly Dictionary<TextureArrayTypes, List<(string PreparedKey, ZkTextureData TextureData)>> TexturesToIncludeInArray = new();
        public readonly List<(MeshRenderer Renderer, WorldData.SubMeshData SubmeshData)> WorldMeshRenderersForTextureArray = new();
        public readonly Dictionary<Mesh, VobMeshData> VobMeshesForTextureArray = new();

        public enum TextureArrayTypes
        {
            Opaque,
            Transparent,
            Water
        }

        [Serializable]
        public class ZkTextureData
        {
            public string Key;
            public Vector2 Scale;
            public int Width;
            public int Height;
            public int MipmapCount;

            public ZkTextureData(string key, ITexture zkTexture)
            {
                Key = key;
                MipmapCount = zkTexture.MipmapCount;
                Width = zkTexture.Width;
                Height = zkTexture.Height;
                Scale = new Vector2((float)zkTexture.Width / ReferenceTextureSize, (float)zkTexture.Height / ReferenceTextureSize);
            }
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

                (string, ZkTextureData) cachedTextureData = TexturesToIncludeInArray[(TextureArrayTypes)i].FirstOrDefault(k => k.PreparedKey == key);
                if (cachedTextureData != default)
                {
                    textureArrayType = (TextureArrayTypes)i;

                    arrayIndex = TexturesToIncludeInArray[textureArrayType].IndexOf(cachedTextureData);
                    ZkTextureData zkData = TexturesToIncludeInArray[textureArrayType][arrayIndex].TextureData;
                    maxMipLevel = zkData.MipmapCount - 1;
                    textureScale = zkData.Scale;
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

            textureScale = new Vector2((float)zenTextureData.Width / ReferenceTextureSize, (float)zenTextureData.Height / ReferenceTextureSize);
            if (!TexturesToIncludeInArray.ContainsKey(textureArrayType))
            {
                TexturesToIncludeInArray.Add(textureArrayType, new List<(string, ZkTextureData)>());
            }

            TexturesToIncludeInArray[textureArrayType].Add((key, new ZkTextureData(key, zenTextureData)));
            arrayIndex = TexturesToIncludeInArray[textureArrayType].Count - 1;
        }

        public async Task BuildTextureArraysFromCache(TextureArrayContainer data)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            try
            {
                foreach (var entry in data.WorldChunkTypes)
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
                    int maxSize = Mathf.Min(MaxTextureSize, textures.Max(p => p.Width));
                    ZkTextureData textureWithMaxAllowedSize = textures.Where(t => t.Width == maxSize && t.Height == maxSize).FirstOrDefault();
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

                        if (i % 20 == 0)
                        {
                            await Task.Yield();
                        }
                    }

                    TextureCache.TextureArrays.Add(textureArrayType, texArray);
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

        public void AssignTextureArraysForWorld(TextureArrayContainer data, GameObject worldRootGo)
        {
            var renderers = worldRootGo.GetComponentsInChildren<Renderer>();

            if (renderers.Length != data.WorldChunks.Count)
            {
                Debug.LogError($"Number of texture arrays from cached world metadata ({data.WorldChunks.Count}) " +
                               $"does not match number of mesh renderers in glTF cached file ({renderers.Length}). " +
                               "Please recreate your cache.");
            }

            for (var i = 0; i < data.WorldChunks.Count; i++)
            {
                var chunk = data.WorldChunks[i];
                var textureType = chunk.TextureTypes.First();
                var rend = renderers[i];
                var mesh = rend.gameObject.GetComponent<MeshFilter>().sharedMesh;

                if (mesh.subMeshCount != 1)
                {
                    Debug.LogError("World meshes with multiple submeshes (aka multiple shaders to render in the end) aren't supported.");
                    return;
                }

                var texture = TextureCache.TextureArrays[textureType];

                Material material;
                if (chunk.MaterialGroup == MaterialGroup.Water)
                {
                    material = GetWaterMaterial();
                }
                else
                {
                    material = GetDefaultMaterial(textureType == TextureArrayTypes.Transparent);
                }

                material.mainTexture = texture;
                rend.material = material;
                mesh.SetUVs(0, chunk.UVs);
                mesh.SetColors(chunk.Colors);
            }
        }

        public void AssignTextureArraysForVobs(TextureArrayContainer data, GameObject vobsRootGo)
        {
            // For faster lookup
            Dictionary<string, TextureArrayContainer.MeshEntry> vobLookup;
            try
            {
                 vobLookup = data.Vobs.ToDictionary(i => i.Name, i => i);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var renderers = vobsRootGo.GetComponentsInChildren<Renderer>();

            foreach (var rend in renderers)
            {
                Transform rootGo = rend.transform;
                TextureArrayContainer.MeshEntry vob = null;
                string vobEntryName = $"{rootGo.name}-{rend.name}";
                while (rootGo != null && !vobLookup.TryGetValue(vobEntryName, out vob))
                {
                    rootGo = rootGo.parent;

                    // We build the entry name always like this: ROOT_GO-RENDERER_GO
                    vobEntryName = $"{rootGo.name}-{rend.name}";
                }

                if (vob == null)
                {
                    Debug.LogError($"Couldn't find VOB root on {rend.gameObject.name}. Skipping...");
                    continue;
                }

                var finalMaterials = new List<Material>(vob.TextureTypes.Count);
                var mesh = rend.GetComponent<MeshFilter>().sharedMesh;
                var subMeshCount = mesh.subMeshCount;

                if (vob.TextureTypes.Count != subMeshCount)
                {
                    Debug.LogError($"Number of texture types ({vob.TextureTypes.Count}) does not match number of subMeshes " +
                                   $"({subMeshCount}) in cached VOB {vob.Name}. This is mainly a bug or the world.zen is updated " +
                                   "(via installing a mod) after cache was created.");
                    return;
                }

                for (var i = 0; i < subMeshCount; i++)
                {
                    var textureType = vob.TextureTypes[i];
                    var texture = TextureCache.TextureArrays[textureType];
                    var material = GetDefaultMaterial(texture && ((Texture2DArray)texture).format == TextureFormat.RGBA32);

                    material.mainTexture = texture;
                    rend.material = material;
                    finalMaterials.Add(material);
                }

                rend.SetMaterials(finalMaterials);
                mesh.SetUVs(0, vob.UVs);
                mesh.SetColors(vob.Colors);
            }
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
            // TODO
            // TextureArrays.Clear();
            // TextureArrays.TrimExcess();

            TexturesToIncludeInArray.Clear();
            TexturesToIncludeInArray.TrimExcess();

            WorldMeshRenderersForTextureArray.Clear();
            WorldMeshRenderersForTextureArray.TrimExcess();
        }
    }
}
