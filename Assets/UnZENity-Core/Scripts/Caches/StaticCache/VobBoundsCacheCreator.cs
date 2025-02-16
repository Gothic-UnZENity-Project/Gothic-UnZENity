using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ZenKit;
using ZenKit.Vobs;
using Object = UnityEngine.Object;

namespace GUZ.Core.Caches.StaticCache
{
    public class VobBoundsCacheCreator
    {
        public Dictionary<string, Bounds> Bounds { get; } = new();


        public async Task CalculateVobBounds(List<IVirtualObject> vobs)
        {
            foreach (var vob in vobs)
            {
                await FrameSkipper.TrySkipToNextFrame();

                // We ignore oCItem for now as we will load them all in one afterward.
                // We also calculate bounds only for objects which are marked to be cached inside Constants.
                if (vob.Type == VirtualObjectType.oCItem || !Constants.StaticCacheVobTypes.Contains(vob.Type))
                {
                    // Check children
                    await CalculateVobBounds(vob.Children);
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

                Bounds[visualName] = boundingBox;

                await CalculateVobBounds(vob.Children);
            }
        }

        /// <summary>
        /// As there might be VOBs which aren't in a new game, but when gamers load a save game,
        /// we need to calculate bounds for all! items.
        /// </summary>
        public async Task CalculateVobItemBounds()
        {
            var allItems = GameData.GothicVm.GetInstanceSymbols("C_Item");

            foreach (var obj in allItems)
            {
                await FrameSkipper.TrySkipToNextFrame();

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
                    IModelMesh mdm;
                    if (mdl != null)
                    {
                        mdm = mdl.Mesh;
                    }
                    else
                    {
                        // Some models miss their wrapping .mdl file. We therefore load the .mdm file (with same name) directly.
                        mdm = ResourceLoader.TryGetModelMesh(visualName);
                    }

                    foreach (var mesh in mdm!.Meshes)
                    {
                        bounds.Encapsulate(GetBoundsByOrientedBbox(mesh.Mesh.OrientedBoundingBox));
                    }

                    foreach (var attachment in mdm.Attachments)
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

        private enum AxesType
        {
            TypeUnknown,
            Type123 = 0b100010001,
            Type231 = 0b010001100,
            Type312 = 0b001100010
        }

        private Bounds GetBoundsByOrientedBbox(IOrientedBoundingBox bbox)
        {
            var center = new Vector3(bbox.Center.X / 100, bbox.Center.Y / 100, bbox.Center.Z / 100);
            AxesType axesType = AxesType.TypeUnknown;

            if (bbox.Axes.Item1.X == 1f && bbox.Axes.Item2.Y == 1f && bbox.Axes.Item3.Z == 1f)
            {
                axesType = AxesType.Type123;
            }
            else if (bbox.Axes.Item1.Y == 1f && bbox.Axes.Item2.Z == 1f && bbox.Axes.Item3.X == 1f)
            {
                axesType = AxesType.Type231;
            }
            else if (bbox.Axes.Item1.Z == 1f && bbox.Axes.Item2.X == 1f && bbox.Axes.Item3.Y == 1f)
            {
                axesType = AxesType.Type312;
            }

            Vector3 size;
            switch (axesType)
            {
                case AxesType.Type123:
                    size = new Vector3(bbox.HalfWidth.X / 100, bbox.HalfWidth.Y / 100, bbox.HalfWidth.Z / 100) * 2;
                    break;
                case AxesType.Type231:
                    size = new Vector3(bbox.HalfWidth.Z / 100, bbox.HalfWidth.X / 100, bbox.HalfWidth.Y / 100) * 2;
                    break;
                case AxesType.Type312:
                    size = new Vector3(bbox.HalfWidth.Y / 100, bbox.HalfWidth.Z / 100, bbox.HalfWidth.X / 100) * 2;
                    break;
                default:
                    Debug.LogError($"AxesType {bbox.Axes.Item1}/{bbox.Axes.Item2}/{bbox.Axes.Item3} not yet handled.");
                    return default;
            }

            return new Bounds(center, size);
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
