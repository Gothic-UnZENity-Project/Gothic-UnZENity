using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager.Scenes
{
    public class PlayerSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            GameManager.I.InitPhase1();


            GameContext.ContextInteractionService.CreatePlayerController(SceneManager.GetSceneByName(Constants.ScenePlayer));
            GameContext.ContextInteractionService.CreateVRDeviceSimulator();

            GlobalEventDispatcher.PlayerSceneLoaded.Invoke();

            GameManager.I.LoadScene(Constants.SceneGameVersion);
        }
    }
}
