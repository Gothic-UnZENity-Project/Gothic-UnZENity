using GUZ.Core.Config;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using NUnit.Framework;
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
            if (CurrentState != State.Loading)
            {
                Logger.LogWarning($"CullingGroup for Sounds closed already. Can't add >{container.Go.name}<", LogCat.Audio);
                return;
            }

            AddCullingEntry(container.Go, container.VobAs<ISound>());
        }
        
        public void AddCullingEntry(GameObject go, ISound vob)
        {
            AddCullingEntryInternal(go, vob);
        }

        /// <summary>
        ///Logic:
        /// 1. If In World loading state, we add all entries to the list based on rootVob position (e.g., a soundVob directly below levelCompo)
        /// 2. If After Loading, then added entries are subVobs (e.g., Cauldron->Sound) and we enlarge the cullingArray now.
        /// </summary>
        private void AddCullingEntryInternal(GameObject go, ISound vob)
        {
            Objects.Add(go);
            
            // FIXME - First call of VisibilityChanged() always provides visible=false? Is the pos+radius correct?
            var sphere = new BoundingSphere(go.transform.position, vob.Radius / 100f); // Gothic's values are in cm, Unity's in m.
            TempSpheres.Add(sphere);

            if (CurrentState == State.WorldLoaded)
            {
                // Each time we add an entry, we need to recreate the array for the CullingGroup.
                CullingGroup.SetBoundingSpheres(TempSpheres.ToArray());
            }
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
        }
    }
}
