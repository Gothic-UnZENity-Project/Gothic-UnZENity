using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (!GameGlobals.Config.EnableMainMenu)
            {
                // Load world.zen
                // TODO - In future, we can also fetch name of scene to load from another config setting.
                GameManager.I.LoadWorld(Constants.SelectedWorld, -1, SceneManager.GetActiveScene().name);
                return;
            }
            
            // We set the gothic background image in MainMenu with this material.
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuImageBackgroundMaterial;

            GameContext.InteractionAdapter.InitUIInteraction();
            GameContext.InteractionAdapter.TeleportPlayerTo(Vector3.zero);
        }
    }
}
