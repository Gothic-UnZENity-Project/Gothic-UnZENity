using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class VideoManager
    {
        public List<string> VideoFileNames = new();
        public List<string> VideoFilePaths = new();
        
        
        public VideoManager(GameConfiguration config)
        { }
        
        // TODO - We can trigger conversion from here later.
        public void Init()
        {
            var videoFileFolder = $"{GameGlobals.Settings.GothicIPath}/_work/DATA/video/";

            if (!Directory.Exists(videoFileFolder))
            {
                Debug.LogError($"Video folder >{videoFileFolder}< not found!");
                return;
            }

            VideoFilePaths = Directory.EnumerateFiles(videoFileFolder, "*.bik").ToList();
            VideoFileNames = VideoFilePaths.Select(i => Path.GetFileName(i)).ToList();
        }
    }
}
