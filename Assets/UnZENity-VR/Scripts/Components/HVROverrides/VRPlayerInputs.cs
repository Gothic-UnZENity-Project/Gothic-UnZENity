﻿#if GUZ_HVR_INSTALLED
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Shared;
using MyBox;
using UnityEngine.InputSystem;

namespace GUZ.VR.Components.HVROverrides
{
    public class VRPlayerInputs : HVRPlayerInputs
    {
        [Separator("GUZ - Settings")]
        public bool IsMenuActivated;
        public HVRButtonState MenuState;
        public bool IsMenuButtonEnabled = true;
        
        public bool IsSpeakingActivated;
        public HVRButtonState SpeakingState;
        public bool IsSpeakButtonEnabled = true;


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

            IsSpeakingActivated = GetSpeakingActivated();
            ResetState(ref SpeakingState);
            SetState(ref SpeakingState, IsSpeakingActivated);
        }

        private bool GetMenuActivated()
        {
            if (!IsMenuButtonEnabled)
            {
                return false;
            }
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
                return HVRController.GetButtonState(HVRHandSide.Left, HVRButtons.Secondary).JustActivated;
            }
        }
        
        private bool GetSpeakingActivated()
        {
            if (UseWASD)
            {
                return Keyboard.current[Key.T].isPressed;
            }
            else
            {
                return HVRController.GetButtonState(HVRHandSide.Left, HVRButtons.Grip).Active
                       && HVRController.GetButtonState(HVRHandSide.Right, HVRButtons.Grip).Active;
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
        
        protected override bool GetSprinting()
        {
            // If HVRSimulator is active
            if (UseWASD)
            {
                return Keyboard.current[Key.LeftShift].isPressed;
            }
            else
            {
                return base.GetSprinting();
            }
        }

    }
}
#endif

