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
            GameContext.ContextInteractionService.InitUIInteraction();

            if (!GameGlobals.Config.Dev.EnableMainMenu)
            {
                // We need to invoke this event, even when we skip MainMenu (for event listeners, main menu is 'loaded')
                GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();

                if (GameGlobals.Config.Dev.LoadFromSaveSlot)
                {
                    var saveId = (SaveGameManager.SlotId)GameGlobals.Config.Dev.SaveSlotToLoad;
                    var save = GameGlobals.SaveGame.GetSaveGame(saveId);
                    GameManager.I.LoadWorld(save.Metadata.World, saveId, Constants.SceneMainMenu);
                }
                else
                {
                    // Load New game at certain world
                    GameManager.I.LoadWorld(GetWorldNameToSpawn(), 0, Constants.SceneMainMenu);
                }
                return;
            }
            
            // We set the gothic background image in MainMenu with this material.
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuImageBackgroundMaterial;

            GameContext.ContextInteractionService.TeleportPlayerTo(Vector3.zero);
            GameContext.ContextInteractionService.DisableMenus();

            GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();
        }

        private string GetWorldNameToSpawn()
        {
            var world = GameGlobals.Config.Dev.PreselectWorldToSpawn;

            if (world == DeveloperConfigEnums.WorldToSpawn.None)
                return GameGlobals.Config.GothicGame.World;
            else
                return DeveloperConfigEnums.WorldMappings[world];
        }
    }
}
