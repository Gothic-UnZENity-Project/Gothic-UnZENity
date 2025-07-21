#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Manager;
using GUZ.Core.UnZENity_Core.Scripts.Manager;
using GUZ.Core.Util;
using GUZ.VR.Adapter;
using GUZ.VR.Components.HVROverrides;
using GUZ.VR.Components.UI;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.SpeechToText
{
    public class SpeechToTextHandler : MonoBehaviour
    {
        [SerializeField]
        private VRDialog _vrDialog;
        [SerializeField]
        private GameObject _recordingImage;
        [SerializeField]
        private GameObject _aiWaitingImage;
        
        private const int _maxRecordingLength = 30;
        private const int _recordingSampleRate = 16000;

        private VRPlayerInputs _playerInputs;
        
        // Unity's Microphone API will always store output in an AudioClip.
        private AudioClip _recordedClip;

        private VoiceManager.WhisperManager _whisper => GameGlobals.Voice.Whisper;

        private int _microphoneIndex => GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.Microphone);
        
        private State _state;

        private enum State
        {
            Uninitialized,
            Idle,
            Recording,
            AiWaiting
        }

        private void Start()
        {
            _recordingImage.SetActive(false);
            _aiWaitingImage.SetActive(false);
            
            if (!GameGlobals.Voice.IsEnabled)
            {
                Logger.Log("Disabling SpeechToText feature as Manager is Disabled (e.g. because of Microphone or Whisper).", LogCat.Audio);
                _state = State.Uninitialized;
                gameObject.SetActive(false);
                return;
            }
            
            _playerInputs = ((VRInteractionAdapter)GameContext.InteractionAdapter).GetVRPlayerInputs();
            _state = State.Idle;
        }

        private void Update()
        {
            if (_state == State.Uninitialized)
                return;
            
            CheckRecordingState();
            CheckAiWaitingState();
        }


        private void CheckRecordingState()
        {
            if (_playerInputs.IsSpeakingActivated && _state == State.Idle)
                StartRecording();
            else if (!_playerInputs.IsSpeakingActivated && _state == State.Recording)
                StopAndProcessRecording();
        }

        private void CheckAiWaitingState()
        {
            if (_state != State.AiWaiting)
                return;
            
            if (!_whisper.IsTranscribing)
            {
                _state = State.Idle;
                _recordingImage.SetActive(false);
                _aiWaitingImage.SetActive(false);

                var spokenText = _whisper.OutputString;
                var dialogOptions = _vrDialog.CurrentDialogOptionTexts;
                var result = new TextMatcher(dialogOptions.ToArray()).FindBestMatch(spokenText);

                if (result == null || result.Score < 0.6f)
                {
                    Logger.Log($"No matching dialog option found for voice recording >{spokenText}< found. " +
                               $"Most probable Selection was >{result?.Sentence}< with score >{result?.Score}<.", LogCat.Audio);
                    return;
                }

                Logger.Log($"Dialog option found. Spoken: >{spokenText}<. " +
                            $"Selection: >{result.Sentence}< with score (>{result.Score}<).", LogCat.Audio);
                
                DialogManager.SkipNextOutput = true;
                _vrDialog.DialogSelected(result.Index);
            }
        }

        private void StartRecording()
        {
            Logger.Log("Starting recording.", LogCat.Audio);
            _recordedClip = null; // Reset
            _recordedClip = Microphone.Start(GetMicrophoneDeviceName(), false, _maxRecordingLength, _recordingSampleRate);
            
            _recordingImage.SetActive(true);
            _aiWaitingImage.SetActive(false);

            _state = State.Recording;
        }

        private void StopAndProcessRecording()
        {
            Logger.Log("Stopping recording and executing local LLM...", LogCat.Audio);

            _recordingImage.SetActive(false);
            _aiWaitingImage.SetActive(true);
            
            var recordingPosition = Microphone.GetPosition(GetMicrophoneDeviceName());
            Microphone.End(GetMicrophoneDeviceName());

            if (recordingPosition == 0)
            {
                Logger.LogWarning("No audio from Microphone stream received. Skipping local LLM execution.", LogCat.Audio);
                _state = State.Idle;
                return;
            }

            _state = State.AiWaiting;
            
#pragma warning disable CS4014 // Do not wait. We want to let Whisper work in the background
            _whisper.StartExec(_recordedClip);
#pragma warning restore CS4014
        }

        private string GetMicrophoneDeviceName()
        {
            if (_microphoneIndex - 1 > Microphone.devices.Length)
                return string.Empty;
            else
                return Microphone.devices[_microphoneIndex - 1];
        }

        private void OnDestroy()
        {
            if (_state == State.Recording)
                Microphone.End(GetMicrophoneDeviceName());
        }
    }
}
#endif
