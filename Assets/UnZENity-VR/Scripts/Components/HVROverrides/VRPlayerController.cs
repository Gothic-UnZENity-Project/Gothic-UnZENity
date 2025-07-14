#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus;
using HurricaneVR.Framework.Core.Player;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Constants = GUZ.Core.Globals.Constants;

namespace GUZ.VR.Components.HVROverrides
{
    public class VRPlayerController : HVRPlayerController
    {
        public VRPlayerInputs VrInputs => (VRPlayerInputs)Inputs;

        [Separator("GUZ - Settings")]
        public MenuHandler MenuHandler;

        private INpc _playerVob;
        private IAiHuman _playerAi;

        private float _initialGravity;
        private float _initialMoveSpeed;
        private float _initialRunSpeed;
        private float _initialFallSpeed;

        protected override void Start()
        {
            base.Start();
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefsUpdated);
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(() =>
            {
                _playerVob = ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData()!.Vob;
                _playerAi = (IAiHuman)_playerVob.Ai;
            });

            // Enabled later via button press or other events
            MenuHandler.gameObject.SetActive(false);

            _initialGravity = Gravity;
            _initialMoveSpeed = MoveSpeed;
            _initialRunSpeed = RunSpeed;
            _initialFallSpeed = MaxFallSpeed;
        }

        protected override void Update()
        {
            base.Update();

            if (VrInputs.IsMenuActivated && IsGameScene())
            {
                GameData.InGameAndAlive = true;
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
                GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.SitStand, (int)HVRSitStand.PlayerHeight);
            CameraRig.SetSitStandMode((HVRSitStand)sitStandSetting);

            DirectionStyle = (PlayerDirectionMode)GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.MoveDirection, (int)PlayerDirectionMode.Camera);
            RotationType = (RotationType)GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.RotationType, (int)RotationType.Snap);

            var snapSetting = GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.SnapRotationAmount, VRConstants.SnapRotationDefaultValue);
            // e.g., 20° = 5° + 3*5°
            SnapAmount = VRConstants.SnapRotationAmountSettingTickAmount + VRConstants.SnapRotationAmountSettingTickAmount * snapSetting;
            
            var smoothSetting = GameGlobals.Config.Gothic.GetFloat(VRConstants.IniNames.SmoothRotationSpeed, VRConstants.SmoothRotationDefaultValue);
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

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            /*
             * 1. Check if we have water around us.
             * 2. Check if we need to "walk" as we are knee deep in water.
             * 3. Check if we need to swim as w are chest deep in water.
             */
            
			if (_playerVob == null)
				return;

            // FIXME - As we have different sizes of people in VR, we should use the size based on real heights.
            var kneeDeepHeight = GameData.cGuildValue.GetWaterDepthKnee((int)DaedalusConst.Guild.GIL_HUMAN).ToMeter();
            var chestDeepHeight =  GameData.cGuildValue.GetWaterDepthChest((int)DaedalusConst.Guild.GIL_HUMAN).ToMeter();
            var chestDeepHeightWithBuffer = chestDeepHeight + 2; // Do raycast a little bit longer than it needs to be, to ensure it's working.

            RaycastHit hit;

            var hitsAll = Physics.RaycastAll(transform.position, Vector3.up, kneeDeepHeight);
            var hits = Physics.RaycastAll(transform.position, Vector3.up, kneeDeepHeight, 1 << Constants.WaterLayer);
            foreach (var raycastHit in hits)
            { 
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.up) * raycastHit.distance, Color.yellow, 2f, false); 
            }
            
            // Water between our feet and our head found!
            if (Physics.Raycast(transform.position, Vector3.up, out hit, chestDeepHeightWithBuffer, 1 << Constants.WaterLayer))
            {
                var previousWaterLevel = _playerAi.WaterLevel;
                if (hit.distance < kneeDeepHeight)
                    _playerAi.WaterLevel = (int)ZenGineConst.WaterLevel.Normal;
                else if (hit.distance < chestDeepHeight)
                    _playerAi.WaterLevel = (int)ZenGineConst.WaterLevel.Knee;
                else
                    _playerAi.WaterLevel = (int)ZenGineConst.WaterLevel.Chest;

                if (previousWaterLevel != _playerAi.WaterLevel)
                    ChangeWaterBehavior();
            }
            // else
            // FIXME - We need to add a possiblity to reset water IF the hero is teleported away from water or falling through a waterfall, ...
            //         so that the water level is reset and we can walk again normally.
        }

        private void ChangeWaterBehavior()
        {
            switch ((ZenGineConst.WaterLevel)_playerAi.WaterLevel)
            {
                case ZenGineConst.WaterLevel.Normal:
                    Gravity = _initialGravity;
                    MoveSpeed = _initialMoveSpeed;
                    RunSpeed = _initialRunSpeed;
                    CanSprint = true;
                    MaxFallSpeed = _initialFallSpeed;
                    break;
                case ZenGineConst.WaterLevel.Knee:
                    Gravity = _initialGravity;
                    MoveSpeed = _initialMoveSpeed / 2;
                    RunSpeed = _initialMoveSpeed / 2; // No running, but it might be, that we run into deep water, then we need to slow it down like walking.
                    CanSprint = false;
                    MaxFallSpeed = _initialFallSpeed;
                    break;
                case ZenGineConst.WaterLevel.Chest:
                    Gravity = 0f;
                    MaxFallSpeed = 0f;
                    break;
                default:
                    throw new Exception($"Unknown value {_playerAi.WaterLevel} for water.");
            }
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
