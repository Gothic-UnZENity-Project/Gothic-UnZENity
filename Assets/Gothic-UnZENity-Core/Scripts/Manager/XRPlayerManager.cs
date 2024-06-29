using GUZ.Core.Context;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager
{
    public class XRPlayerManager
    {
        public XRPlayerManager(GameConfiguration config)
        {
            // Nothing to do for now. Might be needed later.
        }

        public void Init()
        {
            // Load the player controller upon MainMenu loaded
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(delegate
            {
                GUZContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            });
        }
    }
}
