#if GUZ_HVR_INSTALLED
using GUZ.Core;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class HVRoCItem : MonoBehaviour
    {
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items into our hands.
            transform.GetComponent<Rigidbody>().isKinematic = false;
            
            GameGlobals.VobMeshCulling?.StartTrackVobPositionUpdates(gameObject);
        }
        
        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            GameGlobals.VobMeshCulling?.StopTrackVobPositionUpdates(gameObject);
        }
    }
}
#endif
