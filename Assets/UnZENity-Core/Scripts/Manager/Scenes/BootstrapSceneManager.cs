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
            _music.volume = PlayerPrefsManager.MusicVolume;
            
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefUpdated);
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
