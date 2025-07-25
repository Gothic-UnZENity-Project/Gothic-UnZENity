﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MyBox;

namespace GUZ.Core.Config
{
    public class GothicGameIniConfig
    {
        private readonly Dictionary<string, string> _config;

        public readonly string IniFilePath;

        public string Player => _config.GetValueOrDefault("player", "PC_HERO");
        public string World => _config.GetValueOrDefault("world", "World.zen");
        


        public GothicGameIniConfig(Dictionary<string, string> config, string iniFilePath)
        {
            _config = config;
            IniFilePath = iniFilePath;
        }
    }
}
