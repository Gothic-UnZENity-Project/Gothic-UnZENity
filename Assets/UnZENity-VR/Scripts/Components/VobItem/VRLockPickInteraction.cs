using GUZ.VR.Manager;
using GUZ.VR.Properties.VobItem;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRLockPickInteraction : MonoBehaviour
    {
        [SerializeField] private VRVobLockPickProperties _properties;
 
        private bool _firstFrameHandlingStarted;
        private GameObject _handGrabber;
        private Quaternion _initialRotation;

        private void Update()
        {
            if (!_properties.IsInsideLock)
            {
                _firstFrameHandlingStarted = false;
                return;
            }

            if (!_firstFrameHandlingStarted)
            {
                StartTracking();
                _firstFrameHandlingStarted = true;
            }
            
            CalculateRotation();
        }
 
        private void StartTracking()
        {
            _initialRotation = VRPlayerManager.GrabbedItemLeft.transform.rotation;
        }
        
        private void CalculateRotation()
        {
            /**
             * 0. Set start rotation of hand(s) which grab item. (As we have 2 hands, we need to check via array[2] for both items always!
             * 1.a If rotation is ~45° right - trigger right-information to currently active door
             * 1.b ~45° left - same
             */
            
            var rotation = VRPlayerManager.GrabbedItemLeft.transform.rotation;
            var _rotationAxis = Vector3.forward;
            var currentRotation = Vector3.Angle(_initialRotation.eulerAngles, rotation * _rotationAxis);
            
            Debug.Log("rotation: " + currentRotation);
        }
    }
}
