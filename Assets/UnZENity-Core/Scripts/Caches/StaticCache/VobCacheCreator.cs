using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
                // We ignore oCItem for now as we will load them all in once afterward.
                if (vob.Type != VirtualObjectType.oCItem && Constants.StaticCacheVobTypes.Contains(vob.Type))
                {
                    var visualName = vob.GetVisualName();

                    // Already cached
                    if (Bounds.ContainsKey(visualName))
                    {
                        continue;
                    }

                    GameObject go;
                    switch (vob.Visual!.Type)
                    {
                        case VisualType.Decal:
                            go = MeshFactory.CreateVobDecal(vob, (VisualDecal)vob.Visual);
                            break;
                        case VisualType.ParticleEffect:
                            go = MeshFactory.CreateVobPfx(vob);
                            break;
                        default:
                            go = CreateVobMesh(visualName);
                            break;
                    }

                    if (go == null)
                    {
                        continue;
                    }

                    var boundingBox = CalculateBoundingBox(go);
                    Object.Destroy(go);

                    Bounds[visualName] = boundingBox;
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

                if (go == null)
                {
                    continue;
                }

                var boundingBox = CalculateBoundingBox(go);
                Object.Destroy(go);

                Bounds[item.Visual] = boundingBox;
            }
        }

        private GameObject CreateVobMesh(string visualName)
        {
            // MDL
            var mdl = ResourceLoader.TryGetModel(visualName);
            if (mdl != null)
            {
                return MeshFactory.CreateVob(visualName, mdl, useTextureArray: false);
            }

            // MDH+MDM (without MDL as wrapper)
            var mdh = ResourceLoader.TryGetModelHierarchy(visualName);
            var mdm = ResourceLoader.TryGetModelMesh(visualName);
            if (mdh != null && mdm != null)
            {
                return MeshFactory.CreateVob(visualName, mdm, mdh, useTextureArray: false);
            }

            // MMB
            var mmb = ResourceLoader.TryGetMorphMesh(visualName);
            if (mmb != null)
            {
                return MeshFactory.CreateVob(visualName, mmb, useTextureArray: false);
            }

            // MRM
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(visualName);
            if (mrm != null)
            {
                return MeshFactory.CreateVob(visualName, mrm, withCollider: false, useTextureArray: false);
            }

            // e.g. ITLSTORCHBURNING.ZEN
            // Debug.LogError($"No mesh found for >{meshName}<");
            return null;
        }

        private Bounds CalculateBoundingBox(GameObject go)
        {
            try
            {
                // As we store renderer.bounds, we will get world space, but the object is at 0,0,0 and identity, we have local space again. ;-)
                if (go.TryGetComponent<ParticleSystemRenderer>(out var particleRenderer))
                {
                    return particleRenderer.bounds;
                }
                else if (go.TryGetComponent<DecalProjector>(out var decalProjector))
                {
                    return new Bounds(Vector3.zero, decalProjector.size);
                }
                else
                {
                    var meshFilters = go.GetComponentsInChildren<MeshFilter>();

                    switch (meshFilters.Length)
                    {
                        case 0:
                            return default;
                        case 1:
                            return meshFilters.First().sharedMesh.bounds;
                        default:
                            var finalBounds = new Bounds();
                            meshFilters.ForEach(i => finalBounds.Encapsulate(i.sharedMesh.bounds));
                            return finalBounds;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return default;
            }
        }
    }
}
