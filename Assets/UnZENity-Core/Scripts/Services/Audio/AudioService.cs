using GUZ.Core.Domain.Audio;
using GUZ.Core.Extensions;
using GUZ.Core.Services.Caches;
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
        [Inject] private readonly VmCacheService _vmCacheService;

        public const string NoSoundName = "nosound.wav";

        private readonly MusicDomain _musicDomain = new MusicDomain().Inject();
        private readonly SoundDomain _soundDomain = new SoundDomain().Inject();

        public SoundEffectInstance InvOpen => _vmCacheService.TryGetSfxData("INV_OPEN").GetFirstSound();
        public SoundEffectInstance InvClose => _vmCacheService.TryGetSfxData("INV_CLOSE").GetFirstSound();

        
        //
        // Music
        //

        public void SetBackgroundMusic(AudioSource audioSource, AudioReverbFilter reverbFilter)
        {
            _musicDomain.SetBackgroundMusic(audioSource, reverbFilter);
        }

        public void InitMusic()
        {
            _musicDomain.Init();

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
        
        /// <summary>
        /// Hint: If you want to fetch sounds randomly, do not cache them on e.g., MonoBehavior, but fetch them each time you want to run it.
        ///       The AudioClips itself are cached by this method automatically. No performance penalty when re-running this method.
        /// </summary>
        public AudioClip GetRandomSoundClip(string soundName)
        {
            AudioClip clip;

            if (soundName.EqualsIgnoreCase(NoSoundName))
            {
                //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                return null;
            }

            // Bugfix - Normally the data is to get C_SFX_DEF entries from VM. But sometimes there might be the real .wav file stored.
            if (soundName.EndsWithIgnoreCase(".wav"))
            {
                clip = CreateAudioClip(soundName);
            }
            else
            {
                var sfxContainer = _vmCacheService.TryGetSfxData(soundName);

                if (sfxContainer == null)
                    return null;

                // Instead of decoding nosound.wav which might be decoded incorrectly, just return null.
                if (sfxContainer.GetFirstSound().File.EqualsIgnoreCase(NoSoundName))
                    return null;

                clip = CreateAudioClip(sfxContainer.GetRandomSound());
            }

            return clip;
        }
    }
}
