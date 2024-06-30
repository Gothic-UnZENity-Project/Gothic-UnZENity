using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GUZ.Core.Manager
{
    public class LoadingManager
    {
        public enum LoadingProgressType
        {
            WorldMesh,
            VOb,
            Npc
        }

        private GameObject _bar;

        private Scene _loadingScene;

        private const string _loadingSceneName = "Loading";

        private Dictionary<LoadingProgressType, float> _progressByType = new();

        public void Init()
        {
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(OnLoadingSceneLoaded);

            // Initializing the Dictionary with the default progress (which is 0) for each type
            foreach (LoadingProgressType progressType in Enum.GetValues(typeof(LoadingProgressType)))
            {
                if (!_progressByType.ContainsKey(progressType))
                {
                    _progressByType.Add(progressType, 0f);
                }
            }
        }

        public void ResetProgress()
        {
            foreach (var progressType in _progressByType.Keys.ToList())
            {
                _progressByType[progressType] = 0f;
            }
        }

        private void OnLoadingSceneLoaded()
        {
            SetBarFromScene();
            SetMaterialForLoading();
        }

        private void SetBarFromScene()
        {
            var sphere = GameObject.Find("LoadingSphere");
            _bar = sphere.FindChildRecursively("ProgressBar");
        }

        private void SetMaterialForLoading()
        {
            var scene = SceneManager.GetSceneByName(Constants.SceneLoading);
            var sphere = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "LoadingSphere");

            var tm = GameGlobals.Textures;
            sphere.GetComponent<MeshRenderer>().material = tm.LoadingSphereMaterial;
            sphere.FindChildRecursively("LoadingImage").GetComponent<Image>().material = tm.GothicLoadingMenuMaterial;
            sphere.FindChildRecursively("ProgressBackground").gameObject.GetComponent<Image>().material =
                tm.LoadingBarBackgroundMaterial;
            sphere.FindChildRecursively("ProgressBar").gameObject.GetComponent<Image>().material =
                tm.LoadingBarMaterial;
        }

        private float CalculateOverallProgress()
        {
            var totalProgress = 0f;
            var numTypes = _progressByType.Count;

            foreach (var progressPair in _progressByType)
            {
                totalProgress += progressPair.Value / numTypes;
            }

            return totalProgress;
        }

        private void UpdateLoadingBar()
        {
            // Calculate the overall progress based on individual progress values
            var overallProgress = CalculateOverallProgress();

            // Update the loading bar with the overall progress
            _bar.GetComponent<Image>().fillAmount = overallProgress;
        }

        public void SetProgress(LoadingProgressType progressType, float progress)
        {
            _progressByType[progressType] = progress;
            if (_bar != null)
            {
                UpdateLoadingBar();
            }
        }

        public void AddProgress(LoadingProgressType progressType, float progress)
        {
            var newProgress = _progressByType[progressType] + progress;
            _progressByType[progressType] = newProgress;
            if (_bar != null)
            {
                UpdateLoadingBar();
            }
        }
    }
}
