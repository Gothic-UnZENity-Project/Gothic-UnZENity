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
            // We set the gothic background image in MainMenu with this material.
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuImageBackgroundMaterial;
        }
    }
}
