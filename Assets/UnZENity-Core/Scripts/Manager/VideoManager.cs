using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Context;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class VideoManager
    {
        public List<string> VideoFileNamesMp4 = new();
        public List<string> VideoFilePathsMp4 = new();


        public VideoManager(GameConfiguration config)
        {
            // NOP
        }
        
        // TODO - We can trigger conversion from here later.
        public void Init()
        {
            var videoFileFolder = $"{GuzContext.GameVersionAdapter.RootPath}/_work/DATA/video/";

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
