using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Shared;
using MyBox;
using UnityEngine.InputSystem;

namespace GUZ.HVR.Components
{
    public class GUZHVRPlayerInputs : HVRPlayerInputs
    {
        [Separator("GUZ - Settings")]
        public bool IsMenuActivated;
        public HVRButtonState MenuState;


        protected override void UpdateInput()
        {
            base.UpdateInput();

            if (!UpdateInputs)
            {
                return;
            }

            IsMenuActivated = GetMenuActivated();

            ResetState(ref MenuState);
            SetState(ref MenuState, IsMenuActivated);
        }

        private bool GetMenuActivated()
        {
            // If HVRSimulator is Active
            if (UseWASD)
            {
                return Keyboard.current[Key.Escape].wasPressedThisFrame;
            }

            if (HVRInputManager.Instance.LeftController.ControllerType == HVRControllerType.Knuckles)
            {
                return HVRController.GetButtonState(HVRHandSide.Left, HVRButtons.TrackPadButton).JustActivated ||
                       HVRController.GetButtonState(HVRHandSide.Right, HVRButtons.TrackPadButton).JustActivated;
            }
            else
            {
                return HVRController.GetButtonState(HVRHandSide.Left, HVRButtons.Menu).JustActivated ||
                       HVRController.GetButtonState(HVRHandSide.Right, HVRButtons.Menu).JustActivated;
            }
        }

        protected override bool GetIsJumpActivated()
        {
            // If HVRSimulator is active
            if (UseWASD)
            {
                return Keyboard.current[Key.Space].wasPressedThisFrame;
            }

            return RightController.PrimaryButtonState.JustActivated;
        }

        protected override bool GetCrouch()
        {
            // If HVRSimulator is active
            if (UseWASD)
            {
                return Keyboard.current[Key.Z].wasPressedThisFrame || Keyboard.current[Key.X].wasPressedThisFrame;
            }
            else
            {
                return base.GetCrouch();
            }
        }

    }
}
