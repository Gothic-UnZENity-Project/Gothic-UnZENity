#if GUZ_HVR_INSTALLED
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class HVROCItem : MonoBehaviour
    {
        // FIXME - Should later reside in another Component for Items only as the physical change is only for oCItems.
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items into our hands.
            transform.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
#endif
