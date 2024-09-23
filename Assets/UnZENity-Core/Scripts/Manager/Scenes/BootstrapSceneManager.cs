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

        // FIXME - Move to Player.unity scene
        [SerializeField] private AudioSource _music;

        // FIXME - Move to Player.unity scene
        private void Start()
        {
            _music.volume = PlayerPrefsManager.MusicVolume;
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefUpdated);
        }

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
            
            // Load Player scene
            GameManager.I.LoadScene(Constants.ScenePlayer);
        }

        // FIXME - Move to Player.unity scene
        private void OnPlayerPrefUpdated(string key, object value)
        {
            _music.volume = PlayerPrefsManager.MusicVolume;
        }
        
        // FIXME - Move to Player.unity scene
        private void OnDestroy()
        {
            GlobalEventDispatcher.PlayerPrefUpdated.RemoveListener(OnPlayerPrefUpdated);
        }
    }
}
