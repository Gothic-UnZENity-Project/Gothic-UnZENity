using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Core.Logging;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Manager
{
    public class VideoManager
    {
        public List<string> VideoFileNamesMp4 = new();
        public List<string> VideoFilePathsMp4 = new();


        public void InitVideos()
        {
            var videoFileFolder = $"{GameContext.ContextGameVersionService.RootPath}/_work/DATA/video/";

            if (!Directory.Exists(videoFileFolder))
            {
                Logger.LogError($"Video folder >{videoFileFolder}< not found!", LogCat.Loading);
                return;
            }

            VideoFilePathsMp4 = Directory.EnumerateFiles(videoFileFolder, "*.mp4").ToList();
            VideoFileNamesMp4 = VideoFilePathsMp4.Select(Path.GetFileName).ToList();

            if (VideoFilePathsMp4.IsEmpty())
            {
                Logger.LogWarning($"No MP4 videos found in the video folder at >{videoFileFolder}<.", LogCat.Loading);
            }
        }
    }
}
