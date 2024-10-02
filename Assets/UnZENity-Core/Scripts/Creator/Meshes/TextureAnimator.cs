using System.Collections;
using System.Collections.Generic;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.Rendering;
using static GUZ.Core.Caches.TextureCache;

namespace GUZ.Core.Creator.Meshes
{
    public class TextureAnimator : SingletonBehaviour<TextureAnimator>
    {
        public List<(string name, float fps, TextureArrayTypes type, int arrayIndex)> matList = new();

        public void Begin()
        {
            foreach (var (name, fps, type, arrayIndex) in matList)
            {
                var frames = TryGetAnimTextures(name);
                StartCoroutine(AnimateTextures(arrayIndex, frames, fps, TextureArrays[type]));
            }
        }

        private static IEnumerator AnimateTextures(int index, Texture2D[] frames, float framesPerSecond,
            Texture texArray)
        {
            int frameIndex = 0;
            while (true)
            {
                if (frames == null || frameIndex >= frames.Length)
                    yield return // Wait for the next frame based on the frame rate
                        new WaitForSeconds(1.0f / framesPerSecond);

                Texture2D sourceTex = frames[frameIndex];

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
                                    sourceTex.height >> mip, texArray, index, targetMip, (sourceTex.width >> mip) * x,
                                    (sourceTex.height >> mip) * y);
                            }
                            else
                            {
                                CommandBuffer cmd = CommandBufferPool.Get();
                                RenderTexture rt = (RenderTexture)texArray;
                                cmd.SetRenderTarget(new RenderTargetBinding(new RenderTargetSetup(rt.colorBuffer, rt.depthBuffer, targetMip, CubemapFace.Unknown, index)));
                                Vector2 scale = new Vector2((float)sourceTex.width / texArray.width, (float)sourceTex.height / texArray.height);
                                Blitter.BlitQuad(cmd, sourceTex, new Vector4(1, 1, 0, 0), new Vector4(scale.x, scale.y, scale.x * x, scale.y * y), mip, false);
                                Graphics.ExecuteCommandBuffer(cmd);
                                cmd.Clear();
                                CommandBufferPool.Release(cmd);
                            }
                        }
                    }
                }

                frameIndex =
                    (frameIndex + 1) % frames.Length; // Loop back to the start of the array after the last frame

                yield return // Wait for the next frame based on the frame rate
                    new WaitForSeconds(1.0f / framesPerSecond);
            }
        }
    }
}
