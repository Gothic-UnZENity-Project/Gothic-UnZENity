using GUZ.Core.Globals;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Manager.Scenes
{
    public class GameVersionSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            /*
             * 1. Check for GameSettings if there are two valid game installations
             * 2. If there's none: Show error message!
             * 3. If there is one, skip this scene immediately
             * 4. If there are two, show selection between these two games. Once one is selected, call GUZContext.SetGameVersion(version)
             */

            var isG1Installed = GameGlobals.Settings.CheckIfGothicInstallationExists(GameVersion.Gothic1);
            var isG2Installed = GameGlobals.Settings.CheckIfGothicInstallationExists(GameVersion.Gothic2);

            if (GameGlobals.Config.PreselectGameVersionUse)
            {
                var isInstalled = GameGlobals.Config.GameVersion == GameVersion.Gothic1 ? isG1Installed : isG2Installed;

                if (isInstalled)
                {
                    GameManager.I.InitPhase2(GameGlobals.Config.GameVersion);
                }

                return;
            }

            // Neither is installed
            if (!isG1Installed && !isG2Installed)
            {
                Debug.Log("No installation of Gothic1 nor Gothic2 found.");
            }
            // Both are installed
            else if (isG1Installed && isG2Installed)
            {
                Debug.Log("Gothic1 and Gothic2 installation found.");
            }
            // Only one is installed
            else
            {
                var version = isG1Installed ? GameVersion.Gothic1 : GameVersion.Gothic2;

                Debug.Log($"Installation for {version} found only.");

                GameManager.I.InitPhase2(version);
                GameManager.I.LoadScene(Constants.SceneMainMenu, true);
            }
        }
    }
}
