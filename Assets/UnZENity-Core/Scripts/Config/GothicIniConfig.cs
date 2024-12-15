using System;
using System.Collections.Generic;

namespace GUZ.Core.Config
{
    public class GothicIniConfig
    {
        public string IniSkyDayColor(int index) => _config.GetValueOrDefault($"zDayColor{index}", "0 0 0");
        public bool IniPlayLogoVideos => Convert.ToBoolean(Convert.ToInt16(_config.GetValueOrDefault("playLogoVideos", "1")));
        public string PlayerInstanceName => _config.GetValueOrDefault("playerInstanceName", "PC_HERO");


        private readonly Dictionary<string, string> _config;

        public GothicIniConfig(Dictionary<string, string> config)
        {
            _config = config;
        }
    }
}
