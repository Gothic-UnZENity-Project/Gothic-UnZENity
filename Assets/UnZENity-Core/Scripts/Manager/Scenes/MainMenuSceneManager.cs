using GUZ.Core.Const;
using GUZ.Core.Models.Config;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.World;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    /// <summary>
    /// Specific manager for MainMenu.unity scene tasks only.
    /// </summary>
    public class MainMenuSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _mainMenuImageBackground;

        
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly TextureService _textureService;
        [Inject] private readonly SaveGameService _saveGameService;


        public void Init()
        {
            GameContext.ContextInteractionService.InitUIInteraction();

            if (!_configService.Dev.EnableMainMenu)
            {
                // We need to invoke this event, even when we skip MainMenu (for event listeners, main menu is 'loaded')
                GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();

                if (_configService.Dev.LoadFromSaveSlot)
                {
                    var saveId = (SaveGameService.SlotId)_configService.Dev.SaveSlotToLoad;
                    var save = _saveGameService.GetSaveGame(saveId);
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
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material = _textureService.MainMenuImageBackgroundMaterial;

            GameContext.ContextInteractionService.TeleportPlayerTo(Vector3.zero);
            GameContext.ContextInteractionService.DisableMenus();

            GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();
        }

        private string GetWorldNameToSpawn()
        {
            var world = _configService.Dev.PreselectWorldToSpawn;

            if (world == DeveloperConfigEnums.WorldToSpawn.None)
                return _configService.GothicGame.World;
            else
                return DeveloperConfigEnums.WorldMappings[world];
        }
    }
}
