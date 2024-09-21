using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using MyBox;
using UnityEngine;
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

        private GameObject _progressBar;

        private readonly Dictionary<LoadingProgressType, float> _progressByType = new();

        
        public void Init()
        {
            // Initializing the Dictionary with the default progress (which is 0) for each type
            foreach (LoadingProgressType progressType in Enum.GetValues(typeof(LoadingProgressType)))
            {
                _progressByType.TryAdd(progressType, 0f);
            }
        }

        /// <summary>
        /// Called whenever a world loading starts (i.e. reset loading state and rearrange some data)
        /// </summary>
        public void InitLoading(GameObject loadingArea)
        {
            _progressBar = loadingArea.FindChildRecursively("ProgressBar");
            
            SetMaterialForLoading(loadingArea);
            ResetProgress();
        }

        private void SetMaterialForLoading(GameObject loadingArea)
        {
            // On G1+world.zen it's either Gomez in his throne room (NewGame) or Gorn holding his Axe (LoadGame)
            var textureNameForLoadingScreen = SaveGameManager.IsNewGame
                ? "LOADING.TGA"
                : $"LOADING_{SaveGameManager.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}.TGA";
            GameGlobals.Textures.SetTexture(textureNameForLoadingScreen, GameGlobals.Textures.GothicLoadingMenuMaterial);
            
            var sphere = loadingArea.FindChildRecursively("LoadingSphere");

            var tm = GameGlobals.Textures;
            sphere.GetComponent<MeshRenderer>().material = tm.LoadingSphereMaterial;
            sphere.FindChildRecursively("LoadingImage").GetComponent<Image>().material = tm.GothicLoadingMenuMaterial;
            sphere.FindChildRecursively("ProgressBackground").GetComponent<Image>().material = tm.LoadingBarBackgroundMaterial;
            sphere.FindChildRecursively("ProgressBar").GetComponent<Image>().material = tm.LoadingBarMaterial;
        }
        
        public void ResetProgress()
        {
            foreach (var progressType in _progressByType.Keys.ToList())
            {
                _progressByType[progressType] = 0f;
            }
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
            _progressBar.GetComponent<Image>().fillAmount = overallProgress;
        }

        public void AddProgress(LoadingProgressType progressType, float progress)
        {
            var newProgress = _progressByType[progressType] + progress;
            _progressByType[progressType] = newProgress;
            if (_progressBar != null)
            {
                UpdateLoadingBar();
            }
        }
    }
}
