#if GUZ_HVR_INSTALLED
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

        public bool IsQuestLogActivated;
        public HVRButtonState QuestLogState;

        public bool IsStatusActivated;
        public HVRButtonState StatusState;

        protected override void UpdateInput()
        {
            base.UpdateInput();

            if (!UpdateInputs)
            {
                return;
            }

            IsMenuActivated = GetMenuActivated();
            IsQuestLogActivated = GetQuestLogActivated();
            IsStatusActivated = GetStatusActivated();

            ResetState(ref MenuState);
            SetState(ref MenuState, IsMenuActivated);
            ResetState(ref QuestLogState);
            SetState(ref QuestLogState, IsQuestLogActivated);
            ResetState(ref StatusState);
            SetState(ref StatusState, IsStatusActivated);
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
                return HVRController.GetButtonState(HVRHandSide.Left, HVRButtons.Secondary).JustActivated;
            }
        }

        private bool GetQuestLogActivated()
        {
            // If HVRSimulator is Active
            if (UseWASD)
            {
                return Keyboard.current[Key.L].wasPressedThisFrame;
            }

            // During normal gameplay, we grab the menu from our chest socket.
            return false;
        }

        private bool GetStatusActivated()
        {
            // If HVRSimulator is Active
            if (UseWASD)
            {
                return Keyboard.current[Key.B].wasPressedThisFrame;
            }

            // During normal gameplay, we grab the menu from our chest socket.
            return false;
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
#endif

