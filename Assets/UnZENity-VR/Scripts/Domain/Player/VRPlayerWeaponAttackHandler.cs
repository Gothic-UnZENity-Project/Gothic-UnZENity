#if GUZ_HVR_INSTALLED
using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.VR.Manager;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using ZenKit;
using EventType = ZenKit.EventType;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Domain.Player
{
    public class VRPlayerWeaponAttackHandler
    {
        /**
         * Fight windows from animations:
         * eventTag(0 "DEF_HIT_LIMB"   "ZS_RIGHTHAND") --> This animation will check the named limbs (colliders) for attacks later.
         * eventTag(0 "DEF_OPT_FRAME"  "4          ...") --> START_FRAME...DEF_OPT_FRAME is range when an attack is recognized and collider checked each frame.
         * eventTag(0 "DEF_WINDOW"     "   10   33 ...") --> Attack window for combo. In this case: DEF_WINDOW1...DEF_HIT_END (10-31), 33 would be window for combo, we ignore for now.
         * eventTag(0 "DEF_HIT_END"    "      31   ...") --> End of attack itself.
         * eventSFX(5 "WHOOSH" EMPTY_SLOT)
         *
         * Used animations:
         * - s_1hAttack
         * - s_2hAttack
         */
        
        private bool _soundPlayed;
        private SfxAdapter _swingSwordSound;
        private float _soundPlayTime;

        private readonly CharacterController _characterController;
        private readonly Rigidbody _weaponWeaponRigidbody;
        private readonly Collider[] _weaponColliders;
        private bool _isLeftHand;
        private bool _isRightHand;

        private readonly float _attackVelocityThreshold;
        private readonly float _velocityDropPercentage;
        
        private float _attackWindowTime;
        private float _comboWindowStart;
        private float _comboWindowTime;

        // Velocity sampling properties
        private readonly float _velocityCheckDuration;
        private readonly int _velocitySampleCount;
        private readonly Queue<float> _velocityHistory = new();
        private float _velocityCheckTimer;
        private List<Collider> _alreadyHitCollidersForThisAttack = new();
        
        // Current state properties
        private float _currentWeaponVelocity => GetAverageVelocity();
        private float _overallFlowTime;
        private TimeWindow _currentWindow;
        
        // Internal tracking variables
        private bool _hasDroppedBelowThreshold;
        private bool _hasReturnedToThreshold;
        private float _velocityDropThreshold;

        /// <summary>
        /// Assumptions:
        /// 1. Attack window (collider hit check) always ends before the combo window starts.
        /// --attack---|--
        /// --------------|--combo---|
        ///
        /// 2. If we fail the combo window by doing e.g., "left-right" within the attack window, the combo is failed, and we need to wait.
        /// --attack---|--
        /// ------|fail--------------|
        /// </summary>
        private enum TimeWindow
        {
            Initial,
            ComboFailed,
            Attack,
            WaitingForCombo,
            Combo
        }
        
        // Events for external systems
        public Action OnAttackTriggered;
        public Action OnComboTriggered;
        public Action OnAttackMissed;


        public VRPlayerWeaponAttackHandler(Rigidbody weaponRigidbody, bool is2HD, HVRHandSide handSide, float attackVelocityThreshold,
            float velocityDropPercentage, float velocityCheckDuration, int velocitySampleCount)
        {
            _characterController = VRPlayerManager.VRInteractionAdapter.GetVRPlayerController().CharacterController;

            _weaponWeaponRigidbody = weaponRigidbody;
            _weaponColliders = _weaponWeaponRigidbody.GetComponentsInChildren<Collider>();

            // Initial hand setup
            _isLeftHand = handSide == HVRHandSide.Left;
            _isRightHand = !_isLeftHand;
            
            _attackVelocityThreshold =  attackVelocityThreshold;
            _velocityDropPercentage = velocityDropPercentage;

            var attackAnimation = GetAttackAnimation(is2HD);
            CalculateWindowTimes(attackAnimation);
            CalculateAttackSound(attackAnimation);

            _velocityCheckDuration = velocityCheckDuration;
            _velocitySampleCount = velocitySampleCount;

            _velocityDropThreshold = _attackVelocityThreshold * (1f - _velocityDropPercentage);
        }

        private IAnimation GetAttackAnimation(bool is2Hd)
        {
            // Left-Right attacks always have a useful combo setting. (The combo for 2H forward (s_2hAttack) is broken to use with combos.)
            var attackAnimationName = $"t_{(is2Hd ? "2" : "1")}hAttackL";

            // FIXME - Combo settings for hero with more skills are in overlay mds (e.g., HUMANS_1HST2.mds) Use for improved weapon handling.
            var mds = ResourceLoader.TryGetModelScript("Humans")!;
            return mds.Animations.First(i => i.Name.EqualsIgnoreCase(attackAnimationName));

        }

        /// <summary>
        /// After checking G1 animations (e.g., Humans.mds), we leverage the following data:
        /// 1. We assume our event calculations always start at frame 1.
        /// 2. DEF_OPT_FRAME is always from 1...x frame
        /// 3. DEF_WINDOW is for combo window and always goes between "x...y" frame.
        /// </summary>
        private void CalculateWindowTimes(IAnimation attackAnim)
        {
            var eventLimb = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.HitLimb);
            var eventLastHitFrame = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.OptimalFrame);
            var eventHitWindow = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.ComboWindow);

            if (eventLimb == null || eventLastHitFrame == null || eventHitWindow == null)
            {
                Logger.LogError(
                    $"Attack animation >{attackAnim.Name}< has missing at least one of the required events. Skipping fight for it.",
                    LogCat.VR);
                return;
            }

            // Limb --> Check if collider is ZS_RIGHTHAND. If not --> Error log
            if (!eventLimb.Slots.Item1.EqualsIgnoreCase("ZS_RIGHTHAND"))
                Logger.LogError(
                    $"Collider check for weapon attack is not ZS_RIGHTHAND. Others aren't handled so far. Current: {eventLimb.Slots.Item1}",
                    LogCat.VR);
            
            // LastHitFrame --> Define _attackWindowTime
            _attackWindowTime = eventLastHitFrame.Frames.First() / attackAnim.Fps;

            // HitWindow --> Define _comboWindowTime
            var hitWindows = eventHitWindow.Frames;
            if (hitWindows.Count < 2)
            {
                Logger.LogError(
                    $"Animation >{attackAnim.Name}< need to provide at least two windows (start-end). Skipping...",
                    LogCat.VR);
                return;
            }

            _comboWindowStart = hitWindows[0] / attackAnim.Fps;
            _comboWindowTime = hitWindows[1] - hitWindows[0] / attackAnim.Fps;
        }

        private void CalculateAttackSound(IAnimation attackAnim)
        {
            var soundAttack = attackAnim.SoundEffects.FirstOrDefault();

            if (soundAttack == null)
            {
                return;
            }
            
            _swingSwordSound = VmInstanceManager.TryGetSfxData(soundAttack.Name);
            _soundPlayTime = soundAttack.Frame / attackAnim.Fps;
        }

        public void FixedUpdate()
        {
            UpdateVelocityHistory();
            UpdateStateMachine();
        }

        public void AddLeftHand()
        {
            _isLeftHand = true;
        }

        public void AddRightHand()
        {
            _isRightHand = true;
        }

        public void RemoveLeftHand()
        {
            _isLeftHand = false;
        }

        public void RemoveRightHand()
        {
            _isRightHand = false;
        }

        /// <summary>
        /// Logic goes like this:
        /// To ignore some Controller tracking issues for a frame, we need to have a few samples of velocity before calculating the average.
        /// We also don't use every frame (a tracking issue could last for x-frames) but instead only a sample at each x-milliseconds.
        /// </summary>
        private void UpdateVelocityHistory()
        {
            _velocityCheckTimer += Time.fixedDeltaTime;

            if (_velocityCheckTimer < _velocityCheckDuration / _velocitySampleCount)
                return;

            // We need to subtract the current players movement. Otherwise a run will count as a swing.
            // TODO - Maybe we should subtract the V3 velocity instead of magnitude to countermeasure player movement?
            var currentVelocity = _weaponWeaponRigidbody.linearVelocity.magnitude - _characterController.velocity.magnitude;
            _velocityHistory.Enqueue(currentVelocity);
            
            // Keep only the required number of samples
            if (_velocityHistory.Count > _velocitySampleCount)
            {
                _velocityHistory.Dequeue();
            }
            
            _velocityCheckTimer = 0f;
        }
        
        private float GetAverageVelocity()
        {
            if (_velocityHistory.Count == 0)
                return 0f;

            var sum = _velocityHistory.Sum();
            return sum / _velocityHistory.Count;
        }
        
        private void UpdateStateMachine()
        {
            _overallFlowTime += Time.fixedDeltaTime;

            switch (_currentWindow)
            {
                case TimeWindow.Initial:
                    HandleInitialWindow();
                    break;
                case TimeWindow.Attack:
                    HandleAttackWindow();
                    break;
                case TimeWindow.ComboFailed:
                    // Simply wait until the whole "animation" is over and then start again.
                    if (_overallFlowTime >= _comboWindowTime)
                        _currentWindow = TimeWindow.Initial;
                    break;
                case TimeWindow.WaitingForCombo:
                    HandleWaitingForComboWindow();
                    break;
                case TimeWindow.Combo:
                    HandleComboWindow();
                    break;
            }
        }

        private void HandleSound()
        {
            if (_soundPlayed || _overallFlowTime <= _soundPlayTime)
                return;

            _soundPlayed = true;
            SFXPlayer.Instance.PlaySFX(_swingSwordSound.GetRandomClip(), _weaponWeaponRigidbody.position);
        }
        
        private void HandleInitialWindow()
        {
            if (_currentWeaponVelocity >= _attackVelocityThreshold)
                StartAttackWindow();
        }
        
        private void HandleAttackWindow()
        {
            HandleSound();

            if (CheckIfComboWindowFailed())
                return;

            CheckHitCollider();

            if (_overallFlowTime >= _attackWindowTime)
                _currentWindow = TimeWindow.WaitingForCombo;
        }

        private void CheckHitCollider()
        {
            var overlappingColliders = new List<Collider>();

            foreach (var weaponCollider in _weaponColliders)
            {
                // Handle different collider types
                switch (weaponCollider)
                {
                    case BoxCollider boxCollider:
                        overlappingColliders.AddRange(CheckBoxColliderOverlap(boxCollider));
                        break;
                    case CapsuleCollider capsuleCollider:
                        overlappingColliders.AddRange(CheckCapsuleColliderOverlap(capsuleCollider));
                        break;
                    default:
                        Logger.LogError($"Unsupported collider type for weapon hit detection: {weaponCollider.GetType().Name}", LogCat.VR);
                        continue;
                }
            }

            ProcessWeaponHits(overlappingColliders);
        }

        private Collider[] CheckBoxColliderOverlap(BoxCollider boxCollider)
        {
            CalculateBoxColliderOverlap(boxCollider, out var center, out var size, out var rotation);;

            var colliders = Physics.OverlapBox(center, size / 2, rotation, 1 << Constants.VobNpcOrMonster);

            return colliders;
        }

        public void CalculateBoxColliderOverlap(BoxCollider boxCollider, out Vector3 center, out Vector3 size,
            out Quaternion rotation)
        {
            var bounds = boxCollider.bounds;
            center = bounds.center;
            size = bounds.size;
            rotation = boxCollider.transform.rotation;
        }

        private Collider[] CheckCapsuleColliderOverlap(CapsuleCollider capsuleCollider)
        {
            CalculateCapsuleOverlap(capsuleCollider, out var point0, out var point1, out var radius);

            var colliders = Physics.OverlapCapsule(point0, point1, radius, 1 << Constants.VobNpcOrMonster);

            return colliders;
        }

        public void CalculateCapsuleOverlap(CapsuleCollider capsuleCollider, out Vector3 point0, out Vector3 point1,
            out float radius)
        {
            // Calculate capsule radius
            var bounds = capsuleCollider.bounds;
            var center = bounds.center;
            radius = capsuleCollider.radius * Mathf.Max(capsuleCollider.transform.lossyScale.x, capsuleCollider.transform.lossyScale.z);

            // Calculate capsule endpoints
            var height = capsuleCollider.height * capsuleCollider.transform.lossyScale.y;
            var direction = capsuleCollider.direction switch
            {
                0 => capsuleCollider.transform.right,
                1 => capsuleCollider.transform.up,
                2 => capsuleCollider.transform.forward,
                _ => throw new ArgumentOutOfRangeException()
            };

            var halfHeight = (height / 2) - radius;
            point0 = center + direction * halfHeight;
            point1 = center - direction * halfHeight;
        }

        private void ProcessWeaponHits(List<Collider> hitColliders)
        {
            foreach (var hitCollider in hitColliders)
            {
                if (_alreadyHitCollidersForThisAttack.Contains(hitCollider))
                    continue;
                else
                    _alreadyHitCollidersForThisAttack.Add(hitCollider);

                var npcContainer = hitCollider.GetComponentInParent<NpcLoader>().Container;
                

                Logger.Log($"Weapon hit detected on: {hitCollider.gameObject.name}", LogCat.VR);

                // Here you can add your hit processing logic, such as:
                // - Damage calculation
                // - Hit effects
                // - Sound effects
                // - Haptic feedback
                // - etc.

                // Example: Get the hit target component and process damage
                // var npcComponent = hitCollider.GetComponentInParent<INpc>();
                // if (npcComponent != null)
                // {
                //     npcComponent.TakeDamage(calculateDamage());
                // }
            }
        }


        private void HandleWaitingForComboWindow()
        {
            HandleSound();

            if (CheckIfComboWindowFailed())
                return;
            
            if (_overallFlowTime >= _comboWindowStart)
                StartComboWindow();
        }

        private bool CheckIfComboWindowFailed()
        {
            // Track velocity drops and returns
            if (_currentWeaponVelocity < _velocityDropThreshold && !_hasDroppedBelowThreshold)
            {
                _hasDroppedBelowThreshold = true;
            }

            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVelocityThreshold && !_hasReturnedToThreshold)
            {
                _hasReturnedToThreshold = true;
                // Combo failed - velocity dropped and returned during attack window
                _currentWindow = TimeWindow.ComboFailed;
                return true;
            }

            return false;
        }
        
        private void HandleComboWindow()
        {
            HandleSound();
            
            // Check for combo conditions

            // Check #1 - When we enter the ComboWindow or we're inside already, we need to have the sword at least dropping below threshold once.
            // Basically fighter changes velocity direction to do a left-right swing.
            if (!_hasDroppedBelowThreshold && _currentWeaponVelocity < _velocityDropThreshold)
                _hasDroppedBelowThreshold = true;

            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVelocityThreshold)
                _hasReturnedToThreshold = true;

            // It means e.g., we changed directions and got up to speed within combo window time. Now let's start the combo immediately.
            if (_hasReturnedToThreshold)
            {
                StartAttackWindow();
                return;
            }
            
            // Check if combo window time is up
            if (_overallFlowTime >= _comboWindowTime)
            {
                // Missed combo window
                OnAttackMissed?.Invoke();
                _currentWindow = TimeWindow.Initial;
            }
        }
        
        private void StartAttackWindow()
        {
            _currentWindow = TimeWindow.Attack;
            _overallFlowTime = 0f; // Restart timer

            // Restart failure checks.
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;
            _alreadyHitCollidersForThisAttack.Clear();

            // If no sound is set, ignore playing it and mark it as "played".
            _soundPlayed = _swingSwordSound == null;
            
            // Trigger attack
            OnAttackTriggered?.Invoke();
        }
        
        // FIXME - DEBUG values. Need to be adjustable via MarvinMode Inspector...
        private float _amplitude = 0.2f;
        private float _duration = 0.5f;
        private float _frequency = 50f;

        private void StartComboWindow()
        {
            _currentWindow = TimeWindow.Combo;
            
            // We restart velocity check now. It's expected, that the player changes velocity within this time window now!
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;

            if (_isLeftHand)
                VRPlayerManager.GetHand(HVRHandSide.Left).Vibrate(_amplitude, _duration, _frequency);
            if (_isRightHand)
                VRPlayerManager.GetHand(HVRHandSide.Right).Vibrate(_amplitude, _duration, _frequency);
        }
        
        private void ExecuteCombo()
        {
            OnComboTriggered?.Invoke();
        }
        
        // Public methods for external systems
        public bool IsInAttackState()
        {
            return _currentWindow == TimeWindow.Attack;
        }
        
        public bool IsInComboState()
        {
            return _currentWindow == TimeWindow.Combo;
        }
        
        public bool IsFailedComboState()
        {
            return _currentWindow == TimeWindow.ComboFailed;
        }
        
        public float GetRemainingStateTime()
        {
            return _overallFlowTime;
        }
        
        public float GetVelocityThreshold()
        {
            return _attackVelocityThreshold;
        }
        
        public float GetVelocityDropThreshold()
        {
            return _velocityDropThreshold;
        }
    }
}
#endif
