using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.Video;

namespace GUZ.Core.Manager.Scenes
{
    public class LogoSceneManager : MonoBehaviour , ISceneManager
    {
        [SerializeField] private VideoPlayer _videoPlayer;

        private Queue<string> _logoVideos = new();

        public void Init()
        {
            Debug.Log($"INI: playLogoVideos = {GameGlobals.Config.Gothic.IniPlayLogoVideos}");
            if (GameGlobals.Config.Gothic.IniPlayLogoVideos)
            {
                GameManager.I.LoadScene(Constants.SceneMainMenu, Constants.SceneLogo);
                return;
            }

            _videoPlayer.loopPointReached += LoadNextLogo;

            _logoVideos = new Queue<string>(GameGlobals.Video.VideoFilePathsMp4.Where(i => i.StartsWithIgnoreCase("logo")));

            if (_logoVideos.IsEmpty())
            {
                Debug.Log("No logo videos in format .mp4 found. Skipping scene.");
            }

            // Start first logo
            LoadNextLogo(null);
        }

        private void LoadNextLogo(VideoPlayer _)
        {
            if (_logoVideos.IsEmpty())
            {
                GameManager.I.LoadScene(Constants.SceneMainMenu, Constants.SceneLogo);
                return;
            }
            
            _videoPlayer.url = _logoVideos.Dequeue();
            _videoPlayer.Play();
        }
    }
}
