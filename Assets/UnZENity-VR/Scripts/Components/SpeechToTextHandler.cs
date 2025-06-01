using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.VR.Components
{
    public class SpeechToTextHandler : MonoBehaviour
    {
        // Reference to a UI button
        public Button RecordButton;
        
        // Reference to an AudioSource component to play the recording
        public AudioSource AudioSource;
        
        // Name of the microphone device to use (empty for default)
        public string MicrophoneDeviceName = "Mikrofonarray (Senary Audio)";
        
        // Maximum recording length in seconds
        public int MaxRecordingLength = 30;
        
        // Recording sample rate
        public int recordingSampleRate = 16000;
        
        // To keep track of recording state
        private bool _isRecording;
        
        // Store the recorded clip
        private AudioClip _recordedClip;
        
        private void Start()
        {
            // Check if we have any microphones connected
            if (Microphone.devices.Length <= 0)
            {
                Debug.LogError("No microphone detected!");
                RecordButton.interactable = false;
                return;
            }
            
            // If no specific device name was set, use the first available
            if (string.IsNullOrEmpty(MicrophoneDeviceName) && Microphone.devices.Length > 0)
            {
                MicrophoneDeviceName = Microphone.devices[0];
                Debug.Log("Using microphone: " + MicrophoneDeviceName);
            }
            
            // Make sure we have an AudioSource component
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
                if (AudioSource == null)
                {
                    AudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Set up button click handler
            RecordButton.onClick.AddListener(OnRecordButtonClicked);
        }
        
        public void OnRecordButtonClicked()
        {
            if (!_isRecording)
                StartRecording();
            else
                StopRecordingAndPlay();
        }
        
        private void StartRecording()
        {
            Debug.Log("Starting recording...");
            
            // Start recording using the microphone
            _recordedClip = Microphone.Start(MicrophoneDeviceName, false, MaxRecordingLength, recordingSampleRate);
            
            _isRecording = true;
            
            // Update button text or appearance to indicate recording state
            if (RecordButton.GetComponentInChildren<Text>() != null)
            {
                RecordButton.GetComponentInChildren<Text>().text = "Stop Recording";
            }
        }
        
        private void StopRecordingAndPlay()
        {
            Debug.Log("Stopping recording and playing...");
            
            // Get the position where we stopped recording
            int recordingPosition = Microphone.GetPosition(MicrophoneDeviceName);
            
            // Stop the recording
            Microphone.End(MicrophoneDeviceName);
            
            if (recordingPosition > 0)
            {
                // Create a new AudioClip that has the correct length
                AudioClip tempClip = AudioClip.Create("TempRecording", recordingPosition, _recordedClip.channels, 
                                                     recordingSampleRate, false);
                
                // Copy the recorded samples to the new clip
                float[] samples = new float[recordingPosition * _recordedClip.channels];
                _recordedClip.GetData(samples, 0);
                tempClip.SetData(samples, 0);
                
                // Replace the original clip with the properly sized one
                _recordedClip = tempClip;
                
                // Assign the clip to the AudioSource and play it
                AudioSource.clip = _recordedClip;
                AudioSource.Play();
            }
            
            _isRecording = false;
            
            // Update button text or appearance
            if (RecordButton.GetComponentInChildren<Text>() != null)
            {
                RecordButton.GetComponentInChildren<Text>().text = "Record";
            }
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (_isRecording)
                Microphone.End(MicrophoneDeviceName);
        }
    }
}
