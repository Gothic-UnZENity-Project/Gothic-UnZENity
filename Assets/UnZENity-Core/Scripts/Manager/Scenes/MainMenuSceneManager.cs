using GUZ.Core.Config;
using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    /// <summary>
    /// Specific manager for MainMenu.unity scene tasks only.
    /// </summary>
    public class MainMenuSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField]
        private GameObject _mainMenuImageBackground;

        public void Init()
        {
            GameContext.InteractionAdapter.InitUIInteraction();

            if (!GameGlobals.Config.Dev.EnableMainMenu)
            {
                // We need to invoke this event, even when we skip MainMenu (for event listeners, main menu is 'loaded')
                GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();

                if (GameGlobals.Config.Dev.LoadFromSaveSlot)
                {
                    var saveId = GameGlobals.Config.Dev.SaveSlotToLoad;
                    var save = GameGlobals.SaveGame.GetSaveGame(saveId);
                    GameManager.I.LoadWorld(save.Metadata.World, GameGlobals.Config.Dev.SaveSlotToLoad, Constants.SceneMainMenu);
                }
                else
                {
                    // Load New game at certain world
                    GameManager.I.LoadWorld(GetWorldNameToSpawn(), -1, Constants.SceneMainMenu);
                }
                return;
            }
            
            // We set the gothic background image in MainMenu with this material.
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuImageBackgroundMaterial;

            GameContext.InteractionAdapter.TeleportPlayerTo(Vector3.zero);

            GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();
        }

        private string GetWorldNameToSpawn()
        {
            var world = GameGlobals.Config.Dev.PreselectWorldToSpawn;

            if (world == WorldToSpawn.None)
            {
                // FIXME - Read default from INI file
                return Constants.SelectedWorld;
            }
            else
            {
                return DeveloperConfig.WorldMappings[world];
            }
        }
    }
}
