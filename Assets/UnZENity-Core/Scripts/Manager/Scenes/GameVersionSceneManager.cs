using System;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using UnityEngine;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager.Scenes
{
    public class GameVersionSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _invalidInstallationDir;


        private void Awake()
        {
            // Just in case we forgot to disable it in scene view. ;-)
            _invalidInstallationDir.SetActive(false);
        }

        public void Init()
        {
            /*
             * 1. Check for GameSettings if there are two valid game installations
             * 2. If there's none: Show error message!
             * 3. If there is one, skip this scene immediately
             * 4. If there are two, show selection between these two games. Once one is selected, call GUZContext.SetGameVersion(version)
             */

            // Whatever comes next, we don't want the player to move around right now.
            GameContext.ContextInteractionService.LockPlayerInPlace();

            var isG1Installed = GameGlobals.Config.CheckIfGothicInstallationExists(GameVersion.Gothic1);
            var isG2Installed = GameGlobals.Config.CheckIfGothicInstallationExists(GameVersion.Gothic2);

            if (GameGlobals.Config.Dev.PreselectGameVersion)
            {
                var isInstalled = GameGlobals.Config.Dev.GameVersion == GameVersion.Gothic1 ? isG1Installed : isG2Installed;

                if (isInstalled)
                {
                    GameManager.I.InitPhase2(GameGlobals.Config.Dev.GameVersion);
                    GameManager.I.LoadScene(Constants.ScenePreCaching, Constants.SceneGameVersion);
                }
                else
                {
                    // If the Gothic installation directory is not set, show an error message and exit.
                    _invalidInstallationDir.SetActive(true);
                    throw new ArgumentException($"{GameGlobals.Config.Dev.GameVersion} installation couldn't be found inside >GameSettings.json< file.");
                }

                return;
            }

            // Neither is installed
            if (!isG1Installed && !isG2Installed)
            {
                Logger.LogWarning("No installation of Gothic1 nor Gothic2 found.", LogCat.Loading);

                _invalidInstallationDir.SetActive(true);
            }
            // Both are installed
            else if (isG1Installed && isG2Installed)
            {
                Logger.Log("Gothic1 and Gothic2 installation found.", LogCat.Loading);
            }
            // Only one is installed
            else
            {
                var version = isG1Installed ? GameVersion.Gothic1 : GameVersion.Gothic2;

                Logger.Log($"Installation for {version} found only.", LogCat.Loading);

                GameManager.I.InitPhase2(version);
                GameManager.I.LoadScene(Constants.SceneLogo, Constants.SceneGameVersion);
            }
        }
    }
}
