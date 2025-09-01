using GUZ.Core.Domain.Audio;
using GUZ.Core.Extensions;
using GUZ.Core.Services.Config;
using JetBrains.Annotations;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Manager
{
    public class AudioService
    {
        [Inject] private readonly ConfigService _configService;

        private readonly MusicDomain _musicDomain = new();
        private readonly SoundDomain _soundDomain = new();


        //
        // Music
        //

        public void SetBackgroundMusic(AudioSource audioSource, AudioReverbFilter reverbFilter)
        {
            _musicDomain.SetBackgroundMusic(audioSource, reverbFilter);
        }

        public void InitMusic()
        {
            _musicDomain
                .Inject() // As we have e.g., ConfigService used inside.
                .Init();

            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(OnMainMenuLoaded);
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(OnLoadingSceneLoaded);
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(_musicDomain.OnWorldLoaded);

            GlobalEventDispatcher.MusicZoneEntered.AddListener(_musicDomain.MusicZoneEntered);
            GlobalEventDispatcher.MusicZoneExited.AddListener(_musicDomain.MusicZoneExited);
            
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener((key, _) =>
            {
                if (key == "musicVolume" || key == "musicEnabled")
                {
                    _musicDomain.UpdateMusicValuesFromIni();
                }
            });
        }

        public void Play(string musicInstanceName)
        {
            _musicDomain.Play(musicInstanceName);
        }

        private void OnMainMenuLoaded()
        {
            Play("SYS_MENU");
        }

        private void OnLoadingSceneLoaded()
        {
            Play("SYS_LOADING");
        }


        //
        // Sound
        //

        [CanBeNull]
        public AudioClip CreateAudioClip(SoundEffectInstance soundEffectInstance)
        {
            return CreateAudioClip(soundEffectInstance.File);
        }

        /// <summary>
        /// Create AudioClip from a file inside .vdf containers.
        /// Usage: ToAudioClip("fileName"):
        /// </summary>
        [CanBeNull]
        public AudioClip CreateAudioClip(string fileName)
        {
            return _soundDomain.CreateAudioClip(fileName);
        }
    }
}
