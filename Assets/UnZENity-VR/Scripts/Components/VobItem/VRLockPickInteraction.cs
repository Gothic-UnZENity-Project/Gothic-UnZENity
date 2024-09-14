using GUZ.VR.Manager;
using GUZ.VR.Properties.VobItem;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRLockPickInteraction : MonoBehaviour
    {
        [SerializeField] private VRVobLockPickProperties _properties;
 
        private bool _firstFrameHandlingStarted = true;
        private GameObject _handGrabber;
        private float _initialZRotation;

        /// <summary>
        /// A hand rotation with lock pick always need to go:
        /// a.) Normal -> Right -> Normal
        /// b) Normal -> Left -> Normal
        /// (We need to ensure that e.g. a left-left-right combination needs to be recognized properly.)
        /// </summary>
        private RotationState _handRotationState;

        private enum RotationState
        {
            Normal,
            Left,
            Right
        }

        private void Update()
        {
            if (!_properties.IsInsideLock)
            {
                _firstFrameHandlingStarted = true;
                return;
            }

            if (_firstFrameHandlingStarted)
            {
                StartTracking();
                _firstFrameHandlingStarted = false;
            }
            
            CalculateRotation();
        }
 
        private void StartTracking()
        {
            _initialZRotation = VRPlayerManager.GrabbedItemLeft.transform.rotation.eulerAngles.z;
        }

        private void CalculateRotation()
        {
            /**
             * 0. Set start rotation of hand(s) which grab item. (As we have 2 hands, we need to check via array[2] for both items always!
             * 1.a If rotation is ~45° right - trigger right-information to currently active door
             * 1.b ~45° left - same
             */

            var rotationDiff = Mathf.DeltaAngle(_initialZRotation,
                VRPlayerManager.GrabbedItemLeft.transform.rotation.eulerAngles.z);

            // Check for specific rotation thresholds
            switch (_handRotationState)
            {
                case RotationState.Normal:
                    if (rotationDiff >= 45f)
                    {
                        _handRotationState = RotationState.Right;
                        // Trigger right-information to currently active door
                        _properties.ActiveDoorLock.UpdateCombination(false);
                    }
                    else if (rotationDiff <= -45f)
                    {
                        _handRotationState = RotationState.Left;
                        // Trigger left-information to currently active door
                        _properties.ActiveDoorLock.UpdateCombination(true);
                    }
                    break;
                case RotationState.Left:
                    if (rotationDiff > -10)
                    {
                        _handRotationState = RotationState.Normal;
                    }
                    break;
                case RotationState.Right:
                    if (rotationDiff < 10f)
                    {
                        _handRotationState = RotationState.Normal;
                    }
                    break;
            }
        }
    }
}
