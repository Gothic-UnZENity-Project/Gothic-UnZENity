using System.Collections.Generic;
using System.Threading.Tasks;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.Profiling;

namespace GUZ.Core.Manager
{
    public class StationaryLightsManager
    {
        private static readonly int _globalStationaryLightPositionsAndAttenuationShaderId =
            Shader.PropertyToID("_GlobalStationaryLightPositionsAndAttenuation");

        private static readonly int _globalStationaryLightColorsShaderId =
            Shader.PropertyToID("_GlobalStationaryLightColors");


        private readonly HashSet<MeshRenderer> _dirtiedMeshes = new();
        private readonly Dictionary<MeshRenderer, List<StationaryLight>> _lightsPerRenderer = new();

        public readonly static List<(Vector3 Position, float Range)> ThreadSafeLightData = new();

        public List<(Vector3 Position, float Range)> GetThreadSafeLiftData(){
            return ThreadSafeLightData;
        }


        public void AddThreadSafeLight(Vector3 position, float range)
        {
            ThreadSafeLightData.Add((position, range));
        }

        public void ClearThreadSafeLights()
        {
            ThreadSafeLightData.Clear();
        }

        public void LateUpdate()
        {
            // Update the renderer once for all updated lights.
            if (_dirtiedMeshes.Count > 0)
            {
                Profiler.BeginSample("Update stationary light renderers");
                foreach (var renderer in _dirtiedMeshes)
                {
                    UpdateRenderer(renderer);
                }

                _dirtiedMeshes.Clear();
                Profiler.EndSample();
            }
        }

        public void AddLightOnRenderer(StationaryLight light, MeshRenderer renderer)
        {
            if (!_lightsPerRenderer.ContainsKey(renderer))
            {
                _lightsPerRenderer.Add(renderer, new List<StationaryLight>());
            }

            _lightsPerRenderer[renderer].Add(light);
            _dirtiedMeshes.Add(renderer);
        }

        public void RemoveLightOnRenderer(StationaryLight light, MeshRenderer renderer)
        {
            if (!_lightsPerRenderer.ContainsKey(renderer))
            {
                return;
            }

            try
            {
                _lightsPerRenderer[renderer].Remove(light);
                _dirtiedMeshes.Add(renderer);
            }
            catch
            {
                //Debug.LogError($"[{nameof(StationaryLight)}] Light {name} wasn't part of {_affectedRenderers[i].name}'s lights on disable. This is unexpected.");
            }
        }

        private void UpdateRenderer(MeshRenderer renderer)
        {
            if (!renderer)
            {
                return;
            }

            var nonAllocMaterials = new List<Material>();
            var indicesMatrix = Matrix4x4.identity;
            renderer.GetSharedMaterials(nonAllocMaterials);
            for (var i = 0; i < Mathf.Min(16, _lightsPerRenderer[renderer].Count); i++)
            {
                indicesMatrix[i / 4, i % 4] = _lightsPerRenderer[renderer][i].Index;
            }

            for (var i = 0; i < nonAllocMaterials.Count; i++)
            {
                if (nonAllocMaterials[i])
                {
                    nonAllocMaterials[i].SetMatrix(StationaryLight.StationaryLightIndicesShaderId, indicesMatrix);
                    nonAllocMaterials[i].SetInt(StationaryLight.StationaryLightCountShaderId,
                        _lightsPerRenderer[renderer].Count);
                }
            }

            if (_lightsPerRenderer[renderer].Count >= 16)
            {
                for (var i = 0; i < Mathf.Min(16, _lightsPerRenderer[renderer].Count - 16); i++)
                {
                    indicesMatrix[i / 4, i % 4] = _lightsPerRenderer[renderer][i + 16].Index;
                }

                for (var i = 0; i < nonAllocMaterials.Count; i++)
                {
                    if (nonAllocMaterials[i])
                    {
                        nonAllocMaterials[i].SetMatrix(StationaryLight.StationaryLightIndices2ShaderId, indicesMatrix);
                    }
                }
            }
        }

        public static async Task InitializeThreadSafeLightData()
        {
            // Find all StationaryLight components, including those on inactive GameObjects
            var allLights = Resources.FindObjectsOfTypeAll<StationaryLight>(); // ~350 for G1

            for (var i = 0; i < allLights.Length; i++)
            {
                StationaryLight light = allLights[i];
                ThreadSafeLightData.Add((light.transform.position, light.Range));

                await FrameSkipper.TrySkipToNextFrame();
                GameGlobals.Loading?.AddProgress(LoadingManager.LoadingProgressType.WorldMesh, 1f / allLights.Length);
            }
        }

        /// <summary>
        /// Set global Shader data when world is being loaded.
        /// </summary>
        public void InitGlobalStationaryLights()
        {
            var lights = GameGlobals.StaticCache.LoadedStationaryLights.StationaryLights;

            var lightPositionsAndAttenuation = new Vector4[lights.Count];
            var lightColors = new Vector4[lights.Count];

            for (var i = 0; i < lights.Count; i++)
            {
                lightPositionsAndAttenuation[i] = new Vector4(
                    lights[i].Position.x, lights[i].Position.y, lights[i].Position.z,
                    1f / (lights[i].Range * lights[i].Range));
                lightColors[i] = lights[i].LinearColor;
            }

            Shader.SetGlobalVectorArray(_globalStationaryLightPositionsAndAttenuationShaderId,
                lightPositionsAndAttenuation);
            Shader.SetGlobalVectorArray(_globalStationaryLightColorsShaderId, lightColors);
        }
    }
}
