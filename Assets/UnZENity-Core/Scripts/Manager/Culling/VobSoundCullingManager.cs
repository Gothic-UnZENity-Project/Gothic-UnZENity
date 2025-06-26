using GUZ.Core.Config;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager.Culling
{
    public class VobSoundCullingManager : AbstractCullingManager
    {
        public VobSoundCullingManager(DeveloperConfig config)
        {
            // NOP
        }

        public void AddCullingEntry(VobContainer container)
        {
            if (IsFinalized)
            {
                Logger.LogWarning($"CullingGroup for Sounds closed already. Can't add >{container.Go.name}<", LogCat.Audio);
                return;
            }
            
            Objects.Add(container.Go);
            var sphere = new BoundingSphere(container.Go.transform.position, container.VobAs<ISound>().Radius / 100f); // Gothic's values are in cm, Unity's in m.
            TempSpheres.Add(sphere);
        }

        /// <summary>
        /// We only check for distance band 0 - visible, and 0 - invisible (or to be more precise here: audible/inaudible)
        /// </summary>
        protected override void VisibilityChanged(CullingGroupEvent evt)
        {
            // A higher distance level means "inaudible" as we only leverage: 0 -> in-range; 1 -> out-of-range.
            var inAudibleRange = evt.currentDistance == 0;
            var go = Objects[evt.index];
            
            go.SetActive(inAudibleRange);
            
            if (inAudibleRange)
                GameGlobals.Vobs.InitVob(go);
        }

        /// <summary>
        /// Set main camera once world is loaded fully.
        /// Doesn't work at loading time as we change scenes etc.
        /// </summary>
        protected override void PostWorldCreate()
        {
            base.PostWorldCreate();

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
