using GUZ.Core.Context;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager
{
    /// <summary>
    /// We need to reference this class inside Bootstrap scene, otherwise it won't get called by Unity during gameplay.
    /// </summary>
    public class MainMenuManager : SingletonBehaviour<MainMenuManager>
    {
        private void Start()
        {
            GUZEvents.MainMenuSceneLoaded.AddListener(delegate
            {
                GUZContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            });
        }
    }
}
