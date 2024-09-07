using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public class VideoManager
    {
        public List<string> VideoFileNamesBik = new();
        public List<string> VideoFilePathsBik = new();
        
        public List<string> VideoFileNamesMp4 = new();
        public List<string> VideoFilePathsMp4 = new();


        public VideoManager(GameConfiguration config)
        {
            // NOP
        }
        
        // TODO - We can trigger conversion from here later.
        public void Init()
        {
            var videoFileFolder = $"{GameGlobals.Settings.GothicIPath}/_work/DATA/video/";

            if (!Directory.Exists(videoFileFolder))
            {
                Debug.LogError($"Video folder >{videoFileFolder}< not found!");
                return;
            }

            VideoFilePathsBik = Directory.EnumerateFiles(videoFileFolder, "*.bik").ToList();
            VideoFileNamesBik = VideoFilePathsBik.Select(Path.GetFileName).ToList();
            
            VideoFilePathsMp4 = Directory.EnumerateFiles(videoFileFolder, "*.mp4").ToList();
            VideoFileNamesMp4 = VideoFilePathsMp4.Select(Path.GetFileName).ToList();
        }
    }
}
