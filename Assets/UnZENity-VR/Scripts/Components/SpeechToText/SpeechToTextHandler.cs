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
        
        // Name of the microphone device to use (empty for default)
        public string MicrophoneDeviceName = "Mikrofonarray (Senary Audio)";

        private const int _maxRecordingLength = 30;
        private const int _recordingSampleRate = 16000;

        private VRPlayerInputs _playerInputs;
        
        // Unity's Microphone API will always store output in an AudioClip.
        private AudioClip _recordedClip;

        private Whisper _whisper = new();
        private State _state;

        private enum State
        {
            Idle,
            Recording,
            AiWaiting
        }

        private void Start()
        {
            _whisper = new();
            _whisper.Initialize();
            
            if (!_whisper.IsInitialized)
            {
                Logger.Log("Disabling SpeechToText feature as Whisper isn't initialized.", LogCat.VR);
                return;
            }
            
            _recordingImage.SetActive(false);
            _aiWaitingImage.SetActive(false);
            
            _playerInputs = ((VRInteractionAdapter)GameContext.InteractionAdapter).GetVRPlayerInputs();
            
            // Check if we have any microphones connected
            if (Microphone.devices.Length <= 0)
            {
                Logger.LogError("No microphone detected!", LogCat.VR);
                return;
            }

            // If no specific device name was set, use the first available
            if (string.IsNullOrEmpty(MicrophoneDeviceName) && Microphone.devices.Length > 0)
            {
                MicrophoneDeviceName = Microphone.devices[0];
                Logger.Log($"Using microphone: {MicrophoneDeviceName}", LogCat.VR);
            }
        }

        private void Update()
        {
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
                var dialogOptions = GameData.Dialogs.CurrentDialog.Options.Select(i => i.Text).ToArray();
                var result = new TextMatcher(dialogOptions).FindBestMatch(spokenText);

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
            _recordedClip = Microphone.Start(MicrophoneDeviceName, false, _maxRecordingLength, _recordingSampleRate);
            
            _recordingImage.SetActive(true);
            _aiWaitingImage.SetActive(false);

            _state = State.Recording;
        }

        private void StopAndProcessRecording()
        {
            Logger.Log("Stopping recording and executing local LLM...", LogCat.VR);

            _recordingImage.SetActive(false);
            _aiWaitingImage.SetActive(true);
            
            var recordingPosition = Microphone.GetPosition(MicrophoneDeviceName);
            Microphone.End(MicrophoneDeviceName);

            if (recordingPosition == 0)
            {
                Logger.LogWarning("No audio from Microphone stream received. Skipping local LLM execution.", LogCat.VR);
                return;
            }

            _state = State.AiWaiting;

            _whisper.StartExec(_recordedClip);
        }

        private void OnDestroy()
        {
            if (_state == State.Recording)
                Microphone.End(MicrophoneDeviceName);
        }
    }
}
