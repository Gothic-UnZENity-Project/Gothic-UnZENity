using System;
using GUZ.Flat;
using UnityEngine;
#if GUZ_HVR_INSTALLED
using GUZ.HVR;
#endif

namespace GUZ.Core.Context
{
    public static class GuzContext
    {
        public static IInteractionAdapter InteractionAdapter { get; private set; }
        public static IDialogAdapter DialogAdapter { get; private set; }

        public enum Controls
        {
            VR,
            Flat
        }

        public static void SetContext(Controls controls)
        {
            switch (controls)
            {
                case Controls.VR:
                    SetVRContext();
                    break;
                case Controls.Flat:
                    SetFlatContext();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controls), controls, null);
            }
        }

        private static void SetFlatContext()
        {
            Debug.Log("Selecting Context: Flat");
            InteractionAdapter = new FlatInteractionAdapter();
        }

        private static void SetVRContext()
        {
#if GUZ_HVR_INSTALLED
            Debug.Log("Selecting Context: VR");
            InteractionAdapter = new HVRInteractionAdapter();
            DialogAdapter = new HVRDialogAdapter();
#else
            throw new Exception("Hurricane VR isn't activated inside Player Settings. Please do before you use it.");
#endif
        }
    }
}
