using System;
using System.Collections.Generic;
using System.Linq;
using DirectMusic;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Services.Config;
using GUZ.Core.UnZENity_Core.Scripts.Domain;
using GUZ.Core.Util;
using GUZ.Core.Vob;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager
{
    public class MusicService
    {
        [Inject] private readonly ConfigService _configService;

        private readonly MusicDomain _musicDomain = new();


        public void SetBackgroundMusic(AudioSource audioSource, AudioReverbFilter reverbFilter)
        {
            _musicDomain.SetBackgroundMusic(audioSource, reverbFilter);
        }

        public void Init()
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
    }
}
