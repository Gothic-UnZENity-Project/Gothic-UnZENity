#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Adapters.UI.Menus;
using GUZ.Core.Const;
using GUZ.Core.Services;
using GUZ.Core.Services.Config;
using HurricaneVR.Framework.Core.Player;
using MyBox;
using Reflex.Attributes;
using UnityEngine.SceneManagement;

namespace GUZ.VR.Adapters.HVROverrides
{
    public class VRPlayerController : HVRPlayerController
    {
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly GameStateService _gameStateService;

        public VRPlayerInputs VrInputs => (VRPlayerInputs)Inputs;

        [Separator("GUZ - Settings")]
        public MenuHandler MenuHandler;

        protected override void Start()
        {
            base.Start();
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefsUpdated);

            // Enabled later via button press or other events
            MenuHandler?.gameObject.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();

            if (VrInputs.IsMenuActivated && IsGameScene())
            {
                _gameStateService.InGameAndAlive = true;
                MenuHandler.ToggleVisibility();
            }
        }

        private void OnDestroy()
        {
            GlobalEventDispatcher.PlayerPrefUpdated.RemoveListener(OnPlayerPrefsUpdated);
        }

        /// <summary>
        /// Used in game scenes where you play the game (world.unity, ...)
        /// </summary>
        public void SetNormalControls(bool useDefaultValues = false)
        {
            // We have our player created before Gothic inis are loaded. We therefore need to set some default values.
            if (useDefaultValues)
            {
                CameraRig.SetSitStandMode(HVRSitStand.PlayerHeight);
                DirectionStyle = PlayerDirectionMode.Camera;
                RotationType = RotationType.Snap;
                SnapAmount = 45f;
                SmoothTurnSpeed = 90f;

                return;
            }

            var sitStandSetting =
                _configService.Gothic.GetInt(VRConstants.IniNames.SitStand, (int)HVRSitStand.PlayerHeight);
            CameraRig.SetSitStandMode((HVRSitStand)sitStandSetting);

            DirectionStyle = (PlayerDirectionMode)_configService.Gothic.GetInt(VRConstants.IniNames.MoveDirection, (int)PlayerDirectionMode.Camera);
            RotationType = (RotationType)_configService.Gothic.GetInt(VRConstants.IniNames.RotationType, (int)RotationType.Snap);

            var snapSetting = _configService.Gothic.GetInt(VRConstants.IniNames.SnapRotationAmount, VRConstants.SnapRotationDefaultValue);
            // e.g., 20° = 5° + 3*5°
            SnapAmount = VRConstants.SnapRotationAmountSettingTickAmount + VRConstants.SnapRotationAmountSettingTickAmount * snapSetting;
            
            var smoothSetting = _configService.Gothic.GetFloat(VRConstants.IniNames.SmoothRotationSpeed, VRConstants.SmoothRotationDefaultValue);
            // e.g., 50 = 5 + 90 * 0.5f
            SmoothTurnSpeed = VRConstants.SmoothRotationMinSpeed + VRConstants.SmoothRotationMaxAdditionalSpeed * smoothSetting;
        }

        /// <summary>
        /// Disable certain actions to keep player stuck in current position.
        /// </summary>
        public void SetLockedControls()
        {
            // HINT: Disable physics
            // We can't disable physics as it would prevent HVRTeleport.Teleport() from finishing (as it checks for Player.IsGrounded every frame).
            // Therefore, we need to ground the player always on a plane and disable movement only.

            MovementEnabled = false;
            RotationEnabled = false;
            Teleporter.enabled = false;
        }

        public void SetUnlockedControls()
        {
            MovementEnabled = true;
            RotationEnabled = true;
            Teleporter.enabled = true;
        }
        
        public void SetWalkingControls()
        {
            ChangeGrabbing(true);

            // Enable backpack
            // HVRHandGrabber.AllowGrabbing = false;
            // Enable grabbing of objects

            // Disable vertical walking controls
        }
        
        public void SetWaterWalkingControls()
        {
            ChangeGrabbing(true);
        }

        public void SetSwimmingControls()
        {
            // Disable grabbing of objects (as in G1)
            ChangeGrabbing(false);

            // Disable vertical walking controls
        }
        
        public void SetDivingControls()
        {
            // Disable grabbing of objects (as in G1)
            ChangeGrabbing(false);
            
            // Enable vertical walking controls
        }

        private void ChangeGrabbing(bool enable)
        {
            LeftHand.AllowGrabbing = enable;
            LeftHand.ForceGrabber.AllowGrabbing = enable;
            LeftHand.AllowHovering = enable;
            LeftHand.ForceGrabber.AllowHovering = enable;

            RightHand.AllowGrabbing = enable;
            RightHand.ForceGrabber.AllowGrabbing = enable;
            RightHand.AllowHovering = enable;
            RightHand.ForceGrabber.AllowHovering = enable;
            
            // Disable hand animations. Basically open the hand fully if disabled=true
            if (LeftHand.HandAnimator && LeftHand.HandAnimator.CurrentPoser)
            {
                if (LeftHand.HandAnimator.CurrentPoser.PrimaryPose != null)
                {
                    LeftHand.HandAnimator.CurrentPoser.PrimaryPose.Disabled = !enable;
                }

                if (LeftHand.HandAnimator.CurrentPoser.Blends != null)
                {
                    LeftHand.HandAnimator.CurrentPoser.Blends.ForEach(i => i.Disabled = !enable);
                }
            }

            if (RightHand.HandAnimator && RightHand.HandAnimator.CurrentPoser)
            {
                if (RightHand.HandAnimator.CurrentPoser.PrimaryPose != null)
                {
                    RightHand.HandAnimator.CurrentPoser.PrimaryPose.Disabled = !enable;
                }

                if (RightHand.HandAnimator.CurrentPoser.Blends != null)
                {
                    RightHand.HandAnimator.CurrentPoser.Blends.ForEach(i => i.Disabled = !enable);
                }
            }
        }

        private void OnPlayerPrefsUpdated(string preferenceKey, object value)
        {
            // Just update everything.
            if (preferenceKey == VRConstants.IniNames.MoveDirection ||
                preferenceKey == VRConstants.IniNames.RotationType ||
                preferenceKey == VRConstants.IniNames.SnapRotationAmount ||
                preferenceKey == VRConstants.IniNames.SmoothRotationSpeed ||
                preferenceKey == VRConstants.IniNames.SitStand)
            {
                SetNormalControls();
            }
        }

        /// <summary>
        /// Game scenes are all the ones where we play. Aka !=MainMenu, !=Loadings, ...
        /// </summary>
        private bool IsGameScene()
        {
            var activeSceneName = SceneManager.GetActiveScene().name;

            return activeSceneName switch
            {
                Constants.SceneMainMenu => false,
                Constants.SceneLoading => false,
                _ => true
            };
        }

        // protected override void HandleHorizontalMovement()
        // {
        //     if (_playerAi.WalkMode == (int)VmGothicEnums.WalkMode.Swim)
        //     {
        //         
        //     }
        //     else if (_playerAi.WalkMode == (int)VmGothicEnums.WalkMode.Dive)
        //     {
        //         
        //     }
        //     else
        //     {
        //         base.HandleHorizontalMovement();
        //     }
        // }
        //
        // protected override void HandleVerticalMovement()
        // {
        //     if (_playerAi.WalkMode == (int)VmGothicEnums.WalkMode.Swim)
        //     {
        //         
        //     }
        //     else if (_playerAi.WalkMode == (int)VmGothicEnums.WalkMode.Dive)
        //     {
        //         
        //     }
        //     else
        //     {
        //         base.HandleVerticalMovement();
        //     }
        // }
    }
}
#endif
