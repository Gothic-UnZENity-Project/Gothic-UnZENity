﻿using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Constants = GUZ.Core.Globals.Constants;

namespace GUZ.Core.Caches.StaticCache
{
    public class TextureArrayCacheCreator
    {
        public Dictionary<string, (int maxDim, TextureFormat textureFormat)> TextureArrayInformation { get; } = new();

        public void CalculateTextureArrayInformation(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                // We ignore oCItem for now as we will load them all in one afterward.
                // We also calculate bounds only for objects which are marked to be cached inside Constants.
                if (vob.Type == VirtualObjectType.oCItem || !Constants.StaticCacheVobTypes.Contains(vob.Type))
                {
                    // Check children
                    CalculateTextureArrayInformation(vob.Children);
                    continue;
                }

                var visualName = vob.GetVisualName();

                // Already cached
                if (TextureArrayInformation.ContainsKey(visualName))
                {
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
                CalculateTextureArrayInformation(vob.Children);
            }
        }

        /// <summary>
        /// As there might be VOBs which aren't in a new game, but when gamers load a save game,
        /// we need to calculate bounds for all! items.
        /// </summary>
        public void CalculateItemTextureArrayInformation()
        {
            var allItems = GameData.GothicVm.GetInstanceSymbols("C_Item");

            foreach (var obj in allItems)
            {
                var item = VmInstanceManager.TryGetItemData(obj.Name);

                if (item == null)
                {
                    continue;
                }

                AddTexInfoForItem(item);
            }
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

                    mdm.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                    mdm.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                    break;
                case VisualType.MultiResolutionMesh:
                    var mrm = ResourceLoader.TryGetMultiResolutionMesh(vob.GetVisualName());

                    if (mrm == null)
                    {
                        return;
                    }

                    mrm.Materials.ForEach(material => AddTextureToCache(material.Texture));
                    break;
                case VisualType.Model:
                    var mdl = ResourceLoader.TryGetModel(vob.GetVisualName());

                    if (mdl == null)
                    {
                        return;
                    }

                    mdl.Mesh.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                    mdl.Mesh.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                    break;
                case VisualType.MorphMesh:
                    var mmb = ResourceLoader.TryGetMorphMesh(vob.GetVisualName());

                    if (mmb == null)
                    {
                        return;
                    }

                    mmb.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture));
                    break;
            }
        }

        private void AddTextureToCache(string textureName)
        {
            var texture = ResourceLoader.TryGetTexture(textureName);

            if (texture == null)
            {
                return;
            }

            var unityTextureFormat = texture.Format.AsUnityTextureFormat();

            if (unityTextureFormat != TextureFormat.DXT1 && unityTextureFormat != TextureFormat.RGBA32)
            {
                Debug.LogError("Only DXT1 and RGBA32 textures are supported for texture arrays as of now!");
            }

            TextureArrayInformation.TryAdd(textureName,
                (maxDim: Math.Max(texture.Width, texture.Height), textureFormat: unityTextureFormat));
        }

        private void AddTexInfoForItem(ItemInstance item)
        {
            // MDL
            var mdl = ResourceLoader.TryGetModel(item.Visual);
            if (mdl != null)
            {
                mdl.Mesh.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                mdl.Mesh.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                return;
            }

            // MDH+MDM (without MDL as wrapper)
            var mdh = ResourceLoader.TryGetModelHierarchy(item.Visual);
            var mdm = ResourceLoader.TryGetModelMesh(item.Visual);
            if (mdh != null && mdm != null)
            {
                mdm.Meshes.ForEach(mesh => mesh.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                mdm.Attachments.ForEach(mesh => mesh.Value.Materials.ForEach(material => AddTextureToCache(material.Texture)));
                return;
            }

            // MMB
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);
            if (mmb != null)
            {
                mmb.Mesh.Materials.ForEach(material => AddTextureToCache(material.Texture));
                return;
            }

            // MRM
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            if (mrm != null)
            {
                mrm.Materials.ForEach(material => AddTextureToCache(material.Texture));
                return;
            }
        }
    }
}
