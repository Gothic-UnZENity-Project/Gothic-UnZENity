using System;
using System.Collections;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit.Vobs;
using Random = UnityEngine.Random;

namespace GUZ.Core.Vob
{
    public class SoundDaytimeHandler : MonoBehaviour
    {
        [FormerlySerializedAs("audioSource1")] public AudioSource AudioSource1;
        [FormerlySerializedAs("audioSource2")] public AudioSource AudioSource2;
        [FormerlySerializedAs("properties")] public VobSoundDaytimeProperties Properties;

        private DateTime _startSound1 = GameTime.MinTime;
        private DateTime _endSound1 = GameTime.MaxTime;

        // We need to avoid to start the Coroutine twice.
        private bool _isCoroutineRunning;
        private AudioSource _activeAudio;

        private void OnEnable()
        {
            HourEventCallback(GameGlobals.Time.GetCurrentDateTime());

            StartCoroutineInternal();
            GlobalEventDispatcher.GameTimeHourChangeCallback.AddListener(HourEventCallback);
        }

        private void OnDisable()
        {
            // Coroutines are stopped when GameObject gets disabled. But we need to restart during OnEnable() manually.
            _isCoroutineRunning = false;
            GlobalEventDispatcher.GameTimeHourChangeCallback.RemoveListener(HourEventCallback);
        }

        public void PrepareSoundHandling()
        {
            var startTime = Properties.SoundDaytimeData.StartTime;
            var endTime = Properties.SoundDaytimeData.EndTime;
            if (startTime != (int)startTime || endTime != (int)endTime)
            {
                Debug.LogError(
                    $"Currently fractional times for DayTimeAudio aren't supported. Only full hours are handled. start={_startSound1} end={_endSound1}");
                return;
            }

            _startSound1 = new DateTime(1, 1, 1, (int)startTime, 0, 0);
            _endSound1 = new DateTime(1, 1, 1, (int)endTime, 0, 0);

            // Reset sounds
            AudioSource1.enabled = false;
            AudioSource2.enabled = false;
            AudioSource1.Stop();
            AudioSource2.Stop();

            // Set active sound initially
            HourEventCallback(GameGlobals.Time.GetCurrentDateTime());

            if (gameObject.activeSelf)
            {
                StartCoroutineInternal();
            }
        }

        private void StartCoroutineInternal()
        {
            // Either it's not yet initialized (no clip) or it's no random loop
            if (AudioSource1.clip == null || Properties.SoundDaytimeData.Mode != SoundMode.Random)
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

        private void HourEventCallback(DateTime currentTime)
        {
            if (currentTime >= _startSound1 && currentTime < _endSound1)
            {
                SwitchToSound1();
            }
            else
            {
                SwitchToSound2();
            }
        }

        private void SwitchToSound1()
        {
            // No need to change anything.
            if (AudioSource1.isActiveAndEnabled)
            {
                return;
            }

            // disable
            AudioSource2.enabled = false;
            AudioSource2.Stop();

            // enable
            AudioSource1.enabled = true;
            _activeAudio = AudioSource1;
        }

        private void SwitchToSound2()
        {
            // No need to change anything.
            if (AudioSource2.isActiveAndEnabled)
            {
                return;
            }

            // disable
            AudioSource1.enabled = false;
            AudioSource1.Stop();

            // enable
            AudioSource2.enabled = true;
            _activeAudio = AudioSource2;
        }

        private IEnumerator ReplayRandomSound()
        {
            while (true)
            {
                var nextRandomPlayTime = Properties.SoundDaytimeData.RandomDelay
                                         + Random.Range(0.0f, Properties.SoundDaytimeData.RandomDelayVar);
                yield return new WaitForSeconds(nextRandomPlayTime);

                _activeAudio.Play();
                yield return new WaitForSeconds(_activeAudio.clip.length);
            }
        }
    }
}
