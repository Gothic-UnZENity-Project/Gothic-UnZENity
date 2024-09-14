using GUZ.Core.Creator.Sounds;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.VR.Properties.VobItem;
using UnityEngine;
using ResourceLoader = GUZ.Core.ResourceLoader;

namespace GUZ.VR.Components.VobDoor
{
    public class VRDoorLockInteraction : MonoBehaviour
    {
        [SerializeField] private VobDoorProperties _properties;
        [SerializeField] private AudioSource _audioSource;
        
        private const string _lockInteractionColliderName = "LockPickInteraction";

        private string _combination = "LLRRL";
        private int _combinationPos = 0;
        
        public enum DoorLockStatus
        {
            StepSuccess,
            StepFailure,
            DoorUnlocked
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            PlaySound(Constants.Daedalus.DoorLockSoundName);
            
            var lockPickProperties = other.gameObject.GetComponentInParent<VRVobLockPickProperties>();
            lockPickProperties.IsInsideLock = true;
            lockPickProperties.ActiveDoorLock = this;
            _combinationPos = 0;
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            PlaySound(Constants.Daedalus.DoorLockSoundName);
            
            var lockPickProperties = other.gameObject.GetComponentInParent<VRVobLockPickProperties>();
            lockPickProperties.IsInsideLock = false;
            lockPickProperties.ActiveDoorLock = null;
        }

        public DoorLockStatus UpdateCombination(bool isLeft)
        {
            var currentChar = _combination[_combinationPos];
            var isCorrect = (isLeft && currentChar == 'L') || (!isLeft && currentChar == 'R');

            Debug.Log($"IsCorrect={isCorrect}, CombinationChar={currentChar}");
            
            if (isCorrect)
            {
                _combinationPos++;

                if (_combinationPos == _combination.Length)
                {
                    // FIXME - Handle "DoorUnlocked" (activate rotation of door).
                    PlaySound(Constants.Daedalus.PickLockUnlockSoundName, Constants.Daedalus.DoorUnlockSoundName);
                    return DoorLockStatus.DoorUnlocked;
                }

                PlaySound(Constants.Daedalus.PickLockSuccessSoundName);
                return DoorLockStatus.StepSuccess;
            }
            else
            {
                // TODO - Pseudo breaking for testings only
                if (Random.value > 0.5f)
                {
                    PlaySound(Constants.Daedalus.PickLockFailureSoundName);
                }
                else
                {
                    PlaySound(Constants.Daedalus.PickLockBrokenSoundName);
                }

                _combinationPos = 0;
                return DoorLockStatus.StepFailure;
            }
        }

        private void PlaySound(string soundName, string fallback = null)
        {
            var sound = ResourceLoader.TryGetSound(soundName);

            if (sound == null && fallback != null)
            {
                PlaySound(fallback);
                return;
            }
                
            _audioSource.PlayOneShot(SoundCreator.ToAudioClip(sound));
        }
    }
}
