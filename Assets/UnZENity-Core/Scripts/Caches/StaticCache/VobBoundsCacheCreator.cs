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
using ZenKit;
using ZenKit.Vobs;
using Object = UnityEngine.Object;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Caches.StaticCache
{
    public class VobBoundsCacheCreator
    {
        public Dictionary<string, Bounds> Bounds { get; } = new();


        public void CalculateVobBounds(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                // We ignore oCItem for now as we will load them all in one afterward.
                // We also calculate bounds only for objects which are marked to be cached inside Constants.
                if (vob.Type == VirtualObjectType.oCItem || !Constants.StaticCacheVobTypes.Contains(vob.Type))
                {
                    // Check children
                    CalculateVobBounds(vob.Children);
                    continue;
                }

                var visualName = vob.GetVisualName();

                // Already cached
                if (Bounds.ContainsKey(visualName))
                {
                    continue;
                }

                Bounds boundingBox;
                switch (vob.Visual!.Type)
                {
                    case VisualType.Decal:
                        // FIXME - We can easily calculate bbox via position (0,0,0) + radius of decal.
                        var go = MeshFactory.CreateVobDecal(vob, (VisualDecal)vob.Visual);
                        boundingBox = CalculateBoundingBox(go);
                        Object.Destroy(go);
                        break;
                    case VisualType.ParticleEffect:
                        // FIXME - We can easily calculate bbox via position (0,0,0) + radius of emitting pfx.
                        var go2 = MeshFactory.CreateVobPfx(vob, parent: null);
                        boundingBox = CalculateBoundingBox(go2);
                        Object.Destroy(go2);
                        break;
                    case VisualType.Unknown:
                        continue; // Skip this Vob as it's only a placeholder with no data.
                    default:
                        // FIXME - e.g. OC_MOB_CAULDRON has vob-children like MAGICPOTIONSMOKE.pfx and TORCH.pfx inside.
                        //         The correct bounds would need to include these elements to be checked.
                        // We load BoundingBox directly now.
                        // go = CreateVobMesh(visualName);
                        boundingBox = CalculateBoundingBox(vob.Visual.Type, vob.Visual.Name);
                        break;
                }


                // FIXME - We can easily calculate if the BBOX from ZenKit is applied correctly if we compare it with a GameObject's bbox.
                // FIXME - Move from Go.MeshFilters.bboxes towards ZenKit.Bboxes 2024-12. Can be safely removed at a later time.
                // if (boundingBoxNew != default)
                // {
                //     // calculate if two Bounds are similar with the first 4 digits after zero.
                //     if (Math.Abs(boundingBox.center.x - boundingBoxNew.center.x) > 0.1 ||
                //         Math.Abs(boundingBox.center.y - boundingBoxNew.center.y) > 0.1 ||
                //         Math.Abs(boundingBox.center.z - boundingBoxNew.center.z) > 0.1 ||
                //         Math.Abs(boundingBox.size.x - boundingBoxNew.size.x) > 0.1 ||
                //         Math.Abs(boundingBox.size.y - boundingBoxNew.size.y) > 0.1 ||
                //         Math.Abs(boundingBox.size.z - boundingBoxNew.size.z) > 0.1)
                //     {
                //         Debug.LogError("Bounds aren't matching.");
                //
                //         // Call to recalculate via debugger.
                //         CalculateBoundingBoxNew(vob);
                //     }
                // }


                Bounds[visualName] = boundingBox;

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

                Bounds boundingBox;

                boundingBox = CalculateBoundingBox(VisualType.MultiResolutionMesh, item.Visual);

                // 99% of items are of mesh type MRM. For all the others, let's try other options.
                if (boundingBox == default)
                {
                    boundingBox = CalculateBoundingBox(VisualType.MorphMesh, item.Visual);

                    // In G1 it is only ITLSTORCHBURNING.ZEN --> RootObjects[0].Visual=ITLS_TORCHBURNED_01.3DS
                    if (item.Visual.EndsWith(".ZEN"))
                    {
                        var world = ResourceLoader.TryGetWorld(item.Visual, GameContext.GameVersionAdapter.Version);
                        if (world!.RootObjects.Count != 1)
                        {
                            Debug.LogError($"Bounds for {item.Visual} couldn't be calculated correctly as focussing on 1 G1 object as of now.");
                        }

                        // FIXME - This Vob has 2 children. One is a Pfx! We would need to calculate these bounds as well.
                        var firstWorldVisual = world.RootObjects.First().Visual;
                        boundingBox = CalculateBoundingBox(firstWorldVisual!.Type, firstWorldVisual.Name);
                    }
                }

                if (boundingBox == default)
                {
                    // Re-enable if you want to check which meshes couldn't be found.
                    // Debug.LogError($"ItemVisual {item.Visual} is neither .mrm, .zen, nor .mmb.");
                    continue;
                }

                Bounds[item.Visual] = boundingBox;
            }
        }

        private Bounds CalculateBoundingBox(VisualType visualType, string visualName)
        {
            Bounds bounds = default;

            switch (visualType)
            {
                case VisualType.Mesh:
                    var msh = ResourceLoader.TryGetMesh(visualName);

                    if (msh == null)
                    {
                        return default;
                    }

                    bounds = GetBoundsByOrientedBbox(msh.OrientedBoundingBox);
                    break;
                case VisualType.MultiResolutionMesh:
                    var mrm = ResourceLoader.TryGetMultiResolutionMesh(visualName);

                    if (mrm == null)
                    {
                        return default;
                    }

                    bounds = GetBoundsByOrientedBbox(mrm.OrientedBoundingBox);
                    break;
               case VisualType.Model:
                    var mdl = ResourceLoader.TryGetModel(visualName);

                    if (mdl == null)
                    {
                        return default;
                    }

                    foreach (var mesh in mdl.Mesh.Meshes)
                    {
                        bounds.Encapsulate(GetBoundsByOrientedBbox(mesh.Mesh.OrientedBoundingBox));
                    }

                    foreach (var attachment in mdl.Mesh.Attachments)
                    {
                        bounds.Encapsulate(GetBoundsByOrientedBbox(attachment.Value.OrientedBoundingBox));
                    }

                    break;
                case VisualType.MorphMesh:
                    var mmb = ResourceLoader.TryGetMorphMesh(visualName);

                    if (mmb == null)
                    {
                        return default;
                    }

                    bounds = GetBoundsByOrientedBbox(mmb.Mesh.OrientedBoundingBox);
                    break;
                case VisualType.ParticleEffect:
                case VisualType.Camera:
                case VisualType.Unknown:
                case VisualType.Decal:
                default:
                    throw new ArgumentOutOfRangeException(visualType.ToString());
            }

            return bounds;
        }

        private const int _axes123 = 0b100010001;
        private const int _axes132 = 0b100001010;
        private const int _axes213 = 0b010100001;
        private const int _axes231 = 0b010001100;
        private const int _axes312 = 0b001100010;
        private const int _axes321 = 0b001010100;


        private Bounds GetBoundsByOrientedBbox(IOrientedBoundingBox bbox)
        {
            var center = new UnityEngine.Vector3(bbox.Center.X / 100, bbox.Center.Y / 100, bbox.Center.Z / 100);
            var axesBitMask = GetTupleBitMask(bbox.Axes);
            UnityEngine.Vector3 size = default;

            switch (axesBitMask)
            {
                case _axes123:
                    size = new UnityEngine.Vector3(bbox.HalfWidth.X / 100, bbox.HalfWidth.Y / 100, bbox.HalfWidth.Z / 100) * 2;
                    break;
                case _axes231:
                    size = new UnityEngine.Vector3(bbox.HalfWidth.Z / 100, bbox.HalfWidth.X / 100, bbox.HalfWidth.Y / 100) * 2;
                    break;
                case _axes312:
                    size = new UnityEngine.Vector3(bbox.HalfWidth.Y / 100, bbox.HalfWidth.Z / 100, bbox.HalfWidth.X / 100) * 2;
                    break;
                default:
                    throw new ArgumentException($"axesBitMask >{Convert.ToString(axesBitMask, 2).PadLeft(9, '0')}< not yet implemented.");
            }

            return new Bounds(center, size);
        }

        /// <summary>
        /// Simply one way to get a T{v3,v3,v3} into something which can be compared by a switch-int statement.
        /// </summary>
        private static int GetTupleBitMask(Tuple<Vector3, Vector3, Vector3> axes)
        {
            // e.g. v3(1,0,0), v3(0,1,0), v3(0,0,1) --> 0b100_010_001
            return GetVectorBitMask(axes.Item1) << 6 |
                   GetVectorBitMask(axes.Item2) << 3 |
                   GetVectorBitMask(axes.Item3);
        }

        /// <summary>
        /// Simply one way to get a v3 into something which can be compared by a switch-int statement.
        /// </summary>
        private static int GetVectorBitMask(Vector3 v)
        {
            // e.g. v3(1,0,0) --> 0b100 (We use x as most significant bit like we are reading it as Vector3(x,y,z)
            return (v.X == 1 ? 1 : 0) << 2 |  // Bit 3 (100)
                   (v.Y == 1 ? 1 : 0) << 1 |  // Bit 2 (010)
                   (v.Z == 1 ? 1 : 0);        // Bit 1 (001)
        }


        [Obsolete("2024-12 - Used for Decals and Pfx but should be removed as the bbox can be created from ZenKit data. No need to create from GO.")]
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
                    return new Bounds(UnityEngine.Vector3.zero, decalProjector.size);
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
