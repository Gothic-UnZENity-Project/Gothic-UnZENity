using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager.Culling
{
    public class VobSoundCullingManager : AbstractCullingManager
    {
        public VobSoundCullingManager(GameConfiguration config)
        {
            // NOP
        }

        public override void AddCullingEntry(GameObject go)
        {
            Objects.Add(go);
            var sphere = new BoundingSphere(go.transform.position, go.GetComponent<AudioSource>().maxDistance);
            TempSpheres.Add(sphere);
        }

        /// <summary>
        /// We only check for distance band 0 - visible, and 0 - invisible (or to be more precise here: audible/inaudible)
        /// </summary>
        protected override void VisibilityChanged(CullingGroupEvent evt)
        {
            // A higher distance level means "inaudible" as we only leverage: 0 -> in-range; 1 -> out-of-range.
            var inAudibleRange = evt.currentDistance == 0;

            Objects[evt.index].SetActive(inAudibleRange);
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected override void PostWorldCreate(GameObject playerGo)
        {
            base.PostWorldCreate(playerGo);

            // Disable sounds if we're leaving the area and therefore last audible location.
            // Hint: As there are non-spatial sounds (always same volume wherever we are),
            // we need to disable the sounds at exactly the spot we are.
            CullingGroup.SetBoundingDistances(new[] { 0f });
            CullingGroup.SetBoundingSpheres(TempSpheres.ToArray());
            CullingGroup.onStateChanged = VisibilityChanged;

            // Cleanup
            TempSpheres.ClearAndReleaseMemory();
        }
    }
}
