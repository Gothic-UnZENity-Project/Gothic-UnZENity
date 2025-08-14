#if GUZ_HVR_INSTALLED
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vob;
using GUZ.VR.Components.VobItem;
using GUZ.VR.Manager;
using GUZ.VR.Properties.VobItem;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.VobDoor
{
    public class VRDoorLockInteraction : MonoBehaviour
    {
        [SerializeField] private GameObject _rootGO;
        [SerializeField] private AudioSource _audioSource;
        private IDoor _vobDoor;

        private const string _lockInteractionColliderName = "LockPickInteraction";

        // FIXME - Move into IDoor lab instance once provided by ZenKit.
        private string _combination = "LLRRL";
        private int _combinationPos = 0;
        
        public enum DoorLockStatus
        {
            StepSuccess,
            StepFailure,
            Unlocked
        }


        private void Start()
        {
            _vobDoor = GetComponentInParent<VobLoader>().Container.VobAs<IDoor>();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            // FIXME - For lab only. Remove once Interface of Door (IDoor) exists: https://github.com/GothicKit/ZenKitCS/pull/12
            // Mark all doors as locked in lab
            if (!_vobDoor.IsLocked)
                return;

            _combinationPos = 0;
            PlaySound(DaedalusConst.DoorLockSoundName);

            var lockPickProperties = other.gameObject.GetComponentInParent<VRVobLockPickProperties>();
            lockPickProperties.IsInsideLock = true;
            lockPickProperties.ActiveDoorLock = this;

            if (VRPlayerManager.GrabbedItemLeft?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = VRPlayerManager.GrabbedItemLeft!.transform;
            }
            else if (VRPlayerManager.GrabbedObjectRight?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = VRPlayerManager.GrabbedObjectRight!.transform;
            }
            else
            {
                Logger.LogError($"VRDoorLockInteraction: No hand found for grabbed object >{other.gameObject.name}<.", LogCat.VR);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            PlaySound(DaedalusConst.DoorLockSoundName);
            
            var lockPickProperties = other.gameObject.GetComponentInParent<VRVobLockPickProperties>();
            lockPickProperties.IsInsideLock = false;
            lockPickProperties.ActiveDoorLock = null;
            lockPickProperties.HoldingHand = null;
        }

        public DoorLockStatus UpdateCombination(bool isLeft)
        {
            var currentChar = _combination[_combinationPos];
            var isCorrect = (isLeft && currentChar == 'L') || (!isLeft && currentChar == 'R');

            Logger.Log($"IsCorrect={isCorrect}, CombinationChar={currentChar}", LogCat.VR);
            
            if (isCorrect)
            {
                _combinationPos++;

                if (_combinationPos == _combination.Length)
                {
                    // FIXME - Set door properties once IDoor interface is used with Lab implementation.
                    // _properties.DoorProperties.IsLocked = false;

                    // Reactivate rotation
                    _rootGO.GetComponentInChildren<ConfigurableJoint>().axis = Vector3.up;

                    PlaySound(DaedalusConst.PickLockUnlockSoundName, DaedalusConst.DoorUnlockSoundName);

                    return DoorLockStatus.Unlocked;
                }

                PlaySound(DaedalusConst.PickLockSuccessSoundName);
                return DoorLockStatus.StepSuccess;
            }
            else
            {
                // TODO - Pseudo breaking for testings only
                if (Random.value > 0.5f)
                {
                    PlaySound(DaedalusConst.PickLockFailureSoundName);
                }
                else
                {
                    PlaySound(DaedalusConst.PickLockBrokenSoundName);
                }

                _combinationPos = 0;
                return DoorLockStatus.StepFailure;
            }
        }

        private void PlaySound(string soundName, string fallback = null)
        {
            var clip = SoundCreator.ToAudioClip(soundName);
            
            if (clip == null && fallback != null)
            {
                PlaySound(fallback);
                return;
            }
                
            _audioSource.PlayOneShot(clip);
        }
    }
}
#endif

