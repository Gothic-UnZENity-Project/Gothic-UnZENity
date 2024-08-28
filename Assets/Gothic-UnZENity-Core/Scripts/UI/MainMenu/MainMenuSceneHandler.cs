using UnityEngine;

namespace GUZ.Core.UI.MainMenu
{
    /// <summary>
    /// Specific manager for MainMenu.unity scene tasks only.
    /// </summary>
    public class MainMenuSceneHandler : MonoBehaviour
    {
        [SerializeField]
        private GameObject _mainMenuImageBackground;

        private void Start()
        {
            _mainMenuImageBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuImageBackgroundMaterial;
        }

    }
}
