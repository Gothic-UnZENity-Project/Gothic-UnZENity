using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class VideoManager
    {
        public List<string> VideoFileNamesMp4 = new();
        public List<string> VideoFilePathsMp4 = new();


        public VideoManager(DeveloperConfig config)
        {
            // NOP
        }
        
        public void InitVideos()
        {
            var videoFileFolder = $"{GameContext.GameVersionAdapter.RootPath}/_work/DATA/video/";

            if (!Directory.Exists(videoFileFolder))
            {
                Debug.LogError($"Video folder >{videoFileFolder}< not found!");
                return;
            }

            VideoFilePathsMp4 = Directory.EnumerateFiles(videoFileFolder, "*.mp4").ToList();
            VideoFileNamesMp4 = VideoFilePathsMp4.Select(Path.GetFileName).ToList();

            if (VideoFilePathsMp4.IsEmpty())
            {
                Debug.LogWarning($"No MP4 videos found in the video folder at >{videoFileFolder}<.");
            }
        }
    }
}
