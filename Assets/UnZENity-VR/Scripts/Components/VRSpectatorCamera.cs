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
        [OverrideLabel("Smoothing Level")]
        public SmoothingLevel SmoothingLvl = SmoothingLevel.Medium;

        [Range(0.0f, 1.0f)]
        public float NoneRotationSmoothing = 0.0f;
        [Range(0.0f, 1.0f)]
        public float LowRotationSmoothing = 0.02f;
        [Range(0.0f, 1.0f)]
        public float MediumRotationSmoothing = 0.08f;
        [Range(0.0f, 1.0f)]
        public float HighRotationSmoothing = 0.15f;

        
        private float _selectedSmoothingValue;

        // Internal variables for smoothing calculations
        private Vector3 _positionVelocity;

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
            playerController.Teleporter.PositionUpdate.AddListener(SetTeleportPosition);

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

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _vrCameraTransform.rotation,
                // Use an exponential ease-in/out for more natural-feeling camera movement
                1.0f - Mathf.Exp(-Time.deltaTime / _selectedSmoothingValue)
            );
        }

        /// <summary>
        /// If we - e.g. - teleport to a new position, we need to set the position once.
        /// Otherwise, the camera would fly to this position slowly.
        /// </summary>
        private void SetTeleportPosition(Vector3 position)
        {
            // 1.7 == ~normal person size rather than at the bottom of the screen.
            transform.position = position + new Vector3(0, 1.7f, 0);
            transform.rotation = _vrCameraTransform.rotation;
        }

        private void SetSmoothness()
        {
            _selectedSmoothingValue = SmoothingLvl switch
            {
                SmoothingLevel.None => NoneRotationSmoothing,
                SmoothingLevel.Low => LowRotationSmoothing,
                SmoothingLevel.Medium => MediumRotationSmoothing,
                SmoothingLevel.High => HighRotationSmoothing,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
