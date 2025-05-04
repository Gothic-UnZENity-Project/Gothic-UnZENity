using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using MyBox;

namespace GUZ.Core.Config
{
    public class GothicIniConfig
    {
        private readonly Dictionary<string, string> _config;
        private readonly GothicIniWriter _gothicIniWriter;
        
        public readonly string IniFilePath;

        public bool IniSubtitles => Convert.ToBoolean(_config.GetValueOrDefault("subTitles", "1"));
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

        public string GetString(string settingName, string defaultValue = "")
        {
            if (_config.TryGetValue(settingName, out var value))
                return value;
            else
                return defaultValue;
        }
        
        private void SetString(string section, string key, string value)
        {
            _config[key] = value;

            _gothicIniWriter.WriteSetting(section, key, value);
            GlobalEventDispatcher.PlayerPrefUpdated.Invoke(key, value);
        }
        
        public int GetInt(string settingName, int defaultValue = 0)
        {
            return Convert.ToInt32(GetString(settingName, defaultValue.ToString()));
        }
        
        public void SetInt(string section, string key, int value)
        {
            SetString(section, key, value.ToString());
        }
        
        public float GetFloat(string settingName, float defaultValue = 1f)
        {
            return Convert.ToSingle(GetString(settingName, defaultValue.ToString(CultureInfo.InvariantCulture)));
        }
        
        public void SetFloat(string section, string key, float value)
        {
            SetString(section, key, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
