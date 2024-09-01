using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GUZ.Core.Manager
{
    public class SkyManager
    {
        private Vector3 _sunDirection;
        private readonly Color _sunColor;
        private readonly Color _ambientColor;
        private readonly float _pointLightIntensity = 1f;
        private bool _isRaining;
        private readonly GameTimeInterval _sunPerformanceSetting;
        private readonly GameSettings _gameSettings;
        private readonly bool _gameSounds;

        private float _masterTime;
        private bool _noSky = true;
        private List<SkyState> _stateList = new();
        private GameTime _gameTime;

        private static readonly int _sunDirectionShaderId = Shader.PropertyToID("_SunDirection");
        private static readonly int _sunColorShaderId = Shader.PropertyToID("_SunColor");
        private static readonly int _ambientShaderId = Shader.PropertyToID("_AmbientColor");
        private static readonly int _pointLightIntensityShaderId = Shader.PropertyToID("_PointLightIntensity");

        private SkyStateRain _rainState = new();
        private ParticleSystem _rainParticleSystem;
        private AudioSource _rainParticleSound;
        private float _rainWeightAndVolume;

        private const int _maxParticleCount = 700;

        private static readonly int _skyTex1ShaderId = Shader.PropertyToID("_Sky1");
        private static readonly int _skyTex2ShaderId = Shader.PropertyToID("_Sky2");
        private static readonly int _skyTex3ShaderId = Shader.PropertyToID("_Sky3");
        private static readonly int _skyTex4ShaderId = Shader.PropertyToID("_Sky4");
        private static readonly int _skyMovement1ShaderId = Shader.PropertyToID("_Vector1");
        private static readonly int _skyMovement2ShaderId = Shader.PropertyToID("_Vector2");
        private static readonly int _skyMovement3ShaderId = Shader.PropertyToID("_Vector3");
        private static readonly int _skyMovement4ShaderId = Shader.PropertyToID("_Vector4");
        private static readonly int _sky1OpacityShaderId = Shader.PropertyToID("_Sky1Opacity");
        private static readonly int _sky2OpacityShaderId = Shader.PropertyToID("_Sky2Opacity");
        private static readonly int _sky3OpacityShaderId = Shader.PropertyToID("_Sky3Opacity");
        private static readonly int _sky4OpacityShaderId = Shader.PropertyToID("_Sky4Opacity");
        private static readonly int _layersBlendShaderId = Shader.PropertyToID("_LayerBlend");
        private static readonly int _fogColor1ShaderId = Shader.PropertyToID("_FogColor");
        private static readonly int _fogColor2ShaderId = Shader.PropertyToID("_FogColor2");
        private static readonly int _domeColor1ShaderId = Shader.PropertyToID("_DomeColor1");
        private static readonly int _domeColor2ShaderId = Shader.PropertyToID("_DomeColor2");

        public SkyManager(GameConfiguration config, GameTime time, GameSettings settings)
        {
            _gameTime = time;

            _sunColor = config.SunLightColor;
            _ambientColor = config.AmbientLightColor;
            _pointLightIntensity = config.SunLightIntensity;
            _sunPerformanceSetting = config.SunUpdateInterval;
            _gameSettings = settings;
            _gameSounds = config.EnableGameSounds;
        }

        public void OnValidate()
        {
            SetShaderProperties();
        }

        public void Init()
        {
            GlobalEventDispatcher.GameTimeSecondChangeCallback.AddListener(Interpolate);
            GlobalEventDispatcher.GameTimeHourChangeCallback.AddListener(UpdateRainTime);
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(GeneralSceneLoaded);
        }

        public void InitSky()
        {
            RotateSun(_gameTime.GetCurrentDateTime());
            switch (_sunPerformanceSetting)
            {
                case GameTimeInterval.EveryGameSecond:
                    GlobalEventDispatcher.GameTimeSecondChangeCallback.AddListener(RotateSun);
                    break;
                case GameTimeInterval.EveryGameMinute:
                    GlobalEventDispatcher.GameTimeMinuteChangeCallback.AddListener(RotateSun);
                    break;
                case GameTimeInterval.EveryGameHour:
                    GlobalEventDispatcher.GameTimeHourChangeCallback.AddListener(RotateSun);
                    break;
            }

            _stateList.AddRange(new[]
            {
                CreatePresetState(new SkyState(), state => state.PresetDay1()),
                CreatePresetState(new SkyState(), state => state.PresetDay2()),
                CreatePresetState(new SkyState(), state => state.PresetEvening()),
                CreatePresetState(new SkyState(), state => state.PresetNight0()),
                CreatePresetState(new SkyState(), state => state.PresetNight1()),
                CreatePresetState(new SkyState(), state => state.PresetNight2()),
                CreatePresetState(new SkyState(), state => state.PresetDawn()),
                CreatePresetState(new SkyState(), state => state.PresetDay0())
            });


            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.ambientMode = AmbientMode.Flat;
            InitRainState();

            Interpolate(new DateTime());
        }

        private void UpdateStateTexAndFog()
        {
            if (_gameSettings.GothicIniSettings.ContainsKey("SKY_OUTDOOR"))
            {
                var currentDay = _gameTime.GetDay();
                var day = currentDay + 1;

                float[] colorValues;

                try
                {
                    // hacky way to use the proper color for the current day until animTex is implemented
                    // % 2 is used as there are only 2 textures for the sky, consistent between G1 and G2 
                    colorValues = _gameSettings.GothicIniSettings["SKY_OUTDOOR"]["zDayColor" + day % 2]
                        .Split(' ').Select(float.Parse).ToArray();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    return;
                }

                foreach (var state in _stateList)
                {
                    // all states that contain day sky layer dawn, evening and day 0 to 3
                    if (state.Time < 0.35 || state.Time > 0.65)
                    {
                        state.Layer[0].TEXName = "SKYDAY_LAYER0_A" + day % 2 + ".TGA";
                        // day states that contain sky cloud layer day 0 to 3
                        if (state.Time < 0.3 || state.Time > 0.7)
                        {
                            state.Layer[1].TEXName = "SKYDAY_LAYER1_A" + day % 2 + ".TGA";
                            state.FogColor = new Vector3(colorValues[0], colorValues[1], colorValues[2]);
                            state.DomeColor0 = new Vector3(colorValues[0], colorValues[1], colorValues[2]);
                        }
                    }
                }
            }
        }

        private void Interpolate(DateTime _)
        {
            _masterTime = _gameTime.GetSkyTime(); // Current time

            var (previousIndex, currentIndex) = FindNextStateIndex();

            var lastState = _stateList[previousIndex];
            var currentState = _stateList[currentIndex];
            _isRaining = _masterTime > _rainState.Time && _masterTime < _rainState.EndTime;

            UpdateStateTexAndFog();

            if (_isRaining)
            {
                UpdateRain(_rainState.Time);
                InterpolateSky(currentState, _rainState, _rainState.Time, _rainState.LerpDuration);
                return;
            }

            if (_masterTime > _rainState.Time &&
                _masterTime < _rainState.EndTime + _rainState.LerpDuration) // when rain is ending
            {
                UpdateRain(_rainState.EndTime);
                InterpolateSky(_rainState, currentState, _rainState.EndTime, _rainState.LerpDuration);
                return;
            }

            UpdateRain(_rainState.Time);
            InterpolateSky(lastState, currentState, currentState.Time, currentState.LerpDuration);
        }

        private void InterpolateSky(SkyState lastState, SkyState newState, float startTime, float lerpDuration = 0.05f)
        {
            // Calculate how far we are between the two ticks (0.0 to 1.0)
            var lerpFraction = Mathf.Clamp01((_masterTime - startTime) / lerpDuration);

            if (lerpFraction >= 1 && _noSky == false)
            {
                return; // finished blending
            }

            var oldPolyColor = lastState.PolyColor.ToUnityColor(255) / 255f;
            var newPolyColor = newState.PolyColor.ToUnityColor(255) / 255f;

            var oldDomeColor = lastState.DomeColor0.ToUnityColor(255) / 255f;
            var newDomeColor = newState.DomeColor0.ToUnityColor(255) / 255f;

            var oldFogColor = lastState.FogColor.ToUnityColor(255) / 255f;
            var newFogColor = newState.FogColor.ToUnityColor(255) / 255f;

            RenderSettings.ambientLight = Color.Lerp(oldPolyColor, newPolyColor, lerpFraction);
            RenderSettings.fogColor = Color.Lerp(oldFogColor, newFogColor, lerpFraction);

            // Old sky layer 1.
            if (!string.IsNullOrEmpty(lastState.Layer[0].TEXName))
            {
                RenderSettings.skybox.SetTexture(_skyTex1ShaderId,
                    TextureCache.TryGetTexture(lastState.Layer[0].TEXName));
            }

            RenderSettings.skybox.SetVector(_skyMovement1ShaderId, lastState.Layer[0].TEXSpeed);
            RenderSettings.skybox.SetFloat(_sky1OpacityShaderId, lastState.Layer[0].TEXAlpha / 255f);

            // Old sky layer 2.
            if (!string.IsNullOrEmpty(lastState.Layer[1].TEXName))
            {
                RenderSettings.skybox.SetTexture(_skyTex2ShaderId,
                    TextureCache.TryGetTexture(lastState.Layer[1].TEXName));
            }

            RenderSettings.skybox.SetVector(_skyMovement2ShaderId, lastState.Layer[1].TEXSpeed);
            RenderSettings.skybox.SetFloat(_sky2OpacityShaderId, lastState.Layer[1].TEXAlpha / 255f);

            // New sky layer 1.
            if (!string.IsNullOrEmpty(newState.Layer[0].TEXName))
            {
                RenderSettings.skybox.SetTexture(_skyTex3ShaderId,
                    TextureCache.TryGetTexture(newState.Layer[0].TEXName));
            }

            RenderSettings.skybox.SetVector(_skyMovement3ShaderId, newState.Layer[0].TEXSpeed);
            RenderSettings.skybox.SetFloat(_sky3OpacityShaderId, newState.Layer[0].TEXAlpha / 255f);

            // New sky layer 2.
            if (!string.IsNullOrEmpty(newState.Layer[1].TEXName))
            {
                RenderSettings.skybox.SetTexture(_skyTex4ShaderId,
                    TextureCache.TryGetTexture(newState.Layer[1].TEXName));
            }

            RenderSettings.skybox.SetVector(_skyMovement4ShaderId, newState.Layer[1].TEXSpeed);
            RenderSettings.skybox.SetFloat(_sky4OpacityShaderId, newState.Layer[1].TEXAlpha / 255f);

            // Fog and dome color.
            RenderSettings.skybox.SetColor(_fogColor1ShaderId, oldFogColor);
            RenderSettings.skybox.SetColor(_fogColor2ShaderId, newFogColor);
            RenderSettings.skybox.SetColor(_domeColor1ShaderId, oldDomeColor);
            RenderSettings.skybox.SetColor(_domeColor2ShaderId, newDomeColor);

            RenderSettings.skybox.SetFloat(_layersBlendShaderId, lerpFraction);
            SetShaderProperties();
        }

        private void InitRainState()
        {
            // values taken from the original game
            _rainState.Time = 0.187f; // 16:30
            _rainState.EndTime = 0.229f; // 17:30
            _rainState.LerpDuration = 0.01f;
            _rainState.PolyColor = new Vector3(255.0f, 250.0f, 235.0f);
            _rainState.FogColor = new Vector3(72.0f, 72.0f, 72.0f);
            _rainState.DomeColor0 = new Vector3(72.0f, 72.0f, 72.0f);
            _rainState.Layer[0].TEXName = "SKYRAINCLOUDS.TGA";
            _rainState.Layer[0].TEXAlpha = 255.0f;
        }

        private void SetShaderProperties()
        {
            Shader.SetGlobalVector(_sunDirectionShaderId, _sunDirection);
            Shader.SetGlobalColor(_sunColorShaderId, _sunColor);
            Shader.SetGlobalColor(_ambientShaderId, _ambientColor);
            Shader.SetGlobalFloat(_pointLightIntensityShaderId, _pointLightIntensity);
        }

        private void GeneralSceneLoaded(GameObject playerGo)
        {
            RenderSettings.skybox = Object.Instantiate(GameGlobals.Textures.SkyMaterial);

            InitRainGo();
        }

        private void InitRainGo()
        {
            // by default rainPFX is disabled so we need to find the parent and activate it
            var rainParticlesGameObject = GameObject.Find("Rain").FindChildRecursively("RainParticles");
            rainParticlesGameObject.SetActive(true);
            _rainParticleSystem = rainParticlesGameObject.GetComponent<ParticleSystem>();
            _rainParticleSystem.Stop();

            _rainParticleSound = rainParticlesGameObject.GetComponentInChildren<AudioSource>();
            _rainParticleSound.clip = SoundCreator.ToAudioClip(ResourceLoader.TryGetSound("RAIN_01.WAV"));
            _rainParticleSound.volume = 0;
            _rainParticleSound.Stop();
        }

        private void UpdateRainTime(DateTime _)
        {
            if (_masterTime > 0.02f || // This function is called every hour but is run only once a day at 12:00 pm
                _gameTime.GetDay() == 1) // Dont update if it is the first day 
            {
                return;
            }

            _rainState.Time = Random.Range(0f, 1f);

            if (0.96f < _rainState.Time)
            {
                _rainState.Time = 0.96f;
            }

            _rainState.EndTime = Random.Range(0f, 0.06f) + 0.04f + _rainState.Time;

            if (1.0f < _rainState.EndTime)
            {
                _rainState.EndTime = 1.0f;
            }
        }

        private void UpdateRain(float stateTime, float lerpDuration = 0.01f)
        {
            if (_rainParticleSound == null || _rainParticleSystem == null)
            {
                return;
            }

            if (_masterTime < _rainState.Time ||
                _masterTime > _rainState.EndTime + _rainState.LerpDuration) // is not raining nor after rain
            {
                _rainParticleSound.volume = 0;
                _rainParticleSound.Stop();
                _rainParticleSystem.Stop();
                return;
            }

            var lerpFraction = (_masterTime - stateTime) / lerpDuration;

            lerpFraction = Mathf.Clamp01(lerpFraction);

            if (_isRaining)
            {
                _rainWeightAndVolume = lerpFraction;
            }
            else if (_masterTime > _rainState.Time && _masterTime < _rainState.EndTime + _rainState.LerpDuration)
            {
                _rainWeightAndVolume = 1 - lerpFraction;
            }

            _rainParticleSound.volume = _rainWeightAndVolume;

            var module = _rainParticleSystem.emission;
            module.rateOverTime = new ParticleSystem.MinMaxCurve(_maxParticleCount * _rainWeightAndVolume);

            if (!_rainParticleSound.isPlaying && _gameSounds)
            {
                _rainParticleSound.Play();
            }

            if (!_rainParticleSystem.isPlaying)
            {
                _rainParticleSystem.Play();
            }
        }

        /// <summary>
        /// Find the previous and next state indices based on the current master time.
        /// </summary>
        private (int previousIndex, int nextIndex) FindNextStateIndex()
        {
            var nextIndex = _stateList.FindLastIndex(x => x.Time < _masterTime);

            if (nextIndex == -1)
            {
                nextIndex = 0;
            }

            var previousIndex = nextIndex - 1;
            if (previousIndex < 0)
            {
                previousIndex = _stateList.Count - 1;
            }

            return (previousIndex, nextIndex);
        }

        private static SkyState CreatePresetState(SkyState skyState, Action<SkyState> applyPreset)
        {
            applyPreset(skyState);
            return skyState;
        }

        /// <summary>
        /// Based on performance settings, the sun direction is changed more or less frequent.
        ///
        /// Unity rotation settings:
        /// 270° = midnight (no light)
        /// 90° = noon (full light)
        /// 
        /// Calculation: 270f is the starting midnight value
        /// Calculation: One full DateTime == 360°. --> e.g. 15° * 24h + 0min + 0sec == 360°
        /// </summary>
        private void RotateSun(DateTime time)
        {
            var xRotation = 270f + 15f * (time.Hour + time.Minute / 60f + time.Second / 3600f);
            _sunDirection = new Vector3(xRotation % 360, 0, 0);
        }
    }
}
