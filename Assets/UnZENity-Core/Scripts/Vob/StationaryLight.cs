using System;
using System.Collections.Generic;
using UnityEngine;

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

        public int Index { get; set; }

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

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, Range);
        }

        private void Awake()
        {
            Lights.Add(this);
            GameGlobals.Lights.AddThreadSafeLight(transform.position, Range);
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


        private bool _affectedRenderersCalculated;

        private void OnEnable()
        {
            if (!GameGlobals.Lights.IsWorldInitialized)
            {
                return;
            }

            // We need to calculate the renderers "after" we loaded all of world mesh and VOBs. Otherwise the Physics.Range() check will return too few elements.
            if (!_affectedRenderersCalculated)
            {
                GatherRenderers();
                _affectedRenderersCalculated = true;
            }

            foreach (var rend in _affectedRenderers)
            {
                GameGlobals.Lights.AddLightOnRenderer(this, rend);
            }
        }

        private void OnDisable()
        {
            foreach (var rend in _affectedRenderers)
            {
                GameGlobals.Lights.RemoveLightOnRenderer(this, rend);
            }
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
