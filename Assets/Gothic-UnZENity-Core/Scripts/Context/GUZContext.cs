using System;
using GUZ.XRIT;
using UnityEngine;
#if GUZ_HVR_INSTALLED
using GUZ.HVR;
#endif

namespace GUZ.Core.Context
{
    public static class GuzContext
    {
        public static IInteractionAdapter InteractionAdapter { get; private set; }

        public enum Controls
        {
            HVR, // Hurricane VR
            XRIT // XR Interaction Toolkit
        }

        public static void SetContext(Controls controls)
        {
            switch (controls)
            {
                case Controls.XRIT:
                    SetXRITContext();
                    break;
                case Controls.HVR:
                    SetHVRContext();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controls), controls, null);
            }
        }

        private static void SetXRITContext()
        {
            Debug.Log("Selecting Context: VR - XR Interaction Toolkit (XRIT)");
            InteractionAdapter = new XritInteractionAdapter();
        }

        private static void SetHVRContext()
        {
#if GUZ_HVR_INSTALLED
            Debug.Log("Selecting Context: VR - HurricaneVR");
            InteractionAdapter = new HVRInteractionAdapter();
#else
            Debug.LogError("HVR isn't activated inside Player Settings. Please do before you use it.");
#endif
        }
    }
}
