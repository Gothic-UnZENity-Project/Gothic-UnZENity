using System.Collections.Generic;

namespace GUZ.Core.Config
{
    public class GothicGameIniConfig
    {
        private readonly Dictionary<string, string> _config;

        // TBD - Add INI entries here


        public GothicGameIniConfig(Dictionary<string, string> config)
        {
            _config = config;
        }
    }
}
