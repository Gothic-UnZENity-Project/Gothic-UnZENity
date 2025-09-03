﻿using System;
using System.Collections.Generic;
using GUZ.Core.Core.Logging;
using GUZ.Core.Util;
using UnityEngine;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Debugging
{
    /// <summary>
    /// Use whatever pre-caching data got saved at StaticCacheManager.SaveDebugCache().
    /// The actual loading of this data is hidden behind an Activate switch to ensure we don't waste resources during normal gameplay.
    /// </summary>
    public class PreCachingDebugDataDebug : MonoBehaviour
    {
        public bool Activate;

        private List<Vector3> _lightPositions = new();

        private async void OnValidate()
        {
            if (!Activate)
            {
                return;
            }

            try
            {
                await GameGlobals.StaticCache.LoadDebugCache(GameGlobals.SaveGame.CurrentWorldName);
                _lightPositions = GameGlobals.StaticCache.LoadedDebugData.LightPositions;
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString(), LogCat.Debug);
            }
        }

        private void OnDrawGizmos()
        {
            foreach (var lightPos in _lightPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(lightPos, 1f);
                Gizmos.color = Color.red;
            }
        }

    }
}
