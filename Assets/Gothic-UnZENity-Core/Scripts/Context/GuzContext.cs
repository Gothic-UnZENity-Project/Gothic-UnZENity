using System;
using GUZ.XRIT;
using UnityEngine;

#if GUZ_HVR_INSTALLED
using
#endif

namespace GUZ.Context
{
    public class GuzContext
    {
        public static IInteractionAdapter InteractionAdapter { get; private set; }

        public enum Controls
        {
            VR_XRIT, // XR Interaction Toolkit
            VR_HVR,  // Hurricane VR
        }
        
        public static void SetContext(Controls controls)
        {
            switch (controls)
            {
                case Controls.VR_XRIT:
                    SetXRITContext();
                    break;
                case Controls.VR_HVR:
                    SetHVRContext();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(controls), controls, null);
            }
        }

        private static void SetXRITContext()
        {
            Debug.Log("Selecting Context: VR - XR Interaction Toolkit (XRIT)");
            InteractionAdapter = new XRITInteractionAdapter();
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
