using GUZ.Core.Context;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager
{
    public class PlayerManager
    {
        public PlayerManager(GameConfiguration config)
        {
            // Nothing to do for now. Might be needed later.
        }

        public void Init()
        {
            // Load the player controller upon MainMenu loaded
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(delegate
            {
                GuzContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            });
            
            // We also need a player controller in loading scene. At least for VR head movements.
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(delegate
            {
                GuzContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene(), new Vector3(10000, 10000, 10000)); // Spawned in a galaxy far far away.
            });
        }
    }
}
