#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Vm;
using GUZ.VR.Manager;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRPlayerWeaponAttackHandler
    {
        private const string _swingSwordSfxName = "Whoosh";
        private SfxAdapter _swingSwordSound;

        
        private readonly Rigidbody _weaponRigidBody;
        private readonly HVRHandSide _handSide;

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
        public System.Action OnAttackTriggered;
        public System.Action OnComboTriggered;
        public System.Action OnAttackMissed;
        public System.Action OnCooldownStarted;


        public VRPlayerWeaponAttackHandler(Rigidbody rigidBody, HVRHandSide handSide, float attackVelocityThreshold,
            float velocityDropPercentage, float attackWindowTime, float comboWindowTime,
            float cooldownWindowTime, float velocityCheckDuration, int velocitySampleCount)
        {
            _weaponRigidBody = rigidBody;
            _handSide = handSide;
            
            _attackVelocityThreshold =  attackVelocityThreshold;
            _velocityDropPercentage = velocityDropPercentage;
        
            _attackWindowTime = attackWindowTime;
            _comboWindowTime = comboWindowTime;
            _cooldownWindowTime = cooldownWindowTime;

            _velocityCheckDuration = velocityCheckDuration;
            _velocitySampleCount = velocitySampleCount;
        
            _swingSwordSound = VmInstanceManager.TryGetSfxData(_swingSwordSfxName);
            
            InitializeState();
        }
        
        public void FixedUpdate()
        {
            UpdateVelocityHistory();
            UpdateStateMachine();
        }
        
        private void UpdateVelocityHistory()
        {
            if (_weaponRigidBody == null)
                return;
                
            _velocityCheckTimer += Time.fixedDeltaTime;

            if (_velocityCheckTimer < _velocityCheckDuration / _velocitySampleCount)
                return;

            var currentVelocity = _weaponRigidBody.linearVelocity.magnitude;
            _velocityHistory.Enqueue(currentVelocity);
            
            // Keep only the required number of samples
            if (_velocityHistory.Count > _velocitySampleCount)
            {
                _velocityHistory.Dequeue();

                Debug.Log("Current Velocity: " + GetAverageVelocity());
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
            
            SFXPlayer.Instance.PlaySFX(_swingSwordSound.GetRandomClip(), _weaponRigidBody.position);
            
            Debug.Log("Attack Window Started");
        }
        
        private void TransitionToAttackWindowComboFailed()
        {
            _currentWindow = TimeWindow.AttackWindowComboFailed;
            // Keep the remaining time from attack window
            
            Debug.Log("Combo Failed During Attack Window");
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
            
            VRPlayerManager.GetHand(_handSide).Vibrate(_amplitude, _duration, _frequency);
            
            Debug.Log("Combo Window Started");
        }
        
        private void ExecuteCombo()
        {
            OnComboTriggered?.Invoke();
            Debug.Log("Combo Executed!");
        }
        
        private void StartCooldownWindow()
        {
            _currentWindow = TimeWindow.CooldownWindow;
            _currentStateTime = _cooldownWindowTime;
            
            OnCooldownStarted?.Invoke();
            Debug.Log("Cooldown Window Started");
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
