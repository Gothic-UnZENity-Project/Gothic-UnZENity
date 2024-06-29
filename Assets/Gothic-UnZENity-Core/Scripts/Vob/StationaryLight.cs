using System;
using System.Collections.Generic;
using GUZ.Core.Extensions;
using UnityEngine;
using UnityEngine.Profiling;

namespace GUZ.Core
{
    [RequireComponent(typeof(Light))]
    public class StationaryLight : MonoBehaviour
    {
        public Color Color
        {
            get
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                return _unityLight.color;
            }
            set
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                _unityLight.color = value;
            }
        }

        public LightType Type
        {
            get
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                return _unityLight.type;
            }
            set
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                _unityLight.type = value;
            }
        }

        public float Intensity
        {
            get
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                return _unityLight.intensity;
            }
            set
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                _unityLight.intensity = value;
            }
        }

        public float Range
        {
            get
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                return _unityLight.range;
            }
            set
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                _unityLight.range = value;
            }
        }

        public float SpotAngle
        {
            get
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                return _unityLight.spotAngle;
            }
            set
            {
                if (!_unityLight)
                {
                    _unityLight = GetComponent<Light>();
                }

                _unityLight.spotAngle = value;
            }
        }

        public int Index { get; private set; }

        public static readonly List<StationaryLight> Lights = new();

        public static readonly int GlobalStationaryLightPositionsAndAttenuationShaderId =
            Shader.PropertyToID("_GlobalStationaryLightPositionsAndAttenuation");

        public static readonly int GlobalStationaryLightColorsShaderId =
            Shader.PropertyToID("_GlobalStationaryLightColors");

        public static readonly int StationaryLightIndicesShaderId = Shader.PropertyToID("_StationaryLightIndices");
        public static readonly int StationaryLightIndices2ShaderId = Shader.PropertyToID("_StationaryLightIndices2");
        public static readonly int StationaryLightCountShaderId = Shader.PropertyToID("_StationaryLightCount");

        private static Coroutine _updateDirtiedMeshesRoutine;

        private List<MeshRenderer> _affectedRenderers = new();
        private Light _unityLight;
        private static readonly List<(Vector3 Position, float Range)> _threadSafeLightData = new();

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, Range);
        }

        private void Awake()
        {
            Lights.Add(this);
            _threadSafeLightData.Add((transform.position, Range));
        }

        private void OnDestroy()
        {
            try
            {
                Lights.Remove(this);
            }
            catch (Exception)
            {
                Debug.LogError(
                    $"[{nameof(StationaryLight)}] Light collection unexpectedly does not contain light {name} on destroy.");
            }
        }

        private void OnEnable()
        {
            Profiler.BeginSample("Stationary light enabled");
            for (var i = 0; i < _affectedRenderers.Count; i++)
            {
                GameGlobals.Lights.AddLightOnRenderer(this, _affectedRenderers[i]);
            }

            Profiler.EndSample();
        }

        private void OnDisable()
        {
            Profiler.BeginSample("Stationary light disable");
            for (var i = 0; i < _affectedRenderers.Count; i++)
            {
                GameGlobals.Lights.RemoveLightOnRenderer(this, _affectedRenderers[i]);
            }

            Profiler.EndSample();
        }

        public static void InitStationaryLights()
        {
            Debug.Log($"[{nameof(StationaryLight)}] Total stationary light count: {Lights.Count}");

            // e.g. if we disabled Vob loading within FeatureFlags.
            if (Lights.IsEmpty())
            {
                return;
            }

            var lightPositionsAndAttenuation = new Vector4[Lights.Count];
            var lightColors = new Vector4[Lights.Count];
            for (var i = 0; i < Lights.Count; i++)
            {
                Lights[i].Index = i;
                Lights[i].GatherRenderers();
                lightPositionsAndAttenuation[i] = new Vector4(Lights[i].transform.position.x,
                    Lights[i].transform.position.y, Lights[i].transform.position.z,
                    1f / (Lights[i].Range * Lights[i].Range));
                lightColors[i] = Lights[i].Color.linear;
                Lights[i].gameObject.SetActive(false);
                Lights[i].gameObject.SetActive(true);
            }

            Shader.SetGlobalVectorArray(GlobalStationaryLightPositionsAndAttenuationShaderId,
                lightPositionsAndAttenuation);
            Shader.SetGlobalVectorArray(GlobalStationaryLightColorsShaderId, lightColors);
            _threadSafeLightData.Clear(); // Clear the thread safe data as it is no longer needed.
        }

        public static int CountLightsInBounds(Bounds bounds)
        {
            var count = 0;
            for (var i = 0; i < Lights.Count; i++)
            {
                var lightBounds = new Bounds(_threadSafeLightData[i].Position,
                    Vector3.one * _threadSafeLightData[i].Range * 2);
                if (bounds.Intersects(lightBounds))
                {
                    count++;
                }
            }

            return count;
        }

        private void GatherRenderers()
        {
            var colliders = Physics.OverlapSphere(transform.position, Range);
            for (var i = 0; i < colliders.Length; i++)
            {
                var renderer = colliders[i].GetComponent<MeshRenderer>();
                if (renderer)
                {
                    _affectedRenderers.Add(renderer);
                }
            }
        }
    }
}
