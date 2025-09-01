using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DirectMusic;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data;
using GUZ.Core.Domain.Audio;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Models.Audio;
using GUZ.Core.Services.Config;
using GUZ.Core.UnZENity_Core.Scripts.Domain;
using GUZ.Core.Util;
using GUZ.Core.Vob;
using JetBrains.Annotations;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

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
