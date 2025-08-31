using System.IO;
using GUZ.Core.Domain.Config;
using GUZ.Core.Models.Config;
using ZenKit;

namespace GUZ.Core.Services.Config
{
    /// <summary>
    /// Combines three sources of configuration:
    /// 1. Gothic.ini and GothicGame.ini from Gothic installation directory containing original Gothic settings
    /// 2. GameSettings.json from Gothic-UnZENity/StreamingAssets path for root configuration (e.g. log level)
    /// 3. DeveloperConfig ScriptableObject for developer settings
    /// </summary>
    public class ConfigService
    {
        public JsonRootConfig Root { get; private set; }
        public DeveloperConfig Dev { get; private set; }
        public GothicIniConfig Gothic { get; private set; }
        public GothicGameIniConfig GothicGame { get; private set; }


        /// <summary>
        /// First one to load.
        /// Root, as it contains only a few UnZENity specific bootstrap data like
        /// installation directory of Gothic1/2 and LogLevel.
        /// </summary>
        public void LoadRootJson()
        {
            Root = JsonRootLoader.Load();
        }

        /// <summary>
        /// Config provided from caller (basically GameManager or LabManager).
        /// </summary>
        public void SetDeveloperConfig(DeveloperConfig config)
        {
            // We simply reference the ScriptableObject from GameManager component.
            Dev = config;
        }

        /// <summary>
        /// Last one to be loaded. Whenever GameVersion is set already.
        /// </summary>
        public void LoadGothicInis(GameVersion version)
        {
            var rootPath = version == GameVersion.Gothic1 ? Root.Gothic1Path : Root.Gothic2Path;
            var gothicIniPath = Path.Combine(rootPath, "system/Gothic.ini");
            var gothicGameIniPath = Path.Combine(rootPath, "system/GothicGame.ini");

            Gothic = new GothicIniConfig(IniLoader.LoadFile(gothicIniPath), gothicIniPath);
            GothicGame = new GothicGameIniConfig(IniLoader.LoadFile(gothicGameIniPath), gothicGameIniPath);
            
            GlobalEventDispatcher.GothicInisInitialized.Invoke();
        }

        public bool CheckIfGothicInstallationExists(GameVersion version)
        {
            var gothicRootPath = version == GameVersion.Gothic1 ? Root.Gothic1Path : Root.Gothic2Path;

            var gothicDataPath = $"{gothicRootPath}/Data";
            var gothicWorkPath = $"{gothicRootPath}/_work";

            return Directory.Exists(gothicWorkPath) && Directory.Exists(gothicDataPath);
        }
    }
}
