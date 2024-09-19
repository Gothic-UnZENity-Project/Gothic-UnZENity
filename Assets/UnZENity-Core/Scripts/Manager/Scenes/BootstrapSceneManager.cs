using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    /// <summary>
    /// Specific manager for Bootstrap.unity scene tasks only.
    /// </summary>
    public class BootstrapSceneManager : MonoBehaviour
    {

        [SerializeField] private AudioSource _music;


        private void Start()
        {
            StartCoroutine(BootUnity());
            
            _music.volume = PlayerPrefsManager.MusicVolume;
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefUpdated);
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
            SceneManager.LoadSceneAsync(Constants.ScenePlayer, LoadSceneMode.Additive);
        }
        
        private void OnPlayerPrefUpdated(string key, object value)
        {
            _music.volume = PlayerPrefsManager.MusicVolume;
        }
        
        private void OnDestroy()
        {
            GlobalEventDispatcher.PlayerPrefUpdated.RemoveListener(OnPlayerPrefUpdated);
        }

    }
}
