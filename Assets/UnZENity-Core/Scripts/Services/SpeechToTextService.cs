using System;
using System.Threading.Tasks;
using GUZ.Core.UnZENity_Core.Scripts.Domain;
using GUZ.Core.Util;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Services
{
    public class SpeechToTextService : IDisposable
    {
        public bool IsEnabled => _whisper.IsInitialized;

        private WhisperDomain _whisper;

        public void Init()
        {
            // Check if we have any microphones connected
            if (Microphone.devices.Length <= 0)
            {
                Logger.Log("No microphone detected! Disable feature.", LogCat.Audio);
                return;
            }
            else
            {
                Logger.Log($"Unity detected the following microphones: {string.Join(";", Microphone.devices)}", LogCat.Audio);
            }
            
#pragma warning disable CS4014 // Whisper might take some seconds to initialize. Do not wait.
            InitializeWhisper();
#pragma warning restore CS4014
        }

#pragma warning disable CS1998 // Whisper might take some seconds to initialize. Do not wait.
        private async Task InitializeWhisper()
        {
            _whisper = new();
            _whisper.Init();
        }
#pragma warning restore CS1998
        

        public void StartExec(AudioClip recordedClip)
        {
            _whisper.StartExec(recordedClip);
        }

        public bool IsTranscribing()
        {
            return _whisper.IsTranscribing;
        }

        public string GetOutputString()
        {
            return _whisper.OutputString;
        }


        public void Dispose()
        {
            _whisper?.Dispose();
            _whisper = null;
        }
    }
}
