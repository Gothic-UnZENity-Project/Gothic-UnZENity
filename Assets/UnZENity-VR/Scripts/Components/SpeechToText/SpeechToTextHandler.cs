using System.Linq;
using GUZ.Core;
using GUZ.Core.Globals;
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

        private int _microphoneIndex => GameGlobals.Config.Gothic.GetInt(VRConstants.IniNames.Microphone);
        
        private Whisper _whisper = new();
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

            // Check if we have any microphones connected
            if (_microphoneIndex == 0 || Microphone.devices.Length <= 0)
            {
                Logger.Log("No microphone selected and/or detected!", LogCat.VR);
                _state = State.Uninitialized;
                gameObject.SetActive(false);
                return;
            }
            
            _whisper = new();
            _whisper.Initialize();
            
            if (!_whisper.IsInitialized)
            {
                Logger.Log("Disabling SpeechToText feature as Whisper isn't initialized.", LogCat.VR);
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
                    Logger.Log("No matching text found for spoken text or score is too low.", LogCat.VR);
                    return;
                }
                
                _vrDialog.DialogSelected(result.Index);
                
            }
        }

        private void StartRecording()
        {
            Logger.Log("Starting recording.", LogCat.VR);
            _recordedClip = Microphone.Start(GetMicrophoneDeviceName(), false, _maxRecordingLength, _recordingSampleRate);
            
            _recordingImage.SetActive(true);
            _aiWaitingImage.SetActive(false);

            _state = State.Recording;
        }

        private void StopAndProcessRecording()
        {
            Logger.Log("Stopping recording and executing local LLM...", LogCat.VR);

            _recordingImage.SetActive(false);
            _aiWaitingImage.SetActive(true);
            
            var recordingPosition = Microphone.GetPosition(GetMicrophoneDeviceName());
            Microphone.End(GetMicrophoneDeviceName());

            if (recordingPosition == 0)
            {
                Logger.LogWarning("No audio from Microphone stream received. Skipping local LLM execution.", LogCat.VR);
                return;
            }

            _state = State.AiWaiting;
         
            _whisper.StartExec(_recordedClip);
        }

        private string GetMicrophoneDeviceName()
        {
            if (_microphoneIndex < Microphone.devices.Length)
                return string.Empty;
            else
                return Microphone.devices[_microphoneIndex];
        }

        private void OnDestroy()
        {
            if (_state == State.Recording)
                Microphone.End(GetMicrophoneDeviceName());
        }
    }
}
