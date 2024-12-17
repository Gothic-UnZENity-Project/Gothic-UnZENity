using System;
using System.Collections.Generic;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Vobs;
using Object = UnityEngine.Object;

namespace GUZ.Core.Caches.StaticCache
{
    public class VobCacheCreator
    {
        public Dictionary<string, Bounds> Bounds { get; }

        public VobCacheCreator()
        {
            Bounds = new();
        }

        public void CalculateVobBounds(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                if (IsVobWithMesh(vob.Type))
                {
                    var meshName = GetVobMeshName(vob);

                    if (Bounds.ContainsKey(meshName))
                    {
                        continue;
                    }

                    var go = CreateVobMesh(meshName);
                    var boundingBox = CalculateBoundingBox(go);
                    Object.Destroy(go);

                    Bounds[meshName] = boundingBox;
                }

                CalculateVobBounds(vob.Children);
            }
        }

        /// <summary>
        /// As there might be VOBs which aren't in a new game, but when gamers load a save game,
        /// we need to calculate bounds for all! items.
        /// </summary>
        public void CalculateVobtemBounds()
        {
            var allItems = GameData.GothicVm.GetInstanceSymbols("C_Item");

            foreach (var obj in allItems)
            {
                var item = VmInstanceManager.TryGetItemData(obj.Name);

                if (item == null)
                {
                    continue;
                }

                var go = CreateVobMesh(item.Visual);
                var boundingBox = CalculateBoundingBox(go);
                Object.Destroy(go);

                Bounds[item.Visual] = boundingBox;
            }
        }

        private string GetVobMeshName(IVirtualObject vob)
        {
            return vob.ShowVisual ? vob.Visual!.Name : vob.Name;
        }

        /// <summary>
        /// Important: We skip oCItem, as they're all loaded with CalculateVobtemBounds to fetch them all.
        /// </summary>
        private bool IsVobWithMesh(VirtualObjectType type)
        {
            switch (type)
            {
                case VirtualObjectType.Unknown:
                    return true;
                default:
                    return false;
            }
        }

        private GameObject CreateVobMesh(string meshName)
        {
            // MDL
            var mdl = ResourceLoader.TryGetModel(meshName);
            if (mdl != null)
            {
                return MeshFactory.CreateVob(meshName, mdl, useTextureArray: false);
            }

            // MDH+MDM (without MDL as wrapper)
            var mdh = ResourceLoader.TryGetModelHierarchy(meshName);
            var mdm = ResourceLoader.TryGetModelMesh(meshName);
            if (mdh != null && mdm != null)
            {
                return MeshFactory.CreateVob(meshName, mdm, mdh, useTextureArray: false);
            }

            // MMB
            var mmb = ResourceLoader.TryGetMorphMesh(meshName);
            if (mmb != null)
            {
                return MeshFactory.CreateVob(meshName, mmb);
            }

            // MRM
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(meshName);
            if (mrm != null)
            {
                return MeshFactory.CreateVob(meshName, mrm, withCollider: false);
            }

            Debug.LogError($"No mesh found for >{meshName}<");
            return null;
        }

        private Bounds CalculateBoundingBox(GameObject go)
        {
            try
            {
                return go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return new Bounds();
            }
        }
    }
}
