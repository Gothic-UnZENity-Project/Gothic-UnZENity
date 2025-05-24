using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.UI.Menus.LoadingBars;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Constants = GUZ.Core.Globals.Constants;
using Logger = GUZ.Core.Util.Logger;
using TextureFormat = UnityEngine.TextureFormat;

namespace GUZ.Core.Caches.StaticCache
{
    public class TextureArrayCacheCreator
    {
        public Dictionary<string, StaticCacheManager.TextureInfo> TextureArrayInformation { get; } = new();

        /// <summary>
        /// Load all materials from world mesh and assign textures to texture array accordingly.
        ///
        /// We cache all texture information. In G1.world.zen, there are about 25 which aren't used in normal mesh (maybe in portals only?)
        /// But for sake of simplicity, we use them all.
        /// </summary>
        public async Task CalculateTextureArrayInformation(IMesh worldMesh, int worldIndex)
        {
            GameGlobals.Loading.SetPhase(
                $"{nameof(PreCachingLoadingBarHandler.ProgressTypesPerWorld.CalculateTextureArrayInformationMesh)}_{worldIndex}",
                worldMesh.MaterialCount);
            
            foreach (var material in worldMesh.Materials)
            {
                if (TextureArrayInformation.ContainsKey(material.Texture))
                {
                    continue;
                }

                AddTextureToCache(material.Group, material.Texture);

                GameGlobals.Loading.Tick();
                await FrameSkipper.TrySkipToNextFrame();
            }

            GameGlobals.Loading.FinalizePhase();
        }

        public async Task CalculateTextureArrayInformation(List<IVirtualObject> vobs, int worldIndex)
        {
            var elementAmount = CalculateElementAmount(vobs);
            GameGlobals.Loading.SetPhase($"{nameof(PreCachingLoadingBarHandler.ProgressTypesPerWorld.CalculateTextureArrayInformationVobs)}_{worldIndex}", elementAmount);
            
            await CalculateTextureArrayInformation(vobs);
            GameGlobals.Loading.FinalizePhase();
        }
        
        private int CalculateElementAmount(List<IVirtualObject> vobs)
        {
            var count = 0;
            foreach (var vob in vobs)
            {
                count++; // We count each element as we update potentially with each FrameSkipper call, which is unaffected if it's a light or sth. else.
                count += CalculateElementAmount(vob.Children);
            }
            return count;
        }
        
        private async Task CalculateTextureArrayInformation(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                GameGlobals.Loading.Tick();
                await FrameSkipper.TrySkipToNextFrame();

                // We ignore oCItem for now as we will load them all in one afterward.
                // We also calculate bounds only for objects which are marked to be cached inside Constants.
                if (vob.Type == VirtualObjectType.oCItem || !Constants.StaticCacheVobTypes.Contains(vob.Type))
                {
                    // Check children
                    await CalculateTextureArrayInformation(vob.Children);
                    continue;
                }

                switch (vob.Visual!.Type)
                {
                    case VisualType.Mesh:
                    case VisualType.Model:
                    case VisualType.MorphMesh:
                    case VisualType.MultiResolutionMesh:
                        AddTexInfoForSingleVob(vob);
                        break;

                    // TODO - Should Decals and ParticleEffects also leverage the texture array?
                    case VisualType.Decal:
                    case VisualType.ParticleEffect:
                    default:
                        break;
                }

                // Recursive
                await CalculateTextureArrayInformation(vob.Children);
            }
        }

        /// <summary>
        /// As there might be VOBs which aren't in a new game, but when gamers load a save game,
        /// we need to calculate bounds for all! items.
        /// </summary>
        public async Task CalculateItemTextureArrayInformation()
        {
            var allItems = GameData.GothicVm.GetInstanceSymbols("C_Item");

            GameGlobals.Loading.SetPhase(nameof(PreCachingLoadingBarHandler.ProgressTypesGlobal.CalculateItemTextureArrayInformation), allItems.Count);
            
            foreach (var obj in allItems)
            {
                await FrameSkipper.TrySkipToNextFrame();
                GameGlobals.Loading.Tick();
                
                var item = VmInstanceManager.TryGetItemData(obj.Name);

                if (item == null)
                {
                    continue;
                }

                AddTexInfoForItem(item);
            }
            
            GameGlobals.Loading.FinalizePhase();
        }

        private void AddTexInfoForSingleVob(IVirtualObject vob)
        {
            switch (vob.Visual!.Type)
            {
                case VisualType.Mesh:
                    var mdm = ResourceLoader.TryGetModelMesh(vob.GetVisualName());

                    if (mdm == null)
                    {
                        return;
                    }

                    mdm.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                    mdm.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                    break;
                case VisualType.MultiResolutionMesh:
                    var mrm = ResourceLoader.TryGetMultiResolutionMesh(vob.GetVisualName());

                    if (mrm == null)
                    {
                        return;
                    }

                    mrm.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture));
                    break;
                case VisualType.Model:
                    var mdl = ResourceLoader.TryGetModel(vob.GetVisualName());

                    if (mdl == null)
                    {
                        return;
                    }

                    mdl.Mesh.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                    mdl.Mesh.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                    break;
                case VisualType.MorphMesh:
                    var mmb = ResourceLoader.TryGetMorphMesh(vob.GetVisualName());

                    if (mmb == null)
                    {
                        return;
                    }

                    mmb.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture));
                    break;
            }
        }

        private void AddTextureToCache(MaterialGroup group, string textureName)
        {
            // Already cached.
            if (TextureArrayInformation.ContainsKey(textureName))
            {
                return;
            }

            var texture = ResourceLoader.TryGetTexture(textureName);

            if (texture == null)
            {
                return;
            }

            var unityTextureFormat = texture.Format.AsUnityTextureFormat();

            if (unityTextureFormat != TextureFormat.DXT1 && unityTextureFormat != TextureFormat.RGBA32)
            {
                Logger.LogError("Only DXT1 and RGBA32 textures are supported for texture arrays as of now!", LogCat.PreCaching);
            }

            var textureArrayType = TextureCache.TextureArrayTypes.Unknown;

            // Water is separate as we use a different shader.
            // TODO - Do we need to check for different TextureFormats as well?
            if (group == MaterialGroup.Water)
            {
                textureArrayType = TextureCache.TextureArrayTypes.Water;
            }
            // DXT1 can be opaque
            else if (unityTextureFormat == TextureFormat.DXT1)
            {
                textureArrayType = TextureCache.TextureArrayTypes.Opaque;
            }
            // RGBA32 is transparent
            else if (unityTextureFormat == TextureFormat.RGBA32)
            {
                textureArrayType = TextureCache.TextureArrayTypes.Transparent;
            }
            else
            {
                Logger.LogError($"TextureFormat={unityTextureFormat} + MaterialGroup={group} isn't handled for TextureArray so far.", LogCat.PreCaching);
            }

            var animationTextures = CalculateAnimationTextures(textureName);

            // TryAdd is used to ignore duplicates.
            TextureArrayInformation.TryAdd(textureName,
                new StaticCacheManager.TextureInfo(textureArrayType, Math.Max(texture.Width, texture.Height), animationTextures.Count));

            // If the texture is an "animated one", we also need to add the animation textures. During runtime, water will iterate the z-index of TextureArray to loop through these elements.
            foreach (var animationTexture in animationTextures)
            {
                TextureArrayInformation.Add(animationTexture.Key,
                    new StaticCacheManager.TextureInfo(textureArrayType, Math.Max(animationTexture.Value.Width, animationTexture.Value.Height), 0));
            }
        }

        /// <summary>
        /// If texture name contains _A0, then it is the start of an animated texture.
        /// We can fetch the corresponding animations and return them to be included as next elements inside Texture array.
        /// </summary>
        private Dictionary<string, ITexture> CalculateAnimationTextures(string textureName)
        {
            var textures = new Dictionary<string, ITexture>();
            if (!textureName.ContainsIgnoreCase("_A0"))
            {
                return textures;
            }

            for (var id = 1; ; id++)
            {
                // Replace the frame number in the key with the current id
                var frameKey = Regex.Replace(textureName, "_[Aa]0", $"_A{id}");
                var zkTex = ResourceLoader.TryGetTexture(frameKey);

                if (zkTex == null)
                {
                    break;
                }

                textures.Add(frameKey.ToUpper(), zkTex);
            }

            return textures;
        }

        private void AddTexInfoForItem(ItemInstance item)
        {
            // MDL
            var mdl = ResourceLoader.TryGetModel(item.Visual);
            if (mdl != null)
            {
                mdl.Mesh.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                mdl.Mesh.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                return;
            }

            // MDH+MDM (without MDL as wrapper)
            var mdh = ResourceLoader.TryGetModelHierarchy(item.Visual);
            var mdm = ResourceLoader.TryGetModelMesh(item.Visual);
            if (mdh != null && mdm != null)
            {
                mdm.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                mdm.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture)));
                return;
            }

            // MMB
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);
            if (mmb != null)
            {
                mmb.Mesh.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture));
                return;
            }

            // MRM
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            if (mrm != null)
            {
                mrm.Materials.ForEach(material => AddTextureToCache(material.Group, material.Texture));
                return;
            }
        }
    }
}
