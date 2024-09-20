using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class GameVersionSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            /*
             * 1. Check for GameSettings if there are two valid game installations
             * 2. If there's none: Show error message!
             * 3. If there is one, skip this scene immediately
             * 4. If there are two, show selection between these two games. Once one is selected, call GUZContext.SetGameVersion(version)
             */


            Debug.Log("GameVersionSceneManager initialized");
        }
    }
}
