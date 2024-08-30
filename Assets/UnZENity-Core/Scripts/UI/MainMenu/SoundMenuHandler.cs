using GUZ.Core.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.Core.UI.MainMenu
{
    public class SoundMenuHandler : MonoBehaviour
    {
        [SerializeField] private Slider _musicVolumeSlider;

        
        private void Start()
        {
            // Init field values.
            _musicVolumeSlider.value = PlayerPrefsManager.MusicVolume;
        }
        
        public void OnMusicVolumeChanged(float value)
        {
            PlayerPrefsManager.MusicVolume = value;
        }
    }
}
