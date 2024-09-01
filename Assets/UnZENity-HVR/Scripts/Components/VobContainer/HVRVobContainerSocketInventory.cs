#if GUZ_HVR_INSTALLED
using GUZ.HVR.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components.VobContainer
{
    /// <summary>
    /// Handle Socket-events for a whole Container (e.g. chest) and its corresponding sockets (rings where we put items into).
    /// </summary>
    public class HVRVobContainerSocketInventory : MonoBehaviour
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
