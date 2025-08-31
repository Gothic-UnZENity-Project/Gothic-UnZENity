#if GUZ_HVR_INSTALLED
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.VR.Adapters.Vob.Container
{
    /// <summary>
    /// Handle Socket-events for a whole Container (e.g., chest) and its corresponding sockets (rings where we put items into).
    /// </summary>
    public class VRVobContainerSocketInventory : MonoBehaviour
    {

        public void OnBeforeGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            grabbable.GetComponent<VRVobItemProperties>().IsSocketed = true;
        }

        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            grabbable.GetComponent<VRVobItemProperties>().IsSocketed = false;
        }

    }
}
#endif
