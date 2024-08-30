using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit.Vobs;
using Random = UnityEngine.Random;

namespace GUZ.Core.Vob
{
    public class SoundHandler : MonoBehaviour
    {
        [FormerlySerializedAs("audioSource")] public AudioSource AudioSource;
        [FormerlySerializedAs("properties")] public VobSoundProperties Properties;

        // We need to avoid to start the Coroutine twice.
        private bool _isCoroutineRunning;


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
            if (Properties.SoundData == null)
            {
                Debug.LogError("VobSoundProperties.soundData not set. Can't register random sound play!");
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
            if (AudioSource.clip == null || Properties.SoundData.Mode != SoundMode.Random)
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
                var nextRandomPlayTime = Properties.SoundData.RandomDelay
                                         + Random.Range(0.0f, Properties.SoundData.RandomDelayVar);
                yield return new WaitForSeconds(nextRandomPlayTime);

                AudioSource.Play();
                yield return new WaitForSeconds(AudioSource.clip.length);
            }
        }
    }
}
