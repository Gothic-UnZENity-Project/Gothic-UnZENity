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
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            _audioSource.PlayOneShot(SoundCreator.ToAudioClip(ResourceLoader.TryGetSound(Constants.Daedalus.PicklockFailureSoundName)));
            
            other.gameObject.GetComponentInParent<VRVobLockPickProperties>().IsInsideLock = true;
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.name.Equals(_lockInteractionColliderName))
            {
                return;
            }

            _audioSource.PlayOneShot(SoundCreator.ToAudioClip(ResourceLoader.TryGetSound(Constants.Daedalus.PicklockFailureSoundName)));
            
            other.gameObject.GetComponentInParent<VRVobLockPickProperties>().IsInsideLock = false;
        }
    }
}
