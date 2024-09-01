using GUZ.Core.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace GUZ.Core.Player.Climb
{
    public class ClimbProvider : MonoBehaviour
    {
        [FormerlySerializedAs("characterController")]
        public CharacterController CharacterController;

        [FormerlySerializedAs("velocityRight")]
        public InputActionProperty VelocityRight;

        [FormerlySerializedAs("velocityLeft")] public InputActionProperty VelocityLeft;

        private bool _rightActive;
        private bool _leftActive;

        private float _originalMovementSpeed;
        private float _originalTurnSpeed;

        private Vector3 _grabbedLadderZsTopPosition;

        private void Start()
        {
            XRDirectClimbInteractor.ClimbHandActivated.AddListener(HandActivated);
            XRDirectClimbInteractor.ClimbHandDeactivated.AddListener(HandDeactivated);

            _originalMovementSpeed = transform.GetComponent<ActionBasedContinuousMoveProvider>().moveSpeed;
            _originalTurnSpeed = transform.GetComponent<ContinuousTurnProviderBase>().turnSpeed;
        }

        /// <summary>
        /// As this is called every 0.02 seconds - fixed. This offers a smoother movement than with variable fps.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_rightActive && !_leftActive)
            {
                return;
            }

            if (IsOnTop())
            {
                SpawnToTop();
            }
            else
            {
                Climb();
            }
        }

        private void OnDestroy()
        {
            XRDirectClimbInteractor.ClimbHandActivated.RemoveListener(HandActivated);
            XRDirectClimbInteractor.ClimbHandDeactivated.RemoveListener(HandDeactivated);
        }

        private bool IsOnTop()
        {
            // Check if we're at a certain height with our main Camera.
            // If we are, we can't climb.
            var mainCamera = UnityEngine.Camera.main!;
            var mainCameraHeight = mainCamera.transform.position.y;

            return mainCameraHeight >= _grabbedLadderZsTopPosition.y;
        }

        private void SpawnToTop()
        {
            XRDirectClimbInteractor.ClimbHandDeactivated.Invoke("RightHandBaseController");
            XRDirectClimbInteractor.ClimbHandDeactivated.Invoke("LeftHandBaseController");

            CharacterController.transform.position = _grabbedLadderZsTopPosition;
        }

        private void Climb()
        {
            var velocity = Vector3.zero;
            velocity += _leftActive ? VelocityLeft.action.ReadValue<Vector3>() : Vector3.zero;
            velocity += _rightActive ? VelocityRight.action.ReadValue<Vector3>() : Vector3.zero;

            CharacterController.Move(CharacterController.transform.rotation * -velocity * Time.fixedDeltaTime);
        }

        /// <summary>
        /// If a hand starts grabbing a Ladder.
        /// </summary>
        private void HandActivated(string controllerName, GameObject ladder)
        {
            switch (controllerName)
            {
                case "LeftHandBaseController":
                    _leftActive = true;
                    break;
                case "RightHandBaseController":
                    _rightActive = true;
                    break;
                default:
                    Debug.LogWarning($"Unknown hand controller used for climbing: >{controllerName}<");
                    return; // Do nothing.
            }

            _grabbedLadderZsTopPosition = ladder.FindChildRecursively("ZS_POS1").transform.position;

            DeactivateMovement();
        }

        /// <summary>
        /// If a hand stops grabbing a Ladder.
        /// </summary>
        private void HandDeactivated(string controllerName)
        {
            switch (controllerName)
            {
                case "LeftHandBaseController":
                    _leftActive = false;
                    break;
                case "RightHandBaseController":
                    _rightActive = false;
                    break;
                default:
                    Debug.LogWarning($"Unknown hand controller used for climbing: >{controllerName}<");
                    return; // Do nothing.
            }

            if (_leftActive || _rightActive)
            {
                return;
            }

            ActivateMovement();
        }

        /// <summary>
        /// Activates Gravity, movement and turn options
        /// </summary>
        private void ActivateMovement()
        {
            // Reactivate gravity and speed to original speed
            transform.GetComponent<ActionBasedContinuousMoveProvider>().useGravity = true;
            transform.GetComponent<ActionBasedContinuousMoveProvider>().moveSpeed = _originalMovementSpeed;

            // In case of using Continuous turn, reactivate turn speed
            transform.GetComponent<ContinuousTurnProviderBase>().turnSpeed = _originalTurnSpeed;

            // In case of using Snap Turn, reenable turn
            transform.GetComponent<SnapTurnProviderBase>().enableTurnLeftRight = true;
            transform.GetComponent<SnapTurnProviderBase>().enableTurnAround = true;
        }

        /// <summary>
        /// Deactivates Gravity, movement and turn options
        /// </summary>
        private void DeactivateMovement()
        {
            // Set gravity to false and speed to 0
            transform.GetComponent<ActionBasedContinuousMoveProvider>().useGravity = false;
            transform.GetComponent<ActionBasedContinuousMoveProvider>().moveSpeed = 0;

            // In case of using Continuous turn , set the turn speed to 0
            transform.GetComponent<ContinuousTurnProviderBase>().turnSpeed = 0;

            // In case of using Snap Turn, set turn to false
            transform.GetComponent<SnapTurnProviderBase>().enableTurnLeftRight = false;
            transform.GetComponent<SnapTurnProviderBase>().enableTurnAround = false;
        }
    }
}
