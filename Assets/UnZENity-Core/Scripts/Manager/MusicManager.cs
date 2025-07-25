using System;
using System.Collections.Generic;
using System.Linq;
using DirectMusic;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.Core.Vob;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;
using Object = UnityEngine.Object;

namespace GUZ.Core.Manager
{
    public class MusicManager
    {
        [Flags]
        public enum SegmentTags : byte
        {
            Day = 0,
            Ngt = 1 << 0,

            Std = 0,
            Fgt = 1 << 1,
            Thr = 1 << 2
        }

        private const string _backgroundMusicGOName = "BackgroundMusic";

        private Performance _dxPerformance;
        private AudioSource _audioSourceComp;
        private AudioReverbFilter _reverbFilterComp;

        private Dictionary<string, MusicThemeInstance> _themes = new();
        private DaedalusVm _vm;

        private MusicThemeInstance _currentTheme;

        /// <summary>
        /// Whenever we collide with a musicZoneVobGO, it's entry will be added to the list and the most important theme will be played.
        /// </summary>
        private readonly List<GameObject> _musicZones = new();

        // Depending on speed of track, 2048 == around less than a second
        // If we cache each call to dxMusic synthesizer, we would skip a lot of transition options as the synthesizer assumes we're already ahead.
        // This is due to the fact, that whenever we ask for data from dxMusic, the handler "moves" forward as it assumes we play to the end until asking for more data.
        // But if we ask for numerous seconds and therefore "cache" music way too long, the transition will take place very late which can be heard by gamers.
        private const int _bufferSize = 2048;
        private const int _frequencyRate = 44100;


        public MusicManager(DeveloperConfig config)
        {
            
        }

        public void Init()
        {
            _dxPerformance = Performance.Create(_frequencyRate);

            InitializeUnity();
            InitializeDxMusic();

            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(OnMainMenuLoaded);
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(OnLoadingSceneLoaded);
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(OnWorldLoaded);

            GlobalEventDispatcher.MusicZoneEntered.AddListener(go =>
            {
                AddMusicZone(go);
                Play(SegmentTags.Std);
            });

            GlobalEventDispatcher.MusicZoneExited.AddListener(go =>
            {
                RemoveMusicZone(go);
                Play(SegmentTags.Std);
            });
            
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener((key, value) =>
            {
                if (key == "musicVolume" || key == "musicEnabled")
                {
                    SetMusicFromIni();
                }
            });
        }

        private void InitializeUnity()
        {
            var backgroundMusic = GameObject.Find(_backgroundMusicGOName);
            _audioSourceComp = backgroundMusic.GetComponent<AudioSource>();
            _reverbFilterComp = backgroundMusic.GetComponent<AudioReverbFilter>();

            SetMusicFromIni();
            
            var audioClip = AudioClip.Create("Music", _bufferSize * 2, 2, _frequencyRate, true, PCMReaderCallback);

            _audioSourceComp.priority = 0;
            _audioSourceComp.clip = audioClip;
            _audioSourceComp.loop = true;
            _audioSourceComp.Play();
        }

        /// <summary>
        /// Set values based on Gothic.ini
        /// </summary>
        private void SetMusicFromIni()
        {
            // If we disable the music component, we can't re-enable music again. It's better to mute it.
            if (!GameGlobals.Config.Gothic.IniMusicEnabled)
            {
                _audioSourceComp.volume = 0f;
            }
            else
            {
                // A music sound of 1f is way too loud. We therefore turn it down based on some experience.
                _audioSourceComp.volume = GameGlobals.Config.Gothic.IniMusicVolume / 3;
            }
        }

        private void OnMainMenuLoaded()
        {
            Play("SYS_MENU");
        }

        private void OnLoadingSceneLoaded()
        {
            Play("SYS_LOADING");
        }

        private void OnWorldLoaded()
        {
            _musicZones.Clear();
            
            var zones = MultiTypeCache.VobCache.Where(i => i.Vob is IZoneMusic); // IZoneMusic | IZoneMusicDefault
            var playerPosition = GameObject.FindWithTag(Constants.PlayerTag).transform.position;

            foreach (var zone in zones)
            {
                // We always set default music as fallback.
                if (zone.Vob.GetType() == typeof(ZoneMusicDefault))
                {
                    AddMusicZone(zone.Go);
                    continue;
                }

                // If it's a normal music, we check if we're standing inside.
                if (zone.Go.GetComponentInChildren<BoxCollider>().bounds.Contains(playerPosition))
                {
                    AddMusicZone(zone.Go);
                }
            }

            Play(SegmentTags.Std);
        }

        public void AddMusicZone(GameObject newMusicZoneGo)
        {
            // If a collider triggers multiple times or we added the zone manually: Skip as duplicate
            if (_musicZones.Contains(newMusicZoneGo))
                return;

            _musicZones.Add(newMusicZoneGo);
        }

        public void RemoveMusicZone(GameObject newMusicZoneGo)
        {
            _musicZones.Remove(newMusicZoneGo);
        }

        private void InitializeDxMusic()
        {
            // Load the VM and initialize all music theme instances
            _vm = ResourceLoader.TryGetDaedalusVm("MUSIC");
            _vm.GetInstanceSymbols("C_MUSICTHEME").ForEach(v =>
            {
                _themes[v.Name] = _vm.InitInstance<MusicThemeInstance>(v);
            });
        }

        private void PCMReaderCallback(float[] data)
        {
            _dxPerformance.RenderPcm(data, true);
        }

        public void Play(SegmentTags tags)
        {
            var zoneName = _musicZones
                .OrderByDescending(i => i.GetComponentInParent<VobLoader>().Container.VobAs<IZoneMusic>().Priority)
                .FirstOrDefault()?
                .GetComponentInParent<VobLoader>().Container.VobAs<IZoneMusic>().Name;

            if (zoneName == null)
                return;
            
            var isDay = (tags & SegmentTags.Ngt) == 0;
            var result = zoneName.Substring(zoneName.IndexOf("_") + 1);
            var musicTag = "STD";

            if ((tags & SegmentTags.Fgt) != 0)
            {
                musicTag = "FGT";
            }

            if ((tags & SegmentTags.Thr) != 0)
            {
                musicTag = "THR";
            }

            var musicThemeInstanceName = $"{result}_{(isDay ? "DAY" : "NGT")}_{musicTag}";

            Play(musicThemeInstanceName);
        }

        public void Play(string musicInstanceName)
        {
            var music = _themes[musicInstanceName];
            Play(music);
        }

        public void Play(MusicThemeInstance theme)
        {
            // Do not restart the current theme if already playing.
            // Multiple MusicThemeInstances can reference the same audio. Therefore, checking actual files only.
            if (_currentTheme != null && theme.File == _currentTheme.File)
            {
                return;
            }

            var segment = ResourceLoader.TryGetSegment(theme.File);

            var timing = ToTiming(theme.TransSubType);
            var embellishment = ToEmbellishment(theme.TransType);

            Logger.Log($"Changing music theme to: {theme.File}", LogCat.Audio);

            _dxPerformance.PlayTransition(segment, embellishment, timing);

            // Tests sounded feasible like when you stop the music you get somme afterglow hall.
            // TODO - But I have no clue if decayTime is the right timer to set here. Alter if you have better ears than I have. ;-)
            _reverbFilterComp.decayTime = theme.ReverbTime / 1000; // ms in seconds

            _currentTheme = theme;
        }

        private static Timing ToTiming(MusicTransitionType type)
        {
            return type switch
            {
                MusicTransitionType.Measure or MusicTransitionType.Unknown => Timing.Measure,
                MusicTransitionType.Immediate => Timing.Instant,
                MusicTransitionType.Beat => Timing.Beat,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private static Embellishment ToEmbellishment(MusicTransitionEffect effect)
        {
            return effect switch
            {
                // None or Unknown needs to be set to End - otherwise normal transitions won't happen in G1 music.
                MusicTransitionEffect.Unknown or MusicTransitionEffect.None => Embellishment.End,
                MusicTransitionEffect.Groove => Embellishment.Groove,
                MusicTransitionEffect.Fill => Embellishment.Fill,
                MusicTransitionEffect.Break => Embellishment.Break,
                MusicTransitionEffect.Intro => Embellishment.Intro,
                MusicTransitionEffect.End => Embellishment.End,
                MusicTransitionEffect.EndAndInto => Embellishment.EndAndIntro,
                _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, null)
            };
        }
    }
}
