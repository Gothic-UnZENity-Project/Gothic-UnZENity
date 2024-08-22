#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.HVR.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class HVRVobItem : MonoBehaviour
    {
        [SerializeField] private HVRVobItemProperties _properties;
        
        
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // OnGrabbed is normally called multiple times. Even after an object is already socketed. If so, then let's stop Grab behaviour.
            if (_properties.IsSocketed)
            {
                return;
            }
            
            // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items into our hands.
            transform.GetComponent<Rigidbody>().isKinematic = false;

            // Stop collisions while being dragged around (otherwise e.g. items might stick inside chests when pulled out).
            if (!GameGlobals.Config.ItemsInHandHaveCollision)
            {
                gameObject.layer = Constants.VobItemNoCollision;
            }

            GameGlobals.VobMeshCulling?.StartTrackVobPositionUpdates(gameObject);
        }
        
        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            gameObject.layer = Constants.GrabbableLayer; // Back to HVR default

            GameGlobals.VobMeshCulling?.StopTrackVobPositionUpdates(gameObject);
        }
    }
}
#endif
