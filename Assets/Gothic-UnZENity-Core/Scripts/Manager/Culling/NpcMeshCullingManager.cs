using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class NpcMeshCullingManager : AbstractCullingManager
    {
        private readonly bool _featureEnableCulling;
        private readonly float _featureCullingDistance;


        public NpcMeshCullingManager(GameConfiguration config)
        {
            _featureEnableCulling = config.EnableNpcMeshCulling;
            _featureCullingDistance = config.NpcCullingDistance;
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
        protected override void PostWorldCreate(GameObject playerGo)
        {
            if (_featureEnableCulling)
            {
                base.PostWorldCreate(playerGo);

                // Fill culling information into spawned GOs
                // As we "faked" the volume of NPCs, we will plainly disable them whenever we are out of their volume (aka range).
                CullingGroup.SetBoundingDistances(new[] { 0f });
                CullingGroup.SetBoundingSpheres(TempSpheres.ToArray());
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
            // A higher distance level means "invisible" as we only leverage: 0 -> in-range; 1 -> out-of-range.
            var isInVisibleRange = evt.currentDistance == 0;
            var wasOutOfDistance = evt.previousDistance != 0;

            Objects[evt.index].SetActive(isInVisibleRange);

            // If the NPC !wasOutOfDistance (==wasInDistanceAlready), then we spawned our VRPlayer next to the NPC
            // (e.g. from a save game) and we need to go on with the current routine instead of "resetting" the routine.
            // (Which would respawn NPC at a waypoint, which is wrong.)
            if (isInVisibleRange && wasOutOfDistance)
            {
                // If we walked to an NPC in our game, the NPC will be re-enabled and Routines get reset.
                Objects[evt.index].GetComponent<AiHandler>().ReEnableNpc();
            }
        }
    }
}
