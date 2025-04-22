using System;
using GUZ.Core;
using GUZ.VR.Components.HVROverrides;
using MyBox;
using UnityEngine;

namespace GUZ.VR.Components
{
    public class VRSpectatorCamera : MonoBehaviour
    {
        [Serializable]
        public class SmoothingGroup
        {
            [Range(0.0f, 1.0f)]
            [Tooltip("How quickly position changes are smoothed (lower = smoother but more lag)")]
            public float positionSmoothTime;

            [Range(0.0f, 1.0f)]
            [Tooltip("How quickly rotation changes are smoothed (lower = smoother but more lag)")]
            public float rotationSmoothTime;

            [Range(0.0f, 1.0f)]
            [Tooltip("Strength of vertical movement reduction (lower = more reduction)")]
            public float verticalSmoothTime;
        }

        public enum SmoothingLevel
        {
            None,
            Low,
            Medium,
            High
        }


        [Tooltip("Reference to the VR camera/head transform that will be followed")]
        [SerializeField]
        private Transform _vrCameraTransform;

        [Tooltip("Reference to the camera that will be used for the spectator view")]
        [SerializeField]
        private Camera _spectatorCamera;

        [Header("Smoothing Settings")]

        [OverrideLabel("SmoothingLevel")]
        public SmoothingLevel SmoothingLvl = SmoothingLevel.Medium;

        [Tooltip("Apply additional smoothing to quick vertical movements")]
        public bool reduceVerticalBobbing = true;

        public SmoothingGroup NoneSmoothing = new() { positionSmoothTime = 0.0f, rotationSmoothTime = 0.0f, verticalSmoothTime = 0.0f };
        public SmoothingGroup LowSmoothing = new() { positionSmoothTime = 0.02f, rotationSmoothTime = 0.02f, verticalSmoothTime = 0.05f };
        public SmoothingGroup MediumSmoothing = new() { positionSmoothTime = 0.08f, rotationSmoothTime = 0.08f, verticalSmoothTime = 0.15f };
        public SmoothingGroup HighSmoothing = new() { positionSmoothTime = 0.15f, rotationSmoothTime = 0.15f, verticalSmoothTime = 0.25f };

        
        private SmoothingGroup _selectedSmoothingGroup;

        // Internal variables for smoothing calculations
        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;
        private float _verticalVelocity;
        private float _currentY;

        private void Start()
        {
            // Disable the whole feature if we're not on Windows or Editor build.
            // Save some CPU cycles for Android builds as we don't have a second screen there.
#if !UNITY_EDITOR && !UNITY_STANDALONE
            GetComponent<Camera>().enabled = false;
            this.enabled = false;
            gameObject.SetActive(false);
            return;
#endif

            var playerController = GameContext.InteractionAdapter.GetCurrentPlayerController().GetComponent<VRPlayerController>();
            playerController.Teleporter.PositionUpdate.AddListener(SetPositionOnce);
            GlobalEventDispatcher.PlayerFallingChanged.AddListener(DisablePositionSmoothing);

            SetSmoothness();

            if (_spectatorCamera != null)
            {
                _spectatorCamera.fieldOfView = _vrCameraTransform.GetComponent<Camera>().fieldOfView;
            }

            if (_vrCameraTransform != null)
            {
                // Initialize position to avoid jumps at startup
                transform.position = _vrCameraTransform.position;
                transform.rotation = _vrCameraTransform.rotation;
                _currentY = _vrCameraTransform.position.y;
            }
        }

        private void OnValidate()
        {
            SetSmoothness();
        }

        private void LateUpdate()
        {
            if (_vrCameraTransform == null)
                return;

            // Get the target position with potential modifications
            var targetPosition = _vrCameraTransform.position;

            // Handle vertical movement separately if enabled
            if (reduceVerticalBobbing)
            {
                // Smooth Y position separately with a longer smooth time
                _currentY = Mathf.SmoothDamp(_currentY, targetPosition.y, ref _verticalVelocity, _selectedSmoothingGroup.verticalSmoothTime);
                targetPosition.y = _currentY;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _positionVelocity,
                _selectedSmoothingGroup.positionSmoothTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _vrCameraTransform.rotation,
                // Use an exponential ease-in/out for more natural-feeling camera movement
                1.0f - Mathf.Exp(-Time.deltaTime / _selectedSmoothingGroup.rotationSmoothTime)
            );
        }

        /// <summary>
        /// If we - e.g. - teleport to a new position, we need to set the position once.
        /// Otherwise, the camera would fly to this position slowly.
        /// </summary>
        private void SetPositionOnce(Vector3 position)
        {
            transform.position = position;
            transform.rotation = _vrCameraTransform.rotation;
            _currentY = _vrCameraTransform.position.y;
        }

        private void SetSmoothness()
        {
            _selectedSmoothingGroup = SmoothingLvl switch
            {
                SmoothingLevel.None => NoneSmoothing,
                SmoothingLevel.Low => LowSmoothing,
                SmoothingLevel.Medium => MediumSmoothing,
                SmoothingLevel.High => HighSmoothing,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void DisablePositionSmoothing(bool disableSmoothing)
        {
            throw new NotImplementedException("We need to disable smoothing if we fall down or fly, ... And reenable once normal movement is restarted.");
        }

        /// <summary>
        /// Helper method to handle angle wrapping for proper rotation smoothing
        /// </summary>
        private float FixAngle(float angle)
        {
            if (angle > 180)
            {
                angle -= 360;
            }

            return angle;
        }
    }
}
