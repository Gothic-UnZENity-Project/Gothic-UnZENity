using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GUZ.Core.Manager
{
    public class BarrierManager
    {
        private GameObject _barrier;

        private Material _material1;
        private Material _material2;
        private bool _materialsCached;

        private bool _barrierVisible;

        private bool _showThunder;
        private bool[] _activeThunder = { false, false, false, false };
        private float[] _thunderDelay = { 8, 6, 14, 2 }; // https://ataulien.github.io/Inside-Gothic/barrier/#thunder
        private float[] _thunderTimer = { 0, 0, 0, 0 };

        private AudioSource[] _thunderSoundSources = new AudioSource[4];

        private bool _barrierFadeIn;
        private bool _fadeIn = true;
        private bool _fadeOut;
        private float _fadeState;
        private float _fadeTime;
        private float _timeUpdatedFade;

        private float _nextActivation = 8f;

        private const float _barrierMinOpacity = 0f;
        private const float _barrierMaxOpacity = 120f;

        // all these values are representing the time in seconds
        private const float _timeToStayVisible = 25;
        private const float _timeToStayHidden = 1200;
        private const float _timeStepToUpdateFade = 0.001f;

        private readonly bool _featureEnableSounds;
        private readonly bool _featureShowBarrierLogs;

        public BarrierManager(GameConfiguration config)
        {
            _featureEnableSounds = config.EnableGameSounds;
            _featureShowBarrierLogs = config.EnableBarrierLogs;
        }

        public void CreateBarrier()
        {
            var barrierMesh = ResourceLoader.TryGetMesh("MAGICFRONTIER_OUT.MSH");
            _barrier = MeshFactory.CreateBarrier("Barrier", barrierMesh)
                .GetAllDirectChildren()[0];

            if (!_featureEnableSounds)
            {
                return;
            }

            for (var i = 0; i < _thunderSoundSources.Length; i++)
            {
                _thunderSoundSources[i] = _barrier.AddComponent<AudioSource>();
                // AddThunder(i);
            }
        }

        public void FixedUpdate()
        {
            RenderBarrier();
        }

        /// <summary>
        /// Controls when and how much the barrier is visible
        /// </summary>
        private void RenderBarrier()
        {
            if (_barrier == null)
            {
                return;
            }

            CacheMaterials();

            _nextActivation -= Time.deltaTime;

            if (_nextActivation <= 0)
            {
                _barrierVisible = !_barrierVisible;
                _nextActivation = _timeToStayHidden + Random.Range(-(5f * 60f), 5f * 60f);
                _barrierFadeIn = true;
            }

            if (!_barrierFadeIn)
            {
                if (_featureShowBarrierLogs)
                {
                    Debug.Log("Next Activation: " + _nextActivation);
                }

                return;
            }

            UpdateFadeState();

            if (_showThunder && _featureEnableSounds)
            {
                var sound = VobHelper.GetSoundClip("MFX_BARRIERE_AMBIENT");

                for (var i = 0; i < 4; i++)
                {
                    if (!_activeThunder[i] && !_thunderSoundSources[i].isPlaying &&
                        Time.time - _thunderTimer[i] > _thunderDelay[i])
                    {
                        _thunderTimer[i] = Time.time;
                        _thunderSoundSources[i].PlayOneShot(sound);
                    }
                }
            }
        }

        private void UpdateFadeState()
        {
            if (_fadeIn)
            {
                ApplyFadeToMaterials();

                if (Time.time - _timeUpdatedFade > _timeStepToUpdateFade)
                {
                    _fadeState++;
                    _timeUpdatedFade = Time.time;
                }

                if (_fadeState >= _barrierMaxOpacity)
                {
                    _fadeState = _barrierMaxOpacity;
                    _fadeIn = false;
                    _fadeTime = Time.time;
                    _showThunder = true;
                }
            }
            else
            {
                // Check if it's time to fade out
                if (Time.time - _fadeTime > _timeToStayVisible)
                {
                    _fadeTime = Time.time;
                    _fadeOut = true;
                    _showThunder = false;
                }

                if (!_fadeOut)
                {
                    return;
                }

                ApplyFadeToMaterials();

                if (Time.time - _timeUpdatedFade > _timeStepToUpdateFade)
                {
                    _fadeState--;
                    _timeUpdatedFade = Time.time;
                }

                if (_fadeState <= _barrierMinOpacity)
                {
                    _fadeState = _barrierMinOpacity;
                    _fadeIn = true;
                    _fadeOut = false;
                    _barrierFadeIn = false;
                }
            }
        }

        private void ApplyFadeToMaterials()
        {
            var blendValue = _fadeState / 255f;
            _material1.SetFloat("_Blend", blendValue);
            _material2.SetFloat("_Blend", blendValue);
        }

        private void CacheMaterials()
        {
            if (_barrier != null && !_materialsCached)
            {
                var materials = _barrier.GetComponent<Renderer>().materials;
                _material1 = materials[0];
                _material2 = materials[1];
                _materialsCached = true;
            }
        }

        private void AddThunder(int i)
        {
            var thunderStrip = new GameObject("ThunderStrip");
            thunderStrip.transform.SetParent(_barrier.transform);
            thunderStrip.transform.localPosition = new Vector3(-50, 400, -56);
            thunderStrip.transform.localRotation = Quaternion.identity * Quaternion.Euler(0, i * 90, -90);
            MeshFactory.CreatePolyStrip(thunderStrip, 11, Vector3.zero, new Vector3(0, 320, 100));
        }
    }
}
