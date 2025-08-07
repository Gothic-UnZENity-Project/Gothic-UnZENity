#if GUZ_HVR_INSTALLED
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    public class VRPlayerWeaponTimeHandler
    {
        private readonly Rigidbody _weaponRigidBody;

        private readonly float _attackVlocityThreshold;
        private readonly float _velocityDropPercentage;
        
        private readonly float _attackWindowTime;
        private readonly float _comboWindowTime;
        private readonly float _cooldownWindowTime;
        
        
        // Current state properties
        private float _currentWeaponVelocity => _weaponRigidBody.linearVelocity.magnitude;
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="attackVlocityThreshold"></param>
        /// <param name="velocityDropPercentage">e.g., 10% drop</param>
        /// <param name="attackWindowTime"></param>
        /// <param name="comboWindowTime"></param>
        /// <param name="cooldownWindowTime"></param>
        public VRPlayerWeaponTimeHandler(Rigidbody rigidBody, float attackVlocityThreshold = 5.0f,
            float velocityDropPercentage = 0.1f, float attackWindowTime = 1.0f, float comboWindowTime = 0.5f,
            float cooldownWindowTime = 2.0f)
        {
            _weaponRigidBody = rigidBody;
            _attackVlocityThreshold =  attackVlocityThreshold;
            _velocityDropPercentage = velocityDropPercentage;
        
            _attackWindowTime = attackWindowTime;
            _comboWindowTime = comboWindowTime;
            _cooldownWindowTime = cooldownWindowTime;
        
            InitializeState();
        }
        
        public void FixedUpdate()
        {
            UpdateStateMachine();
        }
        
        private void InitializeState()
        {
            _currentWindow = TimeWindow.InitialWindow;
            _currentStateTime = 0f;
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;
            _velocityDropThreshold = _attackVlocityThreshold * (1f - _velocityDropPercentage);
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
            if (_currentWeaponVelocity >= _attackVlocityThreshold)
                StartAttackWindow();
        }
        
        private void HandleAttackWindow()
        {
            // Track velocity drops and returns
            if (_currentWeaponVelocity < _velocityDropThreshold && !_hasDroppedBelowThreshold)
                _hasDroppedBelowThreshold = true;
            
            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVlocityThreshold && !_hasReturnedToThreshold)
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
            var comboConditionMet = _currentWeaponVelocity < _attackVlocityThreshold;
            
            if (!_hasDroppedBelowThreshold && _currentWeaponVelocity < _velocityDropThreshold)
                _hasDroppedBelowThreshold = true;
            
            if (_hasDroppedBelowThreshold && _currentWeaponVelocity >= _attackVlocityThreshold && !_hasReturnedToThreshold)
            {
                _hasReturnedToThreshold = true;
                comboConditionMet = true;
            }
            
            if (comboConditionMet)
            {
                ExecuteCombo();
                StartSecondAttackWindow();
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
            
            Debug.Log("Attack Window Started");
        }
        
        private void TransitionToAttackWindowComboFailed()
        {
            _currentWindow = TimeWindow.AttackWindowComboFailed;
            // Keep the remaining time from attack window
            
            Debug.Log("Combo Failed During Attack Window");
        }
        
        private void StartComboWindow()
        {
            _currentWindow = TimeWindow.ComboWindow;
            _currentStateTime = _comboWindowTime;
            _hasDroppedBelowThreshold = _currentWeaponVelocity <= _attackVlocityThreshold;
            _hasReturnedToThreshold = false;
            
            Debug.Log("Combo Window Started");
        }
        
        private void ExecuteCombo()
        {
            OnComboTriggered?.Invoke();
            Debug.Log("Combo Executed!");
        }
        
        private void StartSecondAttackWindow()
        {
            _currentWindow = TimeWindow.AttackWindow;
            _currentStateTime = _attackWindowTime;
            _hasDroppedBelowThreshold = false;
            _hasReturnedToThreshold = false;
            
            Debug.Log("Second Attack Window Started");
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
            return _attackVlocityThreshold;
        }
        
        public float GetVelocityDropThreshold()
        {
            return _velocityDropThreshold;
        }
    }
}
#endif
