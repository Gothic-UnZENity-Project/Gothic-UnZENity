#if GUZ_HVR_INSTALLED
using System.Collections;
using GUZ.Core;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Models.Container;
using GUZ.Core.Services.Vm;
using GUZ.VR.Adapters.Vob.VobItem;
using GUZ.VR.Services;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.VR.Adapters.Vob.VobDoor
{
    public class VRLockPickingInteraction : MonoBehaviour
    {

        [SerializeField] private GameObject _rootGO;
        [SerializeField] private AudioSource _audioSource;

        [Inject] private readonly VRPlayerService _vrPlayerService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly VmService _vmService;
        [Inject] private readonly VrHapticsService _hapticsService;

        private bool _isLocked;
        private string _combination;
        private HVRHandSide _handSide;
        private VobContainer _lockPick;
        private VobContainer _lockable;

        private const string _lockInteractionColliderName = "LockPickInteraction";

        private int _combinationPos = 0;
        
        public enum DoorLockStatus
        {
            StepSuccess,
            StepFailure,
            Unlocked
        }


        private void Start()
        {
            _lockable = GetComponentInParent<VobLoader>().Container;
            switch (_lockable.Vob)
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
                    Logger.LogError($"VRDoorLockInteraction: No door or container found for >{_lockable.Vob.Name}<.", LogCat.VR);
                    break;
            }

            // Stop this handler if the object is already unlocked.
            if (!_isLocked)
                gameObject.SetActive(false);

            StartCoroutine(StartDelayed());
        }

        private IEnumerator StartDelayed()
        {
            yield return null;
            
            // FIXME - When door/chest is unlocked, move Joint a little. Maybe there is an ease function at HVR for it.
            
            // Deactivate rotation
            // FIXME - Doesn't work! Door still opening, Chest lid rotating horizontally.
            _rootGO.GetComponentInChildren<ConfigurableJoint>().LockAllAngularMotion();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
                return;

            _combinationPos = 0;
            PlaySound(_vmService.DoorLockSoundName);

            // For later event usage.
            _lockPick = other.gameObject.GetComponentInParent<VobLoader>().Container;

            var lockPickProperties = other.gameObject.GetComponentInParent<VRLockPickProperties>();
            lockPickProperties.IsInsideLock = true;
            lockPickProperties.ActiveLockPicking = this;


            if (_vrPlayerService.GrabbedItemLeft?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = _vrPlayerService.GrabbedItemLeft!.transform;
                _handSide = HVRHandSide.Left;
                _hapticsService.Vibrate(HVRHandSide.Left, VrHapticsService.VibrationType.Info);
            }
            else if (_vrPlayerService.GrabbedObjectRight?.GetComponentInChildren<VRLockPickInteraction>().gameObject == other.gameObject)
            {
                lockPickProperties.HoldingHand = _vrPlayerService.GrabbedObjectRight!.transform;
                _handSide = HVRHandSide.Right;
                _hapticsService.Vibrate(HVRHandSide.Right, VrHapticsService.VibrationType.Info);
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
            lockPickProperties.ActiveLockPicking = null;
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

                // Opened!
                if (_combinationPos == _combination.Length)
                {
                    // FIXME - handle lock state of Interactable in event catching service.
                    GlobalEventDispatcher.LockPickComboFinished.Invoke(_lockPick, _lockable, (int)_handSide);

                    // Reactivate rotation
                    _rootGO.GetComponentInChildren<ConfigurableJoint>().axis = Vector3.up;

                    PlaySound(_vmService.PickLockUnlockSoundName, _vmService.DoorUnlockSoundName);

                    return DoorLockStatus.Unlocked;
                }

                GlobalEventDispatcher.LockPickComboCorrect.Invoke(_lockPick, _lockable, (int)_handSide);

                PlaySound(_vmService.PickLockSuccessSoundName);
                return DoorLockStatus.StepSuccess;
            }
            else
            {
                // FIXME - Pseudo breaking for testings only. Use real skill value from hero.
                if (Random.value > 0.5f)
                {
                    GlobalEventDispatcher.LockPickComboWrong.Invoke(_lockPick, _lockable, (int)_handSide);
                    PlaySound(_vmService.PickLockFailureSoundName);
                }
                else
                {
                    // FIXME - decrease lock pick count in ivnentory.
                    GlobalEventDispatcher.LockPickComboBroken.Invoke(_lockPick, _lockable, (int)_handSide);
                    PlaySound(_vmService.PickLockBrokenSoundName);
                }

                _combinationPos = 0;
                return DoorLockStatus.StepFailure;
            }
        }

        private void PlaySound(string soundName, string fallback = null)
        {
            var clip = _audioService.GetRandomSoundClip(soundName);
            
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

