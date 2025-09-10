#if GUZ_HVR_INSTALLED
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Services.Vm;
using GUZ.VR.Adapters.Vob.VobItem;
using GUZ.VR.Services;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.VR.Adapters.Vob.VobDoor
{
    public class VRDoorLockInteraction : MonoBehaviour
    {

        [SerializeField] private GameObject _rootGO;
        [SerializeField] private AudioSource _audioSource;

        [Inject] private readonly VRPlayerService _vrPlayerService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly VmService _vmService;

        private bool _isLocked;
        private string _combination;

        private const string _lockInteractionColliderName = "LockPickInteraction";

        // FIXME - Move into IDoor lab instance once provided by ZenKit.
        private int _combinationPos = 0;
        
        public enum DoorLockStatus
        {
            StepSuccess,
            StepFailure,
            Unlocked
        }


        private void Start()
        {
            var vob = GetComponentInParent<VobLoader>().Container.Vob;
            switch (vob)
            {
                case IDoor door:
                    _isLocked = door.IsLocked;
                    _combination = door.PickString;
                    break;
                case IContainer container:
                    _isLocked = container.IsLocked;
                    _combination = container.PickString;
                    break;
                default:
                    Logger.LogError($"VRDoorLockInteraction: No door or container found for >{vob.Name}<.", LogCat.VR);
                    break;
            }

            // Stop this handler if the object is already unlocked.
            if (!_isLocked)
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
                return;

            _combinationPos = 0;
            PlaySound(_vmService.DoorLockSoundName);

            var lockPickProperties = other.gameObject.GetComponentInParent<VRLockPickProperties>();
            lockPickProperties.IsInsideLock = true;
            lockPickProperties.ActiveDoorLock = this;

            if (_vrPlayerService.GrabbedItemLeft?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = _vrPlayerService.GrabbedItemLeft!.transform;
            }
            else if (_vrPlayerService.GrabbedObjectRight?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = _vrPlayerService.GrabbedObjectRight!.transform;
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

            PlaySound(_vmService.DoorLockSoundName);
            
            var lockPickProperties = other.gameObject.GetComponentInParent<VRLockPickProperties>();
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

                    PlaySound(_vmService.PickLockUnlockSoundName, _vmService.DoorUnlockSoundName);

                    return DoorLockStatus.Unlocked;
                }

                PlaySound(_vmService.PickLockSuccessSoundName);
                return DoorLockStatus.StepSuccess;
            }
            else
            {
                // TODO - Pseudo breaking for testings only
                if (Random.value > 0.5f)
                {
                    PlaySound(_vmService.PickLockFailureSoundName);
                }
                else
                {
                    PlaySound(_vmService.PickLockBrokenSoundName);
                }

                _combinationPos = 0;
                return DoorLockStatus.StepFailure;
            }
        }

        private void PlaySound(string soundName, string fallback = null)
        {
            var clip = _audioService.CreateAudioClip(soundName);
            
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

