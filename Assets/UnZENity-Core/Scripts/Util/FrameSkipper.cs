﻿using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace GUZ.Core.Util
{
    /// <summary>
    /// Leverage this class to check if an Async operation can continue or should wait until next frame.
    /// Idea is to have enough time for Unity's main thread to also execute other Update() calls, physics, etc.
    ///
    /// Hint: We set ScriptExecutionOrder to something early on to ensure we fetch frame time early on.
    /// </summary>
    public class FrameSkipper : MonoBehaviour
    {
        // We will leverage 50% (0.5f) of a frame time only.
        private const float _frameQuota = 0.5f;

        private static float _targetFrameRate;
        private static float _maxFrameUsage;


        /// <summary>
        /// We need to set the values via Init() to ensure LogFileHandler is set before our class inside GameManager.Init().
        /// </summary>
        public void Init()
        {
            _targetFrameRate = GameContext.InteractionAdapter.GetFrameRate();

            if (_targetFrameRate <= 0)
            {
                // We assume a default when no data is available.
                _targetFrameRate = 60;

                // Ite means there is some error in detecting the frame rate in a production build.
                if (!Application.isEditor)
                {
                    Logger.LogError($"[{typeof(FrameSkipper)}] No target frame rate found.", LogCat.Loading);
                }
            }
            Logger.Log($"[{typeof(FrameSkipper)}] Target frame rate set to: {_targetFrameRate}", LogCat.Loading);
        }


        /// <summary>
        /// Every update cycle, we calculate when it started and when our threshold of _skip to next frame_ is reached.
        /// </summary>
        private void Update()
        {
            // _debugSkippedCount = 0;
            _maxFrameUsage = Time.realtimeSinceStartup + _frameQuota / _targetFrameRate;
        }

        // private static int _debugSkippedCount;
        public static async Task TrySkipToNextFrame()
        {
            if (Time.realtimeSinceStartup > _maxFrameUsage)
            {
                // Logger.Log($"FrameSkipper: Not skipped {_debugSkippedCount}x.");
                await Task.Yield();
            }
            else
            {
                // _debugSkippedCount++;
            }
        }

        public static IEnumerator TrySkipToNextFrameCoroutine()
        {
            if (Time.realtimeSinceStartup > _maxFrameUsage)
            {
                // Logger.Log($"FrameSkipper: Not skipped {_debugSkippedCount}x.");
                yield return null;
            }
            else
            {
                // _debugSkippedCount++;
            }

        }
    }
}
