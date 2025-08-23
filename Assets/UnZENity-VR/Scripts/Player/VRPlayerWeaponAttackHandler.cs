#if GUZ_HVR_INSTALLED
using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.VR.Manager;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using EventType = ZenKit.EventType;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.VobItem
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
        
        
        private const string _swingSwordSfxName = "Whoosh";
        private SfxAdapter _swingSwordSound;


        private readonly CharacterController _characterController;
        private readonly Rigidbody _weaponWeaponRigidbody;
        private bool _isLeftHand;
        private bool _isRightHand;

        private readonly float _attackVelocityThreshold;
        private readonly float _velocityDropPercentage;
        
        private readonly float _attackWindowTime;
        private readonly float _comboWindowTime;
        private readonly float _cooldownWindowTime;
        
        // Velocity sampling properties
        private readonly float _velocityCheckDuration;
        private readonly int _velocitySampleCount;
        private readonly Queue<float> _velocityHistory = new();
        private float _velocityCheckTimer;
        
        // Current state properties
        private float _currentWeaponVelocity => GetAverageVelocity();
        private float _currentStateTime;
        private TimeWindow _currentWindow;
        
        // Internal tracking variables
        private bool _hasDroppedBelowThreshold;
        private bool _hasReturnedToThreshold;
        private float _velocityDropThreshold;
        
        private enum TimeWindow
        {
            InitialWindow,
            AttackWindow,
            AttackWindowComboFailed,
            AttackWindowComboWindow,
            ComboWindow,
            CooldownWindow
        }
        
        // Events for external systems
        public Action OnAttackTriggered;
        public Action OnComboTriggered;
        public Action OnAttackMissed;
        public Action OnCooldownStarted;


        public VRPlayerWeaponAttackHandler(Rigidbody weaponRigidbody, bool is2HD, HVRHandSide handSide, float attackVelocityThreshold,
            float velocityDropPercentage, float velocityCheckDuration, int velocitySampleCount)
        {
            _characterController = VRPlayerManager.VRInteractionAdapter.GetVRPlayerController().CharacterController;

            _weaponWeaponRigidbody = weaponRigidbody;

            // Initial hand setup
            _isLeftHand = handSide == HVRHandSide.Left;
            _isRightHand = !_isLeftHand;
            
            _attackVelocityThreshold =  attackVelocityThreshold;
            _velocityDropPercentage = velocityDropPercentage;

            CalculateWindowTimes(is2HD);

            _velocityCheckDuration = velocityCheckDuration;
            _velocitySampleCount = velocitySampleCount;
        
            _swingSwordSound = VmInstanceManager.TryGetSfxData(_swingSwordSfxName);
            
            InitializeState();
        }

        private void CalculateWindowTimes(bool is2Hd)
        {
            var attackAnimationName = $"s_{(is2Hd ? "2" : "1")}hAttack";

            var mds = ResourceLoader.TryGetModelScript("Humans")!;
            var attackAnim = mds.Animations.First(i => i.Name.EqualsIgnoreCase(attackAnimationName));
            var eventLimb = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.HitLimb);
            var eventLastHitFrame = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.OptimalFrame);
            var eventHitWindow = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.ComboWindow);
            var eventHitEnd = attackAnim.EventTags.FirstOrDefault(i => i.Type == EventType.HitEnd);
            var soundAttack = attackAnim.SoundEffects.FirstOrDefault();

            // Limb --> Check if collider is ZS_RIGHTHAND. If not --> Error log
            if (eventLimb == null || eventLastHitFrame == null || eventHitWindow == null || eventHitEnd == null)
            {
                Logger.LogError(
                    $"Attack animation >{attackAnimationName}< has missing at least one of the required events. Skipping fight for it.",
                    LogCat.VR);
                return;
            }

            if (!eventLimb.Slots.Item1.EqualsIgnoreCase("ZS_RIGHTHAND"))
                Logger.LogError($"Collider check for weapon attack is not ZS_RIGHTHAND. Others aren't handled so far. Current: {eventLimb.Slots.Item1}", LogCat.VR);

            
            // LastHitFrame --> Define _attackWindowTime
            // HitWindow --> Define _comboWindowTime
            // HitEnd --> _cooldownWindowTime -> basically Frames after HitWindow.
            //
            // _sfxSwimAdapter = VmInstanceManager.TryGetSfxData(swimSfxName)!;
            //
            //
            // _attackWindowTime = attackWindowTime;
            // _comboWindowTime = comboWindowTime;
            // _cooldownWindowTime = cooldownWindowTime;            
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
        
        private void InitializeState()
        {
            _currentWindow = TimeWindow.InitialWindow;
            _currentStateTime = 0f;
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;
            _velocityDropThreshold = _attackVelocityThreshold * (1f - _velocityDropPercentage);
        }
        
        private void UpdateStateMachine()
        {
            // Decrease current state time
            if (_currentStateTime > 0f)
                _currentStateTime -= Time.fixedDeltaTime;
            
            switch (_currentWindow)
            {
                case TimeWindow.InitialWindow:
                    HandleInitialWindow();
                    break;
                case TimeWindow.AttackWindow:
                    HandleAttackWindow();
                    break;
                case TimeWindow.AttackWindowComboFailed:
                    HandleAttackWindowComboFailed();
                    break;
                case TimeWindow.AttackWindowComboWindow:
                    HandleAttackWindowComboWindow();
                    break;
                case TimeWindow.ComboWindow:
                    HandleComboWindow();
                    break;
                case TimeWindow.CooldownWindow:
                    HandleCooldownWindow();
                    break;
            }
        }
        
        private void HandleInitialWindow()
        {
            if (_currentWeaponVelocity >= _attackVelocityThreshold)
                StartAttackWindow();
        }
        
        private void HandleAttackWindow()
        {
            // Track velocity drops and returns
            if (_currentWeaponVelocity < _velocityDropThreshold && !_hasDroppedBelowThreshold)
                _hasDroppedBelowThreshold = true;
            
            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVelocityThreshold && !_hasReturnedToThreshold)
            {
                _hasReturnedToThreshold = true;
                // Combo failed - velocity dropped and returned during attack window
                TransitionToAttackWindowComboFailed();
                return;
            }
            
            // Check if attack window time is up
            if (_currentStateTime <= 0f)
                StartComboWindow();
        }
        
        private void HandleAttackWindowComboFailed()
        {
            // Wait for the remaining attack window time, then go to cooldown
            if (_currentStateTime <= 0f)
                StartCooldownWindow();
        }
        
        private void HandleAttackWindowComboWindow()
        {
            // This state represents the overlap period where we can still enter combo window
            if (_currentStateTime <= 0f)
                StartComboWindow();
        }
        
        private void HandleComboWindow()
        {
            // Check for combo conditions

            // Check #1 - When we enter the ComboWindow or we're inside already, we need to have the sword at least dropping below threshold once.
            // Basically fighter changes velocity direction to do a left-right swing.
            if (!_hasDroppedBelowThreshold && _currentWeaponVelocity < _velocityDropThreshold)
                _hasDroppedBelowThreshold = true;

            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVelocityThreshold)
                _hasReturnedToThreshold = true;

            if (_hasReturnedToThreshold)
            {
                ExecuteCombo();
                StartAttackWindow();
                return;
            }
            
            // Check if combo window time is up
            if (_currentStateTime <= 0f)
            {
                // Missed combo window
                OnAttackMissed?.Invoke();
                StartCooldownWindow();
            }
        }
        
        private void HandleCooldownWindow()
        {
            // Ignore all velocity during cooldown
            if (_currentStateTime <= 0f)
                // Cooldown finished, return to initial window
                InitializeState();
        }
        
        private void StartAttackWindow()
        {
            _currentWindow = TimeWindow.AttackWindow;
            _currentStateTime = _attackWindowTime;
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;
            
            // Trigger attack
            OnAttackTriggered?.Invoke();
            
            SFXPlayer.Instance.PlaySFX(_swingSwordSound.GetRandomClip(), _weaponWeaponRigidbody.position);
        }
        
        private void TransitionToAttackWindowComboFailed()
        {
            _currentWindow = TimeWindow.AttackWindowComboFailed;
            // Keep the remaining time from attack window
        }
        

        // FIXME - DEBUG values. Need to be adjustable via MarvinMode Inspector...
        private float _amplitude = 0.2f;
        private float _duration = 0.5f;
        private float _frequency = 50f;

        private void StartComboWindow()
        {
            _currentWindow = TimeWindow.ComboWindow;
            _currentStateTime = _comboWindowTime;
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;

            if (_isLeftHand)
            {
                VRPlayerManager.GetHand(HVRHandSide.Left).Vibrate(_amplitude, _duration, _frequency);
            }
            if (_isRightHand)
            {
                VRPlayerManager.GetHand(HVRHandSide.Right).Vibrate(_amplitude, _duration, _frequency);
            }
        }
        
        private void ExecuteCombo()
        {
            OnComboTriggered?.Invoke();
        }
        
        private void StartCooldownWindow()
        {
            _currentWindow = TimeWindow.CooldownWindow;
            _currentStateTime = _cooldownWindowTime;
            
            OnCooldownStarted?.Invoke();
        }
        
        // Public methods for external systems
        public bool IsInAttackState()
        {
            return _currentWindow == TimeWindow.AttackWindow || 
                   _currentWindow == TimeWindow.AttackWindowComboWindow;
        }
        
        public bool IsInComboState()
        {
            return _currentWindow == TimeWindow.ComboWindow;
        }
        
        public bool IsInCooldown()
        {
            return _currentWindow == TimeWindow.CooldownWindow;
        }
        
        public float GetRemainingStateTime()
        {
            return _currentStateTime;
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
