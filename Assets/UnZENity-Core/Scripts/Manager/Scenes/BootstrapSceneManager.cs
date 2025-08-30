using System.Collections;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager.Scenes
{
    /// <summary>
    /// Specific manager for Bootstrap.unity scene tasks only.
    /// </summary>
    public class BootstrapSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            StartCoroutine(BootUnity());
        }

        private IEnumerator BootUnity()
        {
            var bootstrapScene = SceneManager.GetSceneByName(Constants.SceneBootstrap);
            
            // Check if other scenes are active and remove them now (We need to have a clean Bootstrap scene at start time only).
            while (SceneManager.loadedSceneCount != 1)
            {
                var nextSceneIndex = SceneManager.GetSceneAt(0) == bootstrapScene ? 1 : 0;
                var nextScene = SceneManager.GetSceneAt(nextSceneIndex);
                SceneManager.UnloadSceneAsync(nextScene);
                yield return null;
            }

            // basically Flat or VR
            var playerContextName = GameContext.InteractionAdapter.GetContextName();

            // Load Player scene by full path name. Otherwise, it will not be found as Flat and VR module have same Player.unity scene name in use.
            GameManager.I.LoadScene($"UnZENity-{playerContextName}/Scenes/{playerContextName}/{Constants.ScenePlayer}");
        }
    }
}
