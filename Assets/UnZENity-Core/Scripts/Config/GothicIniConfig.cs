using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MyBox;

namespace GUZ.Core.Config
{
    public class GothicIniConfig
    {
        private readonly Dictionary<string, string> _config;
        private readonly GothicIniWriter _gothicIniWriter;
        
        public readonly string IniFilePath;
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

        
        public GothicIniConfig(Dictionary<string, string> config, string iniFilePath)
        {
            _config = config;
            IniFilePath = iniFilePath;
            _gothicIniWriter = new GothicIniWriter(iniFilePath);
        }
        
        public void SetInt(string section, string key, int value)
        {
            _config[key] = value.ToString();

            _gothicIniWriter.WriteSetting(section, key, value.ToString());
            GlobalEventDispatcher.PlayerPrefUpdated.Invoke(key, value);
        }
        
        public int GetInt(string settingName, int defaultValue = 0)
        {
            if (_config.TryGetValue(settingName, out var value))
                return Convert.ToInt32(value);
            else
                return defaultValue;
        }
    }
}
