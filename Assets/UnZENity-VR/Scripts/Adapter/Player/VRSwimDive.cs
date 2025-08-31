using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Model.Marvin;
using GUZ.Core.Vm;
using GUZ.VR.Adapter.HVROverrides;
using GUZ.VR.Services.Context;
using HurricaneVR.Framework.Core.HandPoser;
using HurricaneVR.Framework.Core.Utils;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.VR.Adapter.Player
{
    [RequireComponent(typeof(VRPlayerController), typeof(VRPlayerInputs))]
    public class VRSwimDive : MonoBehaviour, IMarvinPropertyCollector
    {
        [SerializeField] private VRPlayerController _playerController;
        [SerializeField] private VRPlayerInputs _playerInputs;
        
        private INpc _playerVob;
        private IAiHuman _playerAi;
        private SfxAdapter _sfxSwimAdapter;
        private SfxAdapter _sfxDiveAdapter;
        private SfxAdapter _sfxSwim2DiveAdapter;
        private SfxAdapter _sfxSwim2HangAdapter; // Normally when pulling out of water only, but we also use when dive -> swim.
        
        private HVRHandAnimator _leftHandAnimator;
        private HVRHandAnimator _rightHandAnimator;
            
        private float _initialGravity;
        private float _initialMoveSpeed;
        private float _initialRunSpeed;
        private float _initialFallSpeed;

        private VmGothicEnums.WalkMode _mode = VmGothicEnums.WalkMode.Walk;

        // Water Bobbing
        private Coroutine _waterBobbingCoroutine;
        [SerializeField] private float _waterBobAmplitude = 0.001f;
        [SerializeField] private float _waterBobFrequency = 2.5f;

        // Swim movement
        [SerializeField] private float _swimStartDiveVerticalVelocity = -5f;
        [SerializeField] private float _swimHandMovementMultiplier = 5f;
        [SerializeField] private float _swimVelocityFadeRate = 0.25f; // e.g., 0.25==75% less velocity with each second
        private bool _isSwimmingForceStarted; // If we lift the grips, we set it to false again to re-enable sounds later.
        
        // Dive movement
        private Vector3 _currentVelocity;
        [SerializeField] private float _diveHandMovementMultiplier = 2.5f;
        [SerializeField] private float _diveVelocityFadeRate = 0.25f; // 0.25==75% less velocity with each second
        private bool _isDivingForceStarted; // If we lift the grips, we set it to false again to re-enable sounds later.


        private void Start()
        {
            Shader.SetGlobalInt(Constants.ShaderPropertyWaterEffectToggle, 0);

            var vrPlayer = GameContext.ContextInteractionService.GetImpl<VRContextInteractionService>().GetVRPlayerController();
            _leftHandAnimator = vrPlayer.LeftHand.HandAnimator;
            _rightHandAnimator = vrPlayer.RightHand.HandAnimator;
            
			GlobalEventDispatcher.ZenKitBootstrapped.AddListener(() =>
            {
                var mds = ResourceLoader.TryGetModelScript("Humans")!;
                
                // FIXME - In G1, there are different sounds for SwimBack, Sideways, and Forward
                var swimAnim = mds.Animations.First(i => i.Name.EqualsIgnoreCase("s_SwimF"));
                var swimSfxName = swimAnim.SoundEffects.First().Name;
                _sfxSwimAdapter = VmInstanceManager.TryGetSfxData(swimSfxName)!;
                
                var diveAnim = mds.Animations.First(i => i.Name.EqualsIgnoreCase("s_DiveF"));
                var diveSfxName = diveAnim.SoundEffects.First().Name;
                _sfxDiveAdapter = VmInstanceManager.TryGetSfxData(diveSfxName)!;

                var swim2DiveAnim = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_Swim_2_Dive"));
                var swim2DiveSfxName = swim2DiveAnim.SoundEffects.First().Name;
                _sfxSwim2DiveAdapter = VmInstanceManager.TryGetSfxData(swim2DiveSfxName)!;
                
                var swim2HangAnim = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_Swim_2_Hang"));
                var swim2HangSfxName = swim2HangAnim.SoundEffects.First().Name;
                _sfxSwim2HangAdapter = VmInstanceManager.TryGetSfxData(swim2HangSfxName);
            });

            GlobalEventDispatcher.WorldSceneLoaded.AddListener(() =>
            {
                _playerVob = ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData()!.Vob;
                _playerAi = (IAiHuman)_playerVob.Ai;
            });
            
            _initialGravity = _playerController.Gravity;
            _initialMoveSpeed = _playerController.MoveSpeed;
            _initialRunSpeed = _playerController.RunSpeed;
            _initialFallSpeed = _playerController.MaxFallSpeed;
        }

        private void Update()
        {
            if (_playerAi == null)
                return;

            if (_mode == VmGothicEnums.WalkMode.Swim)
                HandleSwim();
            else if (_mode == VmGothicEnums.WalkMode.Dive)
                HandleDive();
        }
        
        private void FixedUpdate()
        {
            /*
             * 1. Check if we have water around us.
             * 2. Check if we need to "walk" as we are knee deep in water.
             * 3. Check if we need to swim as w are chest deep in water.
             */
            
			if (_playerVob == null)
				return;

            // FIXME - As we have different sizes of people in VR, we should use the size based on real heights.
            var kneeDeepHeight = GameData.GuildValues.GetWaterDepthKnee((int)DaedalusConst.Guild.GIL_HUMAN).ToMeter();
            var chestDeepHeight =  GameData.GuildValues.GetWaterDepthChest((int)DaedalusConst.Guild.GIL_HUMAN).ToMeter();
            var chestDeepHeightWithBuffer = chestDeepHeight + 2; // Do raycast a little bit longer than it needs to be, to ensure it's working.

            RaycastHit hit;

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
            if (_waterBobbingCoroutine != null)
            {
                StopCoroutine(_waterBobbingCoroutine);
                _waterBobbingCoroutine = null;
            }

            switch ((ZenGineConst.WaterLevel)_playerAi.WaterLevel)
            {
                case ZenGineConst.WaterLevel.Normal:
                    _playerController.Gravity = _initialGravity;
                    _playerController.MoveSpeed = _initialMoveSpeed;
                    _playerController.RunSpeed = _initialRunSpeed;
                    _playerController.CanSprint = true;
                    _playerController.MaxFallSpeed = _initialFallSpeed;
                    _leftHandAnimator.enabled = true;
                    _rightHandAnimator.enabled = true;
                    break;
                case ZenGineConst.WaterLevel.Knee:
                    // FIXME - Or is it "WalkMode.Water"?
                    _mode = VmGothicEnums.WalkMode.Walk;
                    Shader.SetGlobalInt(Constants.ShaderPropertyWaterEffectToggle, 0);

                    _playerController.Gravity = _initialGravity;
                    _playerController.MoveSpeed = _initialMoveSpeed / 2;
                    _playerController.RunSpeed = _initialMoveSpeed / 2; // No running, but it might be, that we run into deep water, then we need to slow it down like walking.
                    _playerController.CanSprint = false;
                    _playerController.MaxFallSpeed = _initialFallSpeed;
                    _leftHandAnimator.enabled = true;
                    _rightHandAnimator.enabled = true;
                    break;
                case ZenGineConst.WaterLevel.Chest:
                    // Dive -> Swim
                    if (_mode == VmGothicEnums.WalkMode.Dive)
                        SFXPlayer.Instance.PlaySFX(_sfxSwim2HangAdapter.GetRandomClip(), Camera.main!.transform.position);

                    Shader.SetGlobalInt(Constants.ShaderPropertyWaterEffectToggle, 0);
                    _mode = VmGothicEnums.WalkMode.Swim;
                    _playerController.Gravity = 0f;
                    _playerController.MaxFallSpeed = 0f;
                    _leftHandAnimator.enabled = false;
                    _rightHandAnimator.enabled = false;
                    
                    if (_waterBobbingCoroutine == null)
                        _waterBobbingCoroutine = StartCoroutine(WaterBobbing());
                    break;
                default:
                    throw new Exception($"Unknown value {_playerAi.WaterLevel} for water.");
            }
        }

        private IEnumerator WaterBobbing()
        {
            var time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                var offset = Mathf.Sin(time * _waterBobFrequency) * _waterBobAmplitude;
                _playerController.CharacterController.Move(Vector3.down * offset);
                yield return null;
            }
        }
        
        private void StartDive()
        {
            Shader.SetGlobalInt(Constants.ShaderPropertyWaterEffectToggle, 1);
            _mode = VmGothicEnums.WalkMode.Dive;

            // Reset velocity so that dive down is smoothed. Otherwise, we are on the ground already. ;-)
            // (After "forcing" diving by moving hands down.)
            _currentVelocity = Vector3.zero;
            
            _playerController.Gravity = 0;
            _playerController.MaxFallSpeed = 0;
            
            SFXPlayer.Instance.PlaySFX(_sfxSwim2DiveAdapter.GetRandomClip(), Camera.main!.transform.position);
        }

        /// <summary>
        /// Tested:
        /// Test 1. No translation
        /// var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _swimHandMovementMultiplier;
        /// _currentVelocity = Vector3.Lerp(_currentVelocity, -combinedVelocity, Time.deltaTime);
        ///
        /// [🆗] Moving physical head
        /// [🛑] Rotating player with Thumbstick
        ///
        ///
        /// Test 2. Translation with PlayerController (where Thumbstick rotation is applied)
        /// var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _swimHandMovementMultiplier;
        /// var rotatedVelocity = _playerController.transform.TransformDirection(combinedVelocity);
        /// _currentVelocity = Vector3.Lerp(_currentVelocity, -rotatedVelocity, Time.deltaTime);
        ///
        /// [🛑] Moving physical head
        /// [🆗] Rotating player with Thumbstick
        ///
        ///
        // Test 3. Translation with Camera.main!
        /// var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _swimHandMovementMultiplier;
        /// var rotatedVelocity = Camera.main!.transform.TransformDirection(combinedVelocity);
        /// _currentVelocity = Vector3.Lerp(_currentVelocity, -rotatedVelocity, Time.deltaTime);
        ///
        /// [🛑] Moving physical head
        /// [🆗] Rotating player with Thumbstick
        /// </summary>

        private void HandleSwim()
        {
            if (_playerInputs.BothGripsActiveState.JustActivated)
            {
                _isSwimmingForceStarted = true;
            }
            
            // Use force to pull yourself.
            if (_playerInputs.IsBothGripsActive)
            {
                // Calculate hand movement direction and force
                var leftHandVelocity = _playerInputs.LeftController.Velocity;
                var rightHandVelocity = _playerInputs.RightController.Velocity;
                var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _swimHandMovementMultiplier;
                
                // Transform velocity to world space based on player rotation
                // var rotatedVelocity = Camera.main!.transform.TransformDirection(combinedVelocity);
                
                // Apply opposite force for swimming - Only horizontal!
                _currentVelocity = Vector3.Lerp(_currentVelocity, -combinedVelocity, Time.deltaTime);
            }
            // Fade out movement
            else
            {
                _currentVelocity *= Mathf.Pow(_swimVelocityFadeRate, Time.deltaTime);
                _isSwimmingForceStarted = false;
            }
            
            if (_isSwimmingForceStarted && _currentVelocity.magnitude > 1f)
            {
                // Play a random swim sound via HVRs SFXPlayer.
                SFXPlayer.Instance.PlaySFX(_sfxSwimAdapter.GetRandomClip(), Camera.main!.transform.position);
                _isSwimmingForceStarted = false;
            }

            // Move the character - Horizontal only
            var rotatedHorizontalVelocity = new Vector3(_currentVelocity.x, 0, _currentVelocity.z);
            _playerController.CharacterController.Move(rotatedHorizontalVelocity * Time.deltaTime);

            // We seek -y aka "down"
            if (_currentVelocity.y < _swimStartDiveVerticalVelocity)
            {
                StartDive();
            }
        }

        private void HandleDive()
        {
            if (_playerInputs.BothGripsActiveState.JustActivated)
            {
                _isDivingForceStarted = true;
            }
            
            // Use force to pull yourself.
            if (_playerInputs.IsBothGripsActive)
            {
                // Calculate hand movement direction and force
                var leftHandVelocity = _playerInputs.LeftController.Velocity;
                var rightHandVelocity = _playerInputs.RightController.Velocity;
                var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _diveHandMovementMultiplier;

                // Transform velocity to world space based on player rotation
                // var rotatedVelocity = Camera.main!.transform.TransformDirection(combinedVelocity);

                // Apply opposite force for diving
                _currentVelocity = Vector3.Lerp(_currentVelocity, -combinedVelocity, Time.deltaTime);
            }
            // Fade out movement
            else
            {
                _currentVelocity *= Mathf.Pow(_diveVelocityFadeRate, Time.deltaTime);
                _isDivingForceStarted = false;
            }
            
            if (_isDivingForceStarted && _currentVelocity.magnitude > 1f)
            {
                // Play a random dive sound via HVRs SFXPlayer.
                SFXPlayer.Instance.PlaySFX(_sfxDiveAdapter.GetRandomClip(), Camera.main!.transform.position);
                _isDivingForceStarted = false;
            }

            // Move the character
            _playerController.CharacterController.Move(_currentVelocity * Time.deltaTime);
        }

        public IEnumerable<object> CollectMarvinInspectorProperties()
        {
            return new List<object>
            {
                new MarvinPropertyHeader("VRSwimDive - Water bobbing"),
                new MarvinProperty<float>(
                    "Amplitude",
                    () => _waterBobAmplitude,
                    value => _waterBobAmplitude = value,
                    0f, 0.01f),
                new MarvinProperty<float>(
                    "Frequency",
                    () => _waterBobFrequency,
                    value => _waterBobFrequency = value,
                    0f, 5f),

                new MarvinPropertyHeader("VRSwimDive - Swimming"),
                new MarvinProperty<float>(
                    "Force Dive - Vertical Velocity",
                    () => _swimStartDiveVerticalVelocity,
                    value => _swimStartDiveVerticalVelocity = value,
                    -10f, 0f),
                new MarvinProperty<float>(
                    "Hand Movement Multiplier",
                    () => _swimHandMovementMultiplier,
                    value => _swimHandMovementMultiplier = value,
                    0f, 10f),
                new MarvinProperty<float>(
                    "Velocity Fade Rate",
                    () => _swimVelocityFadeRate,
                    value => _swimVelocityFadeRate = value,
                    0f, 1f),

                new MarvinPropertyHeader("VRSwimDive - Diving"),
                new MarvinProperty<float>(
                    "Hand Movement Multiplier",
                    () => _diveHandMovementMultiplier,
                    value => _diveHandMovementMultiplier = value,
                    0f, 10f),
                new MarvinProperty<float>(
                    "Velocity Fade Rate",
                    () => _diveVelocityFadeRate,
                    value => _diveVelocityFadeRate = value,
                    0f, 1f),
            };
        }
    }
}
