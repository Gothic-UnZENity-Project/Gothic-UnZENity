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

            if (!GameGlobals.Config.EnableMainMenu)
            {
                // We need to invoke this event, even when we skip MainMenu (for event listeners, main menu is 'loaded')
                GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();

                if (GameGlobals.Config.LoadFromSaveSlot)
                {
                    var saveId = GameGlobals.Config.SaveSlotToLoad;
                    var save = SaveGameManager.GetSaveGame(saveId);
                    GameManager.I.LoadWorld(save.Metadata.World, GameGlobals.Config.SaveSlotToLoad, Constants.SceneMainMenu);
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
            var world = GameGlobals.Config.PreselectWorldToSpawn;

            if (world == WorldToSpawn.None)
            {
                // FIXME - Read default from INI file
                return Constants.SelectedWorld;
            }
            else
            {
                return GameConfiguration.WorldMappings[world];
            }
        }
    }
}
