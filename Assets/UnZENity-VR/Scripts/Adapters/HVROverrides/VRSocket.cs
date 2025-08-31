#if GUZ_HVR_INSTALLED
using GUZ.Core.Vob;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;

namespace GUZ.VR.Adapters.HVROverrides
{
    /// <summary>
    /// Our VobItems have the following structure:
    /// |- VobLoader.comp
    /// |-- Mesh + Grabbable.comp
    /// |--- ...
    /// Problem is that HVR assumes the root of an object to be moved is the Grabbable GO. But on our end it's one level higher.
    /// This class therefore overwrites some logic to ensure, that our root with VobLoader (which will be used by e.g. Cullung) is the root.
    /// </summary>
    public class VRSocket : HVRSocket
    {
        protected override void OnGrabbed(HVRGrabArgs args)
        {
            // HINT: We can't call base.OnGrabbed(), as it would break the parent behaviour already. We therefore recreate its logic here.
            
            // From HVRGrabberBase.cs
            {
                args.Grabbable.Destroyed.AddListener(OnGrabbableDestroyed);
            }
            
            // From HVRSocket.cs (change: _previousParent variable is a different one)
            {
                var grabbable = args.Grabbable;
                _previousParent = grabbable.GetComponentInParent<VobLoader>().transform.parent; // We use parent of Grabbable object.
                _previousScale = grabbable.transform.localScale;
                
                AttachGrabbable(grabbable);
                OnGrabbableParented(grabbable);
                HandleRigidBodyGrab(grabbable);
                PlaySocketedSFX(grabbable.Socketable);

                if (args.RaiseEvents)
                {
                    Grabbed.Invoke(this, grabbable);
                }
            }
        }
        
        /// <summary>
        /// base.OnReleased() will set the Grabbable.comp GO to oCItem root. But this would ignore our VobLoader.comp GO in between.
        /// We therefore reset it to the named structure from this class headers documentation again.
        /// </summary>
        protected override void OnReleased(HVRGrabbable grabbable)
        {
            var tmpPreviousParent = _previousParent;
            var itemRoot = grabbable.GetComponentInParent<VobLoader>().transform;

            base.OnReleased(grabbable);
            
            grabbable.transform.parent = itemRoot;
            itemRoot.parent = tmpPreviousParent;
        }
    }
}
#endif
