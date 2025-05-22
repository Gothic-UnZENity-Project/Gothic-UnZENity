using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Config;
using GUZ.Core.Debugging;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager.Culling
{
    /// <summary>
    /// CullingGroups are a way for objects inside a scene to be handled by frustum culling and occlusion culling.
    /// With this set up, VOBs are handled by camera's view and culling behaviour, so that the StateChanged() event disables/enables VOB GameObjects.
    /// @see https://docs.unity3d.com/Manual/CullingGroupAPI.html
    /// </summary>
    public class VobMeshCullingManager
    {
        // Stored for resetting after world switch
        private CullingGroup _vobCullingGroupSmall;
        private CullingGroup _vobCullingGroupMedium;
        private CullingGroup _vobCullingGroupLarge;

        // Stored for later index mapping SphereIndex => GOIndex
        private readonly List<GameObject> _vobObjectsSmall = new();
        private readonly List<GameObject> _vobObjectsMedium = new();
        private readonly List<GameObject> _vobObjectsLarge = new();

        // Stored for later position updates for moved Vobs
        private BoundingSphere[] _vobSpheresSmall;
        private BoundingSphere[] _vobSpheresMedium;
        private BoundingSphere[] _vobSpheresLarge;

        // Do not trigger FC or OC while in that range of an object.
        private const float _gracePeriodCullingDistance = 5f;

        private enum VobList
        {
            Small,
            Medium,
            Large
        }

        // Grabbed Vobs will be ignored from Culling until Grabbing stopped and velocity = 0
        private Dictionary<GameObject, Tuple<VobList, int>> _pausedVobs = new();
        private Dictionary<GameObject, Rigidbody> _pausedVobsToReenable = new();
        private Dictionary<GameObject, Coroutine> _pausedVobsToReenableCoroutine = new();

        private readonly ICoroutineManager _coroutineManager;
        private readonly bool _featureEnableCulling;
        private readonly bool _featureDrawGizmos;
        private readonly MeshCullingGroup _featureSmallCullingGroup;
        private readonly MeshCullingGroup _featureMediumCullingGroup;
        private readonly MeshCullingGroup _featureLargeCullingGroup;

        public VobMeshCullingManager(DeveloperConfig config, ICoroutineManager coroutineManager)
        {
            _coroutineManager = coroutineManager;
            _featureEnableCulling = config.EnableVOBMeshCulling;
            _featureDrawGizmos = config.ShowVOBMeshCullingGizmos;
            _featureSmallCullingGroup = config.SmallVOBMeshCullingGroup;
            _featureMediumCullingGroup = config.MediumVOBMeshCullingGroup;
            _featureLargeCullingGroup = config.LargeVOBMeshCullingGroup;
        }

        public void Init()
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(PreWorldCreate);
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldCreate);

            // Unity demands CullingGroups to be created in Awake() or Start() earliest.
            _vobCullingGroupSmall = new CullingGroup();
            _vobCullingGroupMedium = new CullingGroup();
            _vobCullingGroupLarge = new CullingGroup();

            _coroutineManager.StartCoroutine(StopVobTrackingBasedOnVelocity());
        }

        /// <summary>
        /// This method will only be called within EditorMode. It's tested to not being executed within Standalone mode.
        /// </summary>
        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !_featureDrawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(.9f, 0, 0);
            if (_vobSpheresSmall != null)
            {
                for (var i = 0; i < _vobSpheresSmall.Length; i++)
                {
                    if (_vobObjectsSmall[i].TryGetComponent(out VobCullingGizmo gizmoComp) && gizmoComp.ActivateGizmo)
                    {
                        Gizmos.DrawWireSphere(_vobSpheresSmall[i].position, _vobSpheresSmall[i].radius);
                    }
                }
            }

            Gizmos.color = new Color(.5f, 0, 0);
            if (_vobSpheresMedium != null)
            {
                for (var i = 0; i < _vobSpheresMedium.Length; i++)
                {
                    if (_vobObjectsMedium[i].TryGetComponent(out VobCullingGizmo gizmoComp) && gizmoComp.ActivateGizmo)
                    {
                        Gizmos.DrawWireSphere(_vobSpheresMedium[i].position, _vobSpheresMedium[i].radius);
                    }
                }

            }

            Gizmos.color = new Color(.2f, 0, 0);
            if (_vobSpheresLarge != null)
            {
                for (var i = 0; i < _vobSpheresLarge.Length; i++)
                {
                    if (_vobObjectsLarge[i].TryGetComponent(out VobCullingGizmo gizmoComp) && gizmoComp.ActivateGizmo)
                    {
                        Gizmos.DrawWireSphere(_vobSpheresLarge[i].position, _vobSpheresLarge[i].radius);
                    }
                }
            }
        }

        private void PreWorldCreate()
        {
            _vobCullingGroupSmall.Dispose();
            _vobCullingGroupMedium.Dispose();
            _vobCullingGroupLarge.Dispose();

            _vobCullingGroupSmall = new CullingGroup();
            _vobCullingGroupMedium = new CullingGroup();
            _vobCullingGroupLarge = new CullingGroup();

            _vobObjectsSmall.Clear();
            _vobObjectsMedium.Clear();
            _vobObjectsLarge.Clear();

            _vobSpheresSmall = null;
            _vobSpheresMedium = null;
            _vobSpheresLarge = null;

            _pausedVobs.Clear();
            _pausedVobsToReenable.Clear();
            _pausedVobsToReenableCoroutine.Clear();
        }

        private void VobSmallChanged(CullingGroupEvent evt)
        {
            VobChanged(evt, _vobObjectsSmall, VobList.Small);
        }

        private void VobMediumChanged(CullingGroupEvent evt)
        {
            VobChanged(evt, _vobObjectsMedium, VobList.Medium);
        }

        private void VobLargeChanged(CullingGroupEvent evt)
        {
            VobChanged(evt, _vobObjectsLarge, VobList.Large);
        }

        /// <summary>
        /// Band explanation:
        /// Band 0 - (0m...~5m)   - Grace period where a VOB won't get culled (no FC, no OC) - e.g. to ensure a ladder is still in our hands or a light behind us still shines.
        /// Band 1 - (5m...~100m) - Frustum Culling (FC) and Occlusion Culling (OC) will happen - ensure proper performance for culled out, unseen objects.
        /// Band 2 - [~100m-∞)    - Items inside last band will always be culled out.
        /// </summary>
        private void VobChanged(CullingGroupEvent evt, List<GameObject> vobObjects, VobList vobListType)
        {
            var pausedVobs = _pausedVobs
                .Where(i => i.Value.Item1 == vobListType)
                .Select(i => i.Value.Item2);

            if (pausedVobs.Contains(evt.index))
            {
                return;
            }

            var go = vobObjects[evt.index];

            switch (evt.currentDistance)
            {
                case 0: // grace period band - ignore FC and OC, plainly enable the vob!
                    vobObjects[evt.index].SetActive(true);
                    GameGlobals.Vobs.InitVob(go);
                    break;
                default:
                    var setActive = evt.hasBecomeVisible || (evt.isVisible && !evt.hasBecomeInvisible);
                    vobObjects[evt.index].SetActive(setActive);

                    if (setActive)
                    {
                        GameGlobals.Vobs.InitVob(go);
                    }

                    break;
            }
        }

        /// <summary>
        /// Fill CullingGroups with GOs based on size (radius), position, and object size (small/medium/large)
        /// </summary>
        public void PrepareVobCulling(List<GameObject> objects)
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            // FIXME - As the VOBs aren't loaded yet, we need to fetch the LocalBounds from a cache which needs to be created before the game starts.

            var smallDim = _featureSmallCullingGroup.MaximumObjectSize;
            var mediumDim = _featureMediumCullingGroup.MaximumObjectSize;
            var spheresSmall = new List<BoundingSphere>();
            var spheresMedium = new List<BoundingSphere>();
            var spheresLarge = new List<BoundingSphere>();

            foreach (var obj in objects)
            {
                if (!obj)
                {
                    continue;
                }

                // FIXME - Particles (like leaves in the forest) will be handled like big vobs, but could potentially
                // FIXME - be handled as small ones as leaves shouldn't be visible from 100 of meters away.
                var bounds = GetLocalBounds(obj);
                if (!bounds.HasValue)
                {
                    // e.g. ITMICELLO which has no mesh and therefore no cached Bounds.
                    // Logger.LogError($"Couldn't find mesh for >{obj}< to be used for CullingGroup. Skipping...");
                    continue;
                }

                var sphere = GetSphere(obj, bounds.Value);
                var size = sphere.radius * 2;

                if (size <= smallDim)
                {
                    _vobObjectsSmall.Add(obj);
                    spheresSmall.Add(sphere);
                }
                else if (size <= mediumDim)
                {
                    _vobObjectsMedium.Add(obj);
                    spheresMedium.Add(sphere);
                }
                else
                {
                    _vobObjectsLarge.Add(obj);
                    spheresLarge.Add(sphere);
                }
            }

            _vobCullingGroupSmall.onStateChanged = VobSmallChanged;
            _vobCullingGroupMedium.onStateChanged = VobMediumChanged;
            _vobCullingGroupLarge.onStateChanged = VobLargeChanged;

            _vobCullingGroupSmall.SetBoundingDistances(new[] { _gracePeriodCullingDistance, _featureSmallCullingGroup.CullingDistance });
            _vobCullingGroupMedium.SetBoundingDistances(new[] { _gracePeriodCullingDistance, _featureMediumCullingGroup.CullingDistance });
            _vobCullingGroupLarge.SetBoundingDistances(new[] { _gracePeriodCullingDistance, _featureLargeCullingGroup.CullingDistance });

            _vobSpheresSmall = spheresSmall.ToArray();
            _vobSpheresMedium = spheresMedium.ToArray();
            _vobSpheresLarge = spheresLarge.ToArray();

            _vobCullingGroupSmall.SetBoundingSpheres(_vobSpheresSmall);
            _vobCullingGroupMedium.SetBoundingSpheres(_vobSpheresMedium);
            _vobCullingGroupLarge.SetBoundingSpheres(_vobSpheresLarge);
        }

        private BoundingSphere GetSphere(GameObject go, Bounds bounds)
        {
            var bboxSize = bounds.size;
            var worldCenter = go.transform.TransformPoint(bounds.center);

            // Get the biggest dimension for calculation of object size group.
            var maxDimension = Mathf.Max(bboxSize.x, bboxSize.y, bboxSize.z);
            var sphere = new BoundingSphere(worldCenter, maxDimension / 2); // Radius is half the size.

            return sphere;
        }

        /// <summary>
        /// Fetch Mesh Bounds which are in local space. We will later "move" the bbox to the current world space.
        ///
        /// TODO If performance allows it, we could also look dynamically for all the existing meshes inside GO
        /// TODO and look for maximum value for largest mesh. But it should be fine for now.
        /// </summary>
        private Bounds? GetLocalBounds(GameObject go)
        {
            go.TryGetComponent(out VobLoader loaderComp);

            if (loaderComp == null)
            {
                Logger.LogError($"Couldn't find VobLoader for >{go}< to be used for CullingGroup. Skipping...", LogCat.Mesh);
                return null;
            }

            var vob = loaderComp.Vob;

            var totalBounds = new Bounds();
            AddLocalBounds(vob, ref totalBounds);

            return totalBounds == default ? null : totalBounds;
        }

        /// <summary>
        /// VOBs can contain child-VOBs which might be Particles, Lights, etc.
        /// We therefore need to sum up the overall Bounds to ensure Culling kicks in correctly.
        /// </summary>
        private void AddLocalBounds(IVirtualObject vob, ref Bounds totalBounds)
        {
            Bounds additionalBounds = default;

            switch (vob.Type)
            {
                case VirtualObjectType.zCVobLight:
                    additionalBounds = GetLocalLightBounds((ILight)vob);
                    break;
                default:
                    switch (vob.Visual.Type)
                    {
                        // We don't support Decal and Pfx so far.
                        case VisualType.Decal:
                        case VisualType.ParticleEffect:
                            break;
                        default:
                            additionalBounds = GetLocalMeshBounds(vob);
                            break;
                    }
                    break;
            }

            totalBounds.Encapsulate(additionalBounds);

            foreach (var childVob in vob.Children)
            {
                AddLocalBounds(childVob, ref totalBounds);
            }

            // Fire VOBs children are inside a .zen file
            if (vob.Type == VirtualObjectType.oCMobFire)
            {
                var fireWorld = ResourceLoader.TryGetWorld(((IFire)vob).VobTree, GameContext.GameVersionAdapter.Version, true);

                // e.g. "NC_FIREPLACE_STONE" has no VobTree. But could we potentially render it as mesh?
                if (fireWorld == null)
                {
                    return;
                }

                foreach (var childFireVob in fireWorld!.RootObjects)
                {
                    AddLocalBounds(childFireVob, ref totalBounds);
                }
            }
        }

        private Bounds GetLocalLightBounds(ILight light)
        {
            // FIXME - #1 - Lights shine for the whole mesh they belong to again. :-/
            // FIXME - #2 - When inside a light range, turning to our back will disable the light.
            return new Bounds(Vector3.zero, Vector3.one * light.Range / 100 * 2);
        }

        // TODO - Not yet implemented.
        private Bounds? GetLocalParticleBounds(EventParticleEffect particle)
        {
            return null;
        }

        private Bounds GetLocalMeshBounds(IVirtualObject vob)
        {
            string meshName;
            switch (vob.Type)
            {
                case VirtualObjectType.oCItem:
                    var item = VmInstanceManager.TryGetItemData(vob.Name);
                    meshName = item?.Visual;

                    // e.g. ITMICELLO has no mesh
                    if (meshName == null)
                    {
                        return default;
                    }

                    break;
                default:
                    meshName = vob.Visual?.Name ?? vob.Name;
                    break;
            }

            if (meshName.IsNullOrEmpty())
            {
                return default;
            }

            // FIXME - More magic needed. For objects with Mesh, we can store it. But for e.g. Lights, we need to fetch it from the actual
            // FIXME - VOB.data.radius etc. --> aka more IF-options on how to calculate local bounds
            if (GameGlobals.StaticCache.LoadedVobsBounds.TryGetValue(meshName, out var bounds))
            {
                return bounds;
            }
            else
            {
                // We can carefully disable this log as some elements aren't cached.
                // e.g. when there is no texture like for OC_DECORATE_V4.3DS
                Logger.LogError($"Couldn't find mesh bounds information from StaticCache for >{meshName}<.", LogCat.Mesh);
                return default;
            }
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        private void PostWorldCreate()
        {
            foreach (var group in new[] { _vobCullingGroupSmall, _vobCullingGroupMedium, _vobCullingGroupLarge })
            {
                var mainCamera = Camera.main!;
                group.targetCamera = mainCamera; // Needed for FrustumCulling and OcclusionCulling to work.
                group.SetDistanceReferencePoint(mainCamera.transform); // Needed for BoundingDistances to work.
            }
        }

        public void StartTrackVobPositionUpdates(GameObject go)
        {
            // Meshes are always 1...n levels below initially created VobLoader GO. Therefore, we need to fetch its parent for track updates.
            var rootGo = go.GetComponentInParent<VobLoader>().gameObject;
            
            CancelStopTrackVobPositionUpdates(rootGo);

            // Entry is already in list
            if (_pausedVobs.ContainsKey(rootGo))
            {
                return;
            }

            // Check Small list
            var index = _vobObjectsSmall.IndexOf(rootGo);
            var vobType = VobList.Small;
            
            // Check Medium list
            if (index == -1)
            {
                index = _vobObjectsMedium.IndexOf(rootGo);
                vobType = VobList.Medium;
            }

            // Check Large list
            if (index == -1)
            {
                index = _vobObjectsLarge.IndexOf(rootGo);
                vobType = VobList.Large;
            }

            if (index == -1)
                Logger.LogError($"Couldn't find object in Culling list {rootGo.name}. Culling updates will break.", LogCat.Vob);

            _pausedVobs.Add(rootGo, new Tuple<VobList, int>(vobType, index));
        }

        /// <summary>
        /// If we execute Start() and Stop() during a short time frame, we need to cancel all the "stop" features.
        /// e.g. If we start grabbing it while it's still in release-stop mode, we cancel delay Coroutine and loop itself.
        /// </summary>
        private void CancelStopTrackVobPositionUpdates(GameObject rootGo)
        {
            if (_pausedVobsToReenableCoroutine.ContainsKey(rootGo))
            {
                _coroutineManager.StopCoroutine(_pausedVobsToReenableCoroutine[rootGo]);
                _pausedVobsToReenableCoroutine.Remove(rootGo);
            }

            if (_pausedVobsToReenable.ContainsKey(rootGo))
            {
                _pausedVobsToReenable.Remove(rootGo);
            }
        }
        
        /// <summary>
        /// When we release an item from our hands, we need to wait a few frames before the velocity of the object is != 0.
        /// Therefore, we put the object into the list delayed.
        /// </summary>
        public void StopTrackVobPositionUpdates(GameObject go)
        {
            // Meshes are always 1...n levels below initially created VobLoader GO. Therefore, we need to fetch its parent for track updates.
            var rootGo = go.GetComponentInParent<VobLoader>().gameObject;
            
            if (_pausedVobsToReenableCoroutine.ContainsKey(rootGo))
            {
                return;
            }

            _pausedVobsToReenableCoroutine.Add(rootGo,
                _coroutineManager.StartCoroutine(StopTrackVobPositionUpdatesDelayed(rootGo)));
        }

        /// <summary>
        /// When we release an item from our hands, we need to wait a few frames before the velocity of the object is != 0.
        /// Therefore, we put the object into the list delayed.
        /// </summary>
        private IEnumerator StopTrackVobPositionUpdatesDelayed(GameObject rootGo)
        {
            yield return new WaitForSeconds(1f);
            _pausedVobsToReenableCoroutine.Remove(rootGo);
            if (!_pausedVobsToReenable.ContainsKey(rootGo))
            {
                _pausedVobsToReenable.Add(rootGo, rootGo.GetComponentInChildren<Rigidbody>());
            }
        }

        /// <summary>
        /// Iterate over all currently non-kinematic (physical) items (e.g. after grab stopped).
        /// We then look if their velocity is zero to:
        /// 1. update culling position once
        /// 2. stop physics again
        /// </summary>
        private IEnumerator StopVobTrackingBasedOnVelocity()
        {
            while (true)
            {
                for (var i = _pausedVobsToReenable.Keys.Count - 1; i >= 0; i--)
                {
                    var key = _pausedVobsToReenable.Keys.ElementAt(i);
                    var rigidBody = _pausedVobsToReenable[key];
                    if (rigidBody.velocity != Vector3.zero)
                    {
                        continue;
                    }

                    UpdateSpherePosition(key);
                    rigidBody.isKinematic = true;

                    _pausedVobs.Remove(key);
                    _pausedVobsToReenable.Remove(key);
                }

                yield return null;
            }
        }

        private void UpdateSpherePosition(GameObject go)
        {
            var grabbed = _pausedVobs[go];
            var vobType = grabbed.Item1;
            var index = grabbed.Item2;

            // We need to find the GO's correlated Sphere in the right VobArray.
            var sphereList = vobType switch
            {
                VobList.Small => _vobSpheresSmall,
                VobList.Medium => _vobSpheresMedium,
                VobList.Large => _vobSpheresLarge,
                _ => throw new ArgumentOutOfRangeException()
            };

            sphereList[index].position = go.transform.position;
        }

        public void Destroy()
        {
            if (!_featureEnableCulling)
            {
                return;
            }

            _vobCullingGroupSmall.Dispose();
            _vobCullingGroupMedium.Dispose();
            _vobCullingGroupLarge.Dispose();
        }
    }
}
