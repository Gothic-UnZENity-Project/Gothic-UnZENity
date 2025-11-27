#if GUZ_HVR_INSTALLED
using GUZ.Core;
using HurricaneVR.Framework.Shared;
using Reflex.Attributes;

namespace GUZ.VR.Services
{
    public class VrHapticsService
    {
        public enum VibrationType
        {
            Info,
            Warning,
            Error,
            Success
        }

        [Inject] private readonly VRPlayerService _vrPlayerService;

        private readonly HapticData[] _vibrationData =
        {
            // Info - e.g., you are inside an interaction collider
            new (0.15f, 0.25f, 30f),

            // Warning - e.g., your lock pick rotation is invalid
            new (0.25f, 0.5f, 100f),

            // Error - e.g., your key is broken
            new (0.4f, 0.8f, 175f),

            // Success - e.g., you opened a chest
            new (0.5f, 0.6f, 80f)
        };

        public VrHapticsService()
        {
            // Fight
            GlobalEventDispatcher.FightWindowAttack.AddListener((_, handSide) => AttackVibration(handSide, VibrationType.Success)); // Attack can happen now
            GlobalEventDispatcher.FightWindowCombo.AddListener((_, handSide) => AttackVibration(handSide, VibrationType.Info)); // Now you can start another attack
            GlobalEventDispatcher.FightWindowComboFailed.AddListener((_, handSide) => AttackVibration(handSide, VibrationType.Error)); // Attack window passed
            
            // Lock picking
            GlobalEventDispatcher.LockPickComboWrong.AddListener((_, _, handSide) => Vibrate((HVRHandSide)handSide, VibrationType.Warning));
            GlobalEventDispatcher.LockPickComboBroken.AddListener((_, _, handSide) => Vibrate((HVRHandSide)handSide, VibrationType.Error));
            GlobalEventDispatcher.LockPickComboCorrect.AddListener((_, _, handSide) => Vibrate((HVRHandSide)handSide, VibrationType.Success));
            GlobalEventDispatcher.LockPickComboFinished.AddListener((_, _, handSide) => Vibrate((HVRHandSide)handSide, VibrationType.Success));
        }

        private void AttackVibration(GlobalEventDispatcher.HandSide handSide, VibrationType vibrationType)
        {
            switch (handSide)
            {
                case GlobalEventDispatcher.HandSide.Both:
                    Vibrate(HVRHandSide.Left, vibrationType);
                    Vibrate(HVRHandSide.Right, vibrationType);
                    break;
                case GlobalEventDispatcher.HandSide.Left:
                    Vibrate(HVRHandSide.Left, vibrationType);
                    break;
                case GlobalEventDispatcher.HandSide.Right:
                    Vibrate(HVRHandSide.Right, vibrationType);
                    break;
            }
        }


        public void Vibrate(HVRHandSide handSide, VibrationType type)
        {
            _vrPlayerService.GetHand(handSide).Vibrate(_vibrationData[(int)type]);
        }
    }
}
#endif
