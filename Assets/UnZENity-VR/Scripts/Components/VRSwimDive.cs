using System;
using System.Collections;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using GUZ.VR.Adapter;
using GUZ.VR.Components.HVROverrides;
using HurricaneVR.Framework.Core.HandPoser;
using HurricaneVR.Framework.Core.Utils;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.VR.Components
{
    [RequireComponent(typeof(VRPlayerController), typeof(VRPlayerInputs))]
    public class VRSwimDive : MonoBehaviour
    {
        [SerializeField] private VRPlayerController _playerController;
        [SerializeField] private VRPlayerInputs _playerInputs;
        
        private INpc _playerVob;
        private IAiHuman _playerAi;
        private SfxContainer _sfxDiveContainer;
        private HVRHandAnimator _leftHandAnimator;
        private HVRHandAnimator _rightHandAnimator;
            
        private float _initialGravity;
        private float _initialMoveSpeed;
        private float _initialRunSpeed;
        private float _initialFallSpeed;

        private VmGothicEnums.WalkMode _mode = VmGothicEnums.WalkMode.Walk;
        
        private void Start()
        {
            var vrPlayer = ((VRInteractionAdapter)GameContext.InteractionAdapter).GetVRPlayerController();
            _leftHandAnimator = vrPlayer.LeftHand.HandAnimator;
            _rightHandAnimator = vrPlayer.RightHand.HandAnimator;
            
			GlobalEventDispatcher.ZenKitBootstrapped.AddListener(() =>
            {
                var mds = ResourceLoader.TryGetModelScript("Humans")!;
                var anim = mds.Animations.First(i => i.Name.EqualsIgnoreCase("s_DiveF"));
                var diveSfxName = anim.SoundEffects.First().Name;
                _sfxDiveContainer = VmInstanceManager.TryGetSfxData(diveSfxName)!;
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
                    _playerController.Gravity = _initialGravity;
                    _playerController.MoveSpeed = _initialMoveSpeed / 2;
                    _playerController.RunSpeed = _initialMoveSpeed / 2; // No running, but it might be, that we run into deep water, then we need to slow it down like walking.
                    _playerController.CanSprint = false;
                    _playerController.MaxFallSpeed = _initialFallSpeed;
                    _leftHandAnimator.enabled = true;
                    _rightHandAnimator.enabled = true;
                    break;
                case ZenGineConst.WaterLevel.Chest:
                    _mode = VmGothicEnums.WalkMode.Swim;
                    _playerController.Gravity = 0f;
                    _playerController.MaxFallSpeed = 0f;
                    _waterBobbingCoroutine = StartCoroutine(WaterBobbing());
                    _leftHandAnimator.enabled = false;
                    _rightHandAnimator.enabled = false;
                    break;
                default:
                    throw new Exception($"Unknown value {_playerAi.WaterLevel} for water.");
            }
        }

        private Coroutine _waterBobbingCoroutine;
        private const float _waterBobAmplitude = 0.001f;
        private const float _waterBobFrequency = 2.5f;

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
        
        private const float _diveStartGravityDownTime = 0.75f;
        
        private IEnumerator StartDive()
        {
            _mode = VmGothicEnums.WalkMode.Dive;
            
            _playerController.Gravity = _initialGravity / 2;
            _playerController.MaxFallSpeed = _initialFallSpeed / 2;
            yield return new WaitForSeconds(_diveStartGravityDownTime);
            _playerController.Gravity = 0;
            _playerController.MaxFallSpeed = 0;
        }
        
        [SerializeField] private float _swimStartDiveVerticalVelocity = 5f;
        [SerializeField] private float _swimHandMovementMultiplier = 7.5f;
        [SerializeField] private float _swimVelocityFadeRate = 0.25f; // e.g., 0.25==75% less velocity with each second
        
        private void HandleSwim()
        {
            // Use force to pull yourself.
            if (_playerInputs.IsBothGripsActive)
            {
                // Calculate hand movement direction and force
                var leftHandVelocity = _playerInputs.LeftController.Velocity;
                var rightHandVelocity = _playerInputs.RightController.Velocity;
                var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _swimHandMovementMultiplier;
                
                // Transform velocity to world space based on player rotation
                var rotatedVelocity = transform.TransformDirection(combinedVelocity);
                
                // Apply opposite force for swimming - Only horizontal!
                _currentVelocity = Vector3.Lerp(_currentVelocity, -rotatedVelocity, Time.deltaTime);
            }
            // Fade out movement
            else
            {
                _currentVelocity *= Mathf.Pow(_velocityFadeRate, Time.deltaTime);
            }

            // Move the character - Horizontal only
            var rotatedHorizontalVelocity = new Vector3(_currentVelocity.x, 0, _currentVelocity.z);
            _playerController.CharacterController.Move(rotatedHorizontalVelocity * Time.deltaTime);

            if (_currentVelocity.y > _swimStartDiveVerticalVelocity)
            {
                StopCoroutine(_waterBobbingCoroutine);
                StartCoroutine(StartDive());
            }
        }
        
        private Vector3 _currentVelocity;
        [SerializeField] private float _handMovementMultiplier = 5f;
        [SerializeField] private float _velocityFadeRate = 0.25f; // 0.25==75% less velocity with each second
        private bool _isDivingForceStarted; // If we lift the grips, we set it to false again to re-enable sounds later.
        
        private void HandleDive()
        {
            // Use force to pull yourself.
            if (_playerInputs.IsBothGripsActive)
            {
                if (!_isDivingForceStarted)
                {
                    // Play a random dive sound via HVRs SFXPlayer.
                    SFXPlayer.Instance.PlaySFX(_sfxDiveContainer.GetRandomClip(), Camera.main!.transform.position);
                    _isDivingForceStarted = true;
                }
                
                // Calculate hand movement direction and force
                var leftHandVelocity = _playerInputs.LeftController.Velocity;
                var rightHandVelocity = _playerInputs.RightController.Velocity;
                var combinedVelocity = (leftHandVelocity + rightHandVelocity) * _handMovementMultiplier;

                // Transform velocity to world space based on player rotation
                var rotatedVelocity = transform.TransformDirection(combinedVelocity);

                // Apply opposite force for diving
                _currentVelocity = Vector3.Lerp(_currentVelocity, -rotatedVelocity, Time.deltaTime);
            }
            // Fade out movement
            else
            {
                _currentVelocity *= Mathf.Pow(_velocityFadeRate, Time.deltaTime);
                _isDivingForceStarted = false;
            }

            // Move the character
            _playerController.CharacterController.Move(_currentVelocity * Time.deltaTime);
        }
    }
}
