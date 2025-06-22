using System.Collections;
using GUZ.Core.Data.Container;
using GUZ.Core.Util;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;
using Random = UnityEngine.Random;

namespace GUZ.Core.Vob
{
    public class SoundHandler : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        
        private VobContainer _vobContainer;

        // We need to avoid starting the Coroutine twice.
        private bool _isCoroutineRunning;


        private void Start()
        {
            _vobContainer = GetComponentInParent<VobLoader>().Container;
            
            PrepareSoundHandling();
        }
        
        private void OnEnable()
        {
            StartCoroutine();
        }

        private void OnDisable()
        {
            // Coroutines are stopped when GameObject gets disabled. But we need to restart during OnEnable() manually.
            _isCoroutineRunning = false;
        }

        /// <summary>
        /// This will be called during VobCreation time. OnEnable() is too early on to check, if we really need the Coroutine
        /// as properties.soundData will be set at a later state (it's expected to be before calling this method tbh).
        /// Now we can check starting the Coroutine.
        /// </summary>
        public void PrepareSoundHandling()
        {
            if (_vobContainer?.Vob == null)
            {
                Logger.LogError("VobSoundProperties.soundData not set. Can't register random sound play!", LogCat.Audio);
                return;
            }

            if (gameObject.activeSelf)
            {
                StartCoroutine();
            }
        }

        private void StartCoroutine()
        {
            // Either it's not yet initialized (no clip) or it's no random loop
            if (_audioSource.clip == null || _vobContainer.VobAs<ISound>().Mode != SoundMode.Random)
            {
                return;
            }

            if (_isCoroutineRunning)
            {
                return;
            }

            StartCoroutine(ReplayRandomSound());
            _isCoroutineRunning = true;
        }

        private IEnumerator ReplayRandomSound()
        {
            while (true)
            {
                var nextRandomPlayTime = _vobContainer.VobAs<ISound>().RandomDelay
                                         + Random.Range(0.0f, _vobContainer.VobAs<ISound>().RandomDelayVar);
                yield return new WaitForSeconds(nextRandomPlayTime);

                _audioSource.Play();
                yield return new WaitForSeconds(_audioSource.clip.length);
            }
        }
    }
}
