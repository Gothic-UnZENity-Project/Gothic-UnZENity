using HurricaneVR.Framework.ControllerInput;
using UnityEngine.InputSystem;

namespace GUZ.HVR.Components
{
    public class GUZHVRPlayerInputs : HVRPlayerInputs
    {
        protected override bool GetIsJumpActivated()
        {
            // If HVRSimulator is active
            if (UseWASD)
            {
                return Keyboard.current[Key.Space].wasPressedThisFrame;
            }
            else
            {
                return base.GetIsJumpActivated();
            }
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
