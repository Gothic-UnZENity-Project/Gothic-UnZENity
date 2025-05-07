using System.Collections.Generic;

namespace GUZ.Core.Config
{
    public class GothicGameIniConfig
    {
        private readonly Dictionary<string, string> _config;

        public readonly string IniFilePath;

        // TODO - Add INI entries here


        public GothicGameIniConfig(Dictionary<string, string> config, string iniFilePath)
        {
            _config = config;
            IniFilePath = iniFilePath;
        }
    }
}
