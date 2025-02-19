using System.Collections.Generic;
using GUZ.Core._Npc2;
using GUZ.Core.Config;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class NpcMeshCullingManager : AbstractCullingManager
    {
        private readonly bool _featureEnableCulling;
        private readonly float _featureCullingDistance;

        // Sphere values to track and update when visible NPCs move.
        private BoundingSphere[] _spheres;
        private readonly Dictionary<int, Transform> _visibleNpcs = new();

        public NpcMeshCullingManager(DeveloperConfig config)
        {
            _featureEnableCulling = config.EnableNpcMeshCulling;
            _featureCullingDistance = config.NpcCullingDistance;
        }

        protected override void PreWorldCreate()
        {
            base.PreWorldCreate();
            _spheres = null;
        }


        public override void AddCullingEntry(GameObject go)
        {
            Objects.Add(go);

            // Normally NPC spheres are ~1 meter in radius. But we need to fake the volume, so that Culling always thinks
            // we're "inside" the NPC and Frustum+Occlusion Culling isn't triggered.
            // (@see VobSoundCullingManager where we also use it exactly that way, and it works.)
            var sphere = new BoundingSphere(go.transform.position, _featureCullingDistance);
            TempSpheres.Add(sphere);
        }

        /// <summary>
        /// Set main camera once world is loaded fully. Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected override void PostWorldCreate()
        {
            if (_featureEnableCulling)
            {
                base.PostWorldCreate();

                // For performance reasons, we initially used a List during creation.
                // Now we move to an array which is copied by reference to CullingGroup and can be updated later via Update().
                _spheres = TempSpheres.ToArray();
                CullingGroup.SetBoundingSpheres(_spheres);

                // As we "faked" the volume of NPCs, we will plainly disable them whenever we are out of their volume (aka range).
                CullingGroup.SetBoundingDistances(new[] { 0f });

                CullingGroup.onStateChanged = VisibilityChanged;
            }
            // If we disabled NPC culling, then we need to render them all now!
            else
            {
                Objects.ForEach(obj => obj.SetActive(true));
            }

            // Cleanup
            TempSpheres.ClearAndReleaseMemory();
        }

        protected override void VisibilityChanged(CullingGroupEvent evt)
        {
            var go = Objects[evt.index];

            // A higher distance level means "invisible" as we only leverage: 0 -> in-range; 1 -> out-of-range.
            var isInVisibleRange = evt.currentDistance == 0;
            var wasOutOfDistance = evt.previousDistance != 0;

            var loaderComp = go.GetComponent<NpcLoader2>();
            var npcData = loaderComp.Npc.GetUserData2();
            var isInitialized = loaderComp.IsLoaded;

            if (!isInVisibleRange && isInitialized)
            {
                AnimationCreator.StopAnimation(go);
            }

            go.SetActive(isInVisibleRange);

            // Alter position tracking of NPC
            if (isInVisibleRange)
            {
                GameGlobals.Npcs.InitNpc(go);
                _visibleNpcs.Add(evt.index, go.transform);
            }
            // When an NPC gets invisible, we need to check for their next respawn from their initially spawned position.
            else
            {
                var props = npcData.Properties;

                if (props.RoutineCurrent != null)
                {
                    var spawnedWayPointName = props.RoutineCurrent.Waypoint;
                    var wayNetPoint = WayNetHelper.GetWayNetPoint(spawnedWayPointName);

                    if (wayNetPoint is not null)
                    {
                        UpdatePosition(evt.index, wayNetPoint.Position);
                    }
                }
                _visibleNpcs.Remove(evt.index);
            }

            // If the NPC !wasOutOfDistance (==wasInDistanceAlready), then we spawned our VRPlayer next to the NPC
            // (e.g. from a save game) and we need to go on with the current routine instead of "resetting" the routine.
            // (Which would respawn NPC at a waypoint, which is wrong.)
            if (isInVisibleRange && wasOutOfDistance)
            {
                // If we walked to an NPC in our game, the NPC will be re-enabled and Routines get reset.
                go.GetComponent<AiHandler>().ReEnableNpc();
            }
        }

        /// <summary>
        /// Each frame, we update the visible NPCs' current position.
        /// </summary>
        public void Update()
        {
            foreach (var npc in _visibleNpcs)
            {
                // NpcLoader.NPCRoot is only updated after a walking animation's loop is done.
                // child[0] == NPCRoot/BIP01 -> We need to fetch this one as it's the walking animation root which updates every frame.
                if (npc.Value.childCount > 0)
                {
                    var child = npc.Value.GetChild(0);
                    // It might be, that the NPC is not yet initialized. Therefore wait until the GO structure is fully loaded.
                    if (child.childCount > 0)
                    {
                        UpdatePosition(npc.Key, child.GetChild(0).position);
                    }
                }
            }
        }

        private void UpdatePosition(int sphereKey, Vector3 position)
        {
            _spheres[sphereKey].position = position;
        }
    }
}
