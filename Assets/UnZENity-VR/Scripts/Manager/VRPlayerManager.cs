#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.VR.Adapter;
using GUZ.VR.Components.HVROverrides;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace GUZ.VR.Manager
{
    /// <summary>
    /// Contains global states about Hurricane VR player.
    /// </summary>
    public static class VRPlayerManager
    {
        public static VRInteractionAdapter VRInteractionAdapter => (VRInteractionAdapter)GameContext.InteractionAdapter;
        public static VRPlayerInputs VRPlayerInputs => VRInteractionAdapter.GetVRPlayerInputs();
        
        public static GameObject GrabbedItemLeft;
        public static GameObject GrabbedObjectRight;

        public enum HandType
        {
            Left,
            Right
        }

        public static bool IsDualGrabbed => GrabbedItemLeft != null && GrabbedItemLeft == GrabbedObjectRight;

        public static void SetGrab(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabbable.LeftHandGrabber)
            {
                GrabbedItemLeft = grabbable.gameObject;
            }
            else
            {
                GrabbedObjectRight = grabbable.gameObject;
            }
        }
        
        public static void UnsetGrab(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabbable.LeftHandGrabber)
            {
                GrabbedItemLeft = null;
            }
            else
            {
                GrabbedObjectRight = null;
            }
        }

        public static HVRController GetHand(HVRHandSide side)
        {
            if (side == HVRHandSide.Left)
                return GameContext.InteractionAdapter.GetCurrentPlayerController().GetComponent<VRPlayerController>().LeftHand.Controller;
            else
                return GameContext.InteractionAdapter.GetCurrentPlayerController().GetComponent<VRPlayerController>().RightHand.Controller;
        }
    }
}
#endif
