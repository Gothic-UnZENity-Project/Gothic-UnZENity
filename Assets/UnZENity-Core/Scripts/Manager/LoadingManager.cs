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

        private LoadingProgressType _currentType;
        private float _currentAmountPerUpdate;
        
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
            var textureNameForLoadingScreen = GameGlobals.SaveGame.IsNewGame
                ? "LOADING.TGA"
                : $"LOADING_{GameGlobals.SaveGame.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}.TGA";
            GameGlobals.Textures.SetTexture(textureNameForLoadingScreen, GameGlobals.Textures.GothicLoadingMenuMaterial);
            
            var tm = GameGlobals.Textures;
            loadingArea.FindChildRecursively("LoadingImage").GetComponent<Image>().material = tm.GothicLoadingMenuMaterial;
            loadingArea.FindChildRecursively("ProgressBackground").GetComponent<Image>().material = tm.LoadingBarBackgroundMaterial;
            loadingArea.FindChildRecursively("ProgressBar").GetComponent<Image>().material = tm.LoadingBarMaterial;
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

        /// <summary>
        /// We only load one type of game data at once. We can therefore set it initially and call an AddProgress() without parameters later.
        /// </summary>
        public void SetProgressStep(LoadingProgressType type, float amountPerUpdate)
        {
            _currentType = type;
            _currentAmountPerUpdate = amountPerUpdate;
        }

        /// <summary>
        /// Add a single progress entry based on SetProgressStep() data.
        /// </summary>
        public void AddProgress()
        {
            AddProgress(_currentType, _currentAmountPerUpdate);
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
