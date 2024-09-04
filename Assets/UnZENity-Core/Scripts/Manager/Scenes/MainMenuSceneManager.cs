using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    /// <summary>
    /// Specific manager for MainMenu.unity scene tasks only.
    /// </summary>
    public class MainMenuSceneManager : MonoBehaviour
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
