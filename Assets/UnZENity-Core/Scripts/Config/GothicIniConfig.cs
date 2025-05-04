using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyBox;

namespace GUZ.Core.Config
{
    public class GothicIniConfig
    {
        public string IniSkyDayColor(int index) => _config.GetValueOrDefault($"zDayColor{index}", "0 0 0");
        public bool IniPlayLogoVideos => Convert.ToBoolean(Convert.ToInt16(_config.GetValueOrDefault("playLogoVideos", "1")));
        
        [NotNull]
        public string PlayerInstanceName
        {
            get
            {
                var  playerInstanceName = _config.GetValueOrDefault("playerInstanceName", "PC_HERO");
                return playerInstanceName.IsNullOrEmpty() ? "PC_HERO" : playerInstanceName;
            }
        }
        
        private readonly Dictionary<string, string> _config;


        public void SetInt(string settingName, int value)
        {
            _config[settingName] = value.ToString();

            GlobalEventDispatcher.PlayerPrefUpdated.Invoke(settingName, value);
        }
        
        public int GetInt(string settingName, int defaultValue = 0)
        {
            if (_config.TryGetValue(settingName, out var value))
                return Convert.ToInt32(value);
            else
                return defaultValue;
        }
        
        public GothicIniConfig(Dictionary<string, string> config)
        {
            _config = config;
        }
    }
}
