using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GUZ.Core.Manager
{
    public class StationaryLightsManager
    {
        private readonly HashSet<MeshRenderer> _dirtiedMeshes = new();
        private readonly Dictionary<MeshRenderer, List<StationaryLight>> _lightsPerRenderer = new();
        private readonly List<Material> _nonAllocMaterials = new();

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

            var indicesMatrix = Matrix4x4.identity;
            renderer.GetSharedMaterials(_nonAllocMaterials);
            for (var i = 0; i < Mathf.Min(16, _lightsPerRenderer[renderer].Count); i++)
            {
                indicesMatrix[i / 4, i % 4] = _lightsPerRenderer[renderer][i].Index;
            }

            for (var i = 0; i < _nonAllocMaterials.Count; i++)
            {
                if (_nonAllocMaterials[i])
                {
                    _nonAllocMaterials[i].SetMatrix(StationaryLight.StationaryLightIndicesShaderId, indicesMatrix);
                    _nonAllocMaterials[i].SetInt(StationaryLight.StationaryLightCountShaderId,
                        _lightsPerRenderer[renderer].Count);
                }
            }

            if (_lightsPerRenderer[renderer].Count >= 16)
            {
                for (var i = 0; i < Mathf.Min(16, _lightsPerRenderer[renderer].Count - 16); i++)
                {
                    indicesMatrix[i / 4, i % 4] = _lightsPerRenderer[renderer][i + 16].Index;
                }

                for (var i = 0; i < _nonAllocMaterials.Count; i++)
                {
                    if (_nonAllocMaterials[i])
                    {
                        _nonAllocMaterials[i].SetMatrix(StationaryLight.StationaryLightIndices2ShaderId, indicesMatrix);
                    }
                }
            }
        }
    }
}
