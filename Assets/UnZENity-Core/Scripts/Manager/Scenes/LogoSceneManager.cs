using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using UnityEngine;
using UnityEngine.Video;

namespace GUZ.Core.Manager.Scenes
{
    public class LogoSceneManager : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;

        private Queue<string> logoVideos = new();

        private void Start()
        {
            _videoPlayer.loopPointReached += LoadNextLogo;

            logoVideos = new Queue<string>(GameGlobals.Video.VideoFilePathsMp4.Where(i => i.ContainsIgnoreCase("logo")));
            
            // Start first logo
            LoadNextLogo(_videoPlayer);
        }

        private void LoadNextLogo(VideoPlayer player)
        {
            if (logoVideos.IsEmpty())
            {
#pragma warning disable CS4014 // Do not wait. Just go on.
                GameGlobals.Scene.LoadMainMenuScene();
#pragma warning restore CS4014
                return;
            }
            
            player.url = logoVideos.Dequeue();
            player.Play();
        }
    }
}
