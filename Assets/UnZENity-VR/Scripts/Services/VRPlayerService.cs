#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Player;
using GUZ.VR.Adapters.HVROverrides;
using GUZ.VR.Services.Context;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Shared;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.VR.Services
{
    /// <summary>
    /// Contains global states about Hurricane VR player.
    /// </summary>
    public class VRPlayerService
    {
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly PlayerService _playerService;
        
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

            // If we grabbed the element with second hand, return.
            if (IsDualGrabbed)
                return;

            // Otherwise alter inventory count
            var vobItem = grabbable.GetComponentInParent<VobLoader>().Container.VobAs<IItem>();
            _playerService.AddItem(vobItem.Instance, vobItem.Amount);
        }
        
        public void UnsetGrab(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            var dualGrabPrev = IsDualGrabbed;

            if (grabbable.LeftHandGrabber)
                GrabbedItemLeft = null;
            else
                GrabbedObjectRight = null;

            // If we removed one hand from our item.
            if (dualGrabPrev)
                return;

            // Otherwise alter inventory count
            var vobItem = grabbable.GetComponentInParent<VobLoader>().Container.VobAs<IItem>();
            _playerService.RemoveItem(vobItem.Instance, vobItem.Amount);
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
