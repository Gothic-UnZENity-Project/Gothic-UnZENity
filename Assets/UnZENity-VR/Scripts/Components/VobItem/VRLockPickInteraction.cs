#if GUZ_HVR_INSTALLED
using GUZ.VR.Components.VobDoor;
using GUZ.VR.Properties.VobItem;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRLockPickInteraction : MonoBehaviour
    {
        [SerializeField] private VRVobLockPickProperties _properties;
 
        private bool _firstFrameHandlingStarted = true;
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

        /// <summary>
        /// Set start rotation of hand(s) which grab item. (As we have 2 hands, we need to check via array[2] for both items always!
        /// </summary>
        private void StartTracking()
        {
            _initialZRotation = _properties.HoldingHand.rotation.eulerAngles.z;
        }

        /// <summary>
        /// 1. If rotation is ~45° right/left - trigger information to currently active door
        /// 2. Once we're in a left/right state, rotate back to ~10° where the status gets cleared and a new left/right can be triggered
        /// </summary>
        private void CalculateRotation()
        {
            Debug.Log($"InitialRotZ={_initialZRotation}, CurrentRotZ={_properties.HoldingHand.rotation.eulerAngles.z}");

            var rotationDiff = Mathf.DeltaAngle(_initialZRotation,
                _properties.HoldingHand.rotation.eulerAngles.z);

            // Check for specific rotation thresholds
            switch (_handRotationState)
            {
                case RotationState.Normal:
                    if (rotationDiff >= 45f)
                    {
                        UpdateStatus(RotationState.Right);
                    }
                    else if (rotationDiff <= -45f)
                    {
                        UpdateStatus(RotationState.Left);
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

        private void UpdateStatus(RotationState state)
        {
            _handRotationState = state;

            // Trigger right/left information to currently active door
            var doorState = _properties.ActiveDoorLock.UpdateCombination(_handRotationState == RotationState.Left);

            switch (doorState)
            {
                case VRDoorLockInteraction.DoorLockStatus.StepFailure:
                    // FIXME - Handle break of a Lock Pick based on hero's skill level
                    break;
                case VRDoorLockInteraction.DoorLockStatus.StepSuccess:
                    break;
                case VRDoorLockInteraction.DoorLockStatus.Unlocked:
                    // We immediately reset current door as it's unlocked, and we don't need to use lock pick any longer.
                    _properties.IsInsideLock = false;
                    _properties.ActiveDoorLock = null;
                    _properties.HoldingHand = null;
                    break;
            }
        }
    }
}
#endif
