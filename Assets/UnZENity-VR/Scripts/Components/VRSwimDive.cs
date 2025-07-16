using System;
using System.Collections;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Vm;
using GUZ.VR.Components.HVROverrides;
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

        private float _initialGravity;
        private float _initialMoveSpeed;
        private float _initialRunSpeed;
        private float _initialFallSpeed;

        private VmGothicEnums.WalkMode _mode = VmGothicEnums.WalkMode.Walk;
        
        private void Start()
        {
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

            if (_mode == VmGothicEnums.WalkMode.Swim && _playerInputs.IsBothGripsActivated)
            {
                StopCoroutine(_waterBobbingCoroutine);
                StartCoroutine(StartDive());
            }
            
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
                    break;
                case ZenGineConst.WaterLevel.Knee:
                    _playerController.Gravity = _initialGravity;
                    _playerController.MoveSpeed = _initialMoveSpeed / 2;
                    _playerController.RunSpeed = _initialMoveSpeed / 2; // No running, but it might be, that we run into deep water, then we need to slow it down like walking.
                    _playerController.CanSprint = false;
                    _playerController.MaxFallSpeed = _initialFallSpeed;
                    break;
                case ZenGineConst.WaterLevel.Chest:
                    _mode = VmGothicEnums.WalkMode.Swim;
                    _playerController.Gravity = 0f;
                    _playerController.MaxFallSpeed = 0f;
                    _waterBobbingCoroutine = StartCoroutine(WaterBobbing());
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
    }
}
