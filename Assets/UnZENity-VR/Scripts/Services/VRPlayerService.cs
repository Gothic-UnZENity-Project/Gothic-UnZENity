#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Services.Context;
using GUZ.VR.Adapters.HVROverrides;
using GUZ.VR.Services.Context;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Shared;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Services
{
    /// <summary>
    /// Contains global states about Hurricane VR player.
    /// </summary>
    public class VRPlayerService
    {
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        
        public VRContextInteractionService VRContextInteractionService => _contextInteractionService.GetImpl<VRContextInteractionService>();
        public VRPlayerInputs VRPlayerInputs => VRContextInteractionService.GetVRPlayerInputs();
        
        public GameObject GrabbedItemLeft;
        public GameObject GrabbedObjectRight;

        public enum HandType
        {
            Left,
            Right
        }

        public bool IsDualGrabbed => GrabbedItemLeft != null && GrabbedItemLeft == GrabbedObjectRight;

        public void SetGrab(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabbable.LeftHandGrabber)
                GrabbedItemLeft = grabbable.gameObject;
            else
                GrabbedObjectRight = grabbable.gameObject;
        }
        
        public void UnsetGrab(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabbable.LeftHandGrabber)
                GrabbedItemLeft = null;
            else
                GrabbedObjectRight = null;
        }

        public HVRController GetHand(HVRHandSide side)
        {
            if (side == HVRHandSide.Left)
                return _contextInteractionService.GetCurrentPlayerController().GetComponent<VRPlayerController>().LeftHand.Controller;
            else
                return _contextInteractionService.GetCurrentPlayerController().GetComponent<VRPlayerController>().RightHand.Controller;
        }
    }
}
#endif
