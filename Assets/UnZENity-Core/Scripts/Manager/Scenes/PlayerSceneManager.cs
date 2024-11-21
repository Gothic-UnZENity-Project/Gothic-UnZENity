using System.Linq;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager.Scenes
{
    public class PlayerSceneManager : MonoBehaviour, ISceneManager
    {

        public static GameObject PlayerController {get; internal set;}
        public static GameObject UICamera {get; internal set;}

        public void Init()
        {
            // TODO - Needed?
            // SceneManager.MoveGameObjectToScene(InteractionManager, _generalScene);

            PlayerController = GameContext.InteractionAdapter.CreatePlayerController(SceneManager.GetSceneByName(Constants.ScenePlayer));
            UICamera = GetUICamera();
            GameContext.InteractionAdapter.CreateVRDeviceSimulator();

            GlobalEventDispatcher.PlayerSceneLoaded.Invoke();

            GameManager.I.LoadScene(Constants.SceneGameVersion);
        }

        private GameObject GetUICamera(){
            return PlayerController.GetComponentsInChildren<Camera>().Where(x=>x.name == "UICamera").First().gameObject;
        }
    }
}
