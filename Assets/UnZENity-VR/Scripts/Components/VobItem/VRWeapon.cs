using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Data.Adapter;
using GUZ.Core.Manager;
using GUZ.Core.Marvin;
using GUZ.Core.Npc;
using GUZ.Core.Vm;
using GUZ.VR.Components.HVROverrides;
using UnityEngine;

namespace GUZ.VR.Components.VobItem
{
    /// <summary>
    /// First implementation of VR weapon movement tracker.
    /// FIXME:
    /// 1. Only check velocity when in hand
    /// 2. Vibrate hand which holds item only
    /// 
    /// </summary>
    public class VRWeapon : MonoBehaviour, IMarvinPropertyCollector
    {
        [SerializeField] private bool _debugSomething;
        
        [SerializeField] private Rigidbody _rigidBody;
        [SerializeField] private AudioSource _audioSource;
        
        [SerializeField] private float _attackVelocityThreshold = 2.5f;
        [SerializeField] private float _velocityCheckDuration = 0.5f;
        [SerializeField] private int _velocitySampleCount = 5;
        [SerializeField] private float _velocityCooldownAfterExecution = -0.8f; // e.g., wait 0.8 seconds before starting velocity check again.
        
        private Queue<float> _velocityHistory = new();
        private float _velocityCheckTimer;
        private bool _isAttacking;
        
        
        void Update()
        {
            UpdateVelocityHistory();
            CheckAttackState();
        }

        // Add this method to your VRWeapon class
        private void OnCollisionEnter(Collision collision)
        {
            if (!_isAttacking)
                return;
            
            var npcLoader = collision.gameObject.GetComponentInParent<NpcLoader>();

            if (npcLoader == null)
                return;

            npcLoader.Container.PrefabProps.AiHandler.enabled = false; // HACK: Stop AI logic.
            PhysicsHelper.DisablePhysicsForNpc(npcLoader.Container.PrefabProps);
            npcLoader.Container.PrefabProps.AnimationSystem.PlayAnimation("t_Dead");
        }
    
        private void UpdateVelocityHistory()
        {
            _velocityCheckTimer += Time.deltaTime;

            // Attacking is active until our timeout is reached.
            // FIXME - Add better logic which is active for a few frames.
            if (_velocityCheckTimer > 0f)
                _isAttacking = false;

            if (_velocityCheckTimer < _velocityCheckDuration / _velocitySampleCount)
                return;

            var currentVelocity = _rigidBody.linearVelocity.magnitude;
            _velocityHistory.Enqueue(currentVelocity);
            
            // Keep only the required number of samples
            if (_velocityHistory.Count > _velocitySampleCount)
            {
                _velocityHistory.Dequeue();
            }
            
            _velocityCheckTimer = 0f;
        }

        [SerializeField] private float amplitude = 0.2f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float frequency = 50f;
        
        private void CheckAttackState()
        {
            if (_velocityHistory.Count < _velocitySampleCount)
                return;
            
            var averageVelocity = GetAverageVelocity();

            if (averageVelocity > _attackVelocityThreshold)
            {
                GameContext.InteractionAdapter.GetCurrentPlayerController().GetComponent<VRPlayerController>().LeftHand.Controller.Vibrate(amplitude, duration, frequency);
                GameContext.InteractionAdapter.GetCurrentPlayerController().GetComponent<VRPlayerController>().RightHand.Controller.Vibrate(amplitude, duration, frequency);

                _velocityHistory.Clear(); // Reset to have the sound being played in x frames again only.
                _velocityCheckTimer = _velocityCooldownAfterExecution;
                _isAttacking = true;
            }
        }
    
        private float GetAverageVelocity()
        {
            var sum = _velocityHistory.Sum();
            return sum / _velocityHistory.Count;
        }

        public IEnumerable<object> CollectMarvinInspectorProperties()
        {
            return new List<object>
            {
                new MarvinPropertyHeader("VRWeapon - RigidBody"),
                new MarvinProperty<float>(
                    "Mass",
                    () => _rigidBody.mass,
                    value => _rigidBody.mass = value,
                    0f, 50f),
                new MarvinProperty<float>(
                    "Linear Damping",
                    () => _rigidBody.linearDamping,
                    value => _rigidBody.linearDamping = value,
                    0f, 2f),
                new MarvinProperty<float>(
                    "Angular Damping",
                    () => _rigidBody.angularDamping,
                    value => _rigidBody.angularDamping = value,
                    0f, 2f),
                
                new MarvinPropertyHeader("VRWeapon - Fight - Velocity"),
                new MarvinProperty<float>(
                    "Threshold",
                    () => _attackVelocityThreshold,
                    value => _attackVelocityThreshold = value,
                    0f, 10f),
                new MarvinProperty<float>(
                    "Check Duration",
                    () => _velocityCheckDuration,
                    value => _velocityCheckDuration = value,
                    0.1f, 2f),
                new MarvinProperty<int>(
                    "Sample Count",
                    () => _velocitySampleCount,
                    value => _velocitySampleCount = value,
                    1, 20),
                    
                new MarvinPropertyHeader("VRWeapon - Fight - Vibration"),
                new MarvinProperty<float>(
                    "Amplitude",
                    () => amplitude,
                    value => amplitude = value,
                    0f, 1f),
                new MarvinProperty<float>(
                    "Duration", 
                    () => duration,
                    value => duration = value,
                    0.1f, 2f),
                new MarvinProperty<float>(
                    "Frequency",
                    () => frequency,
                    value => frequency = value,
                    1f, 100f)
            };
        }
    }
}
