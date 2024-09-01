#if GUZ_HVR_INSTALLED
using GUZ.VR.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.VR.Components.VobContainer
{
    /// <summary>
    /// Handle Socket-events for a whole Container (e.g. chest) and its corresponding sockets (rings where we put items into).
    /// </summary>
    public class VRVobContainerSocketInventory : MonoBehaviour
    {

        public void OnBeforeGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            grabbable.GetComponent<HVRVobItemProperties>().IsSocketed = true;
        }

        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            grabbable.GetComponent<HVRVobItemProperties>().IsSocketed = false;
        }

    }
}
#endif
