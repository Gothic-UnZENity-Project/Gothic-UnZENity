using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Data.Container;
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
    public class VRWeapon : MonoBehaviour
    {
        private const string _swingSwordSfxName = "Whoosh";
        
        [SerializeField] private Rigidbody _rigidBody;
        [SerializeField] private AudioSource _audioSource;
        
        private SfxContainer _swingSwordSound;
        
        [SerializeField] private float _attackVelocityThreshold = 2.5f;
        [SerializeField] private float _velocityCheckDuration = 0.5f;
        [SerializeField] private int _velocitySampleCount = 5;
        [SerializeField] private float _velocityCooldownAfterExecution = -0.8f; // e.g., wait 0.8 seconds before starting velocity check again.
        
        private Queue<float> _velocityHistory = new();
        private float _velocityCheckTimer;
        
        
        
        private void Start()
        {
            _swingSwordSound = VmInstanceManager.TryGetSfxData(_swingSwordSfxName);
        }
        
        void Update()
        {
            UpdateVelocityHistory();
            CheckAttackState();
        }
    
        private void UpdateVelocityHistory()
        {
            _velocityCheckTimer += Time.deltaTime;

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

                _audioSource.PlayOneShot(_swingSwordSound.GetRandomClip());
                _velocityHistory.Clear(); // Reset to have the sound being played in x frames again only.
                _velocityCheckTimer = _velocityCooldownAfterExecution;
            }
        }
    
        private float GetAverageVelocity()
        {
            var sum = _velocityHistory.Sum();
            return sum / _velocityHistory.Count;
        }

    }
}
