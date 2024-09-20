using MyBox;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class LoadingSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            var textureNameForLoadingScreen = SaveGameManager.IsNewGame
                ? "LOADING.TGA"
                : $"LOADING_{SaveGameManager.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}.TGA";

            // On G1+world.zen it's either Gomez in his throne room (NewGame) or Gorn holding his Axe (LoadGame)
            GameGlobals.Textures.SetTexture(textureNameForLoadingScreen, GameGlobals.Textures.GothicLoadingMenuMaterial);

            // Start loading world!
            GameManager.I.LoadScene(SaveGameManager.CurrentWorldName);
        }
    }
}
