using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager.Scenes
{
    public class LoadingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _loadingArea;
        
        public void Init()
        {
            GlobalEventDispatcher.WorldFullyLoaded.AddListener(OnWorldFullyLoaded);
            
            GameGlobals.Loading.InitLoading(_loadingArea);

            GameContext.InteractionAdapter.TeleportPlayerTo(_loadingArea.transform.position);
            
            // Start loading world!
            GameManager.I.LoadScene(SaveGameManager.CurrentWorldName);
        }

        private void OnWorldFullyLoaded()
        {
            SceneManager.UnloadSceneAsync(Constants.SceneLoading);
        }

        private void OnDestroy()
        {
            GlobalEventDispatcher.WorldFullyLoaded.RemoveListener(OnWorldFullyLoaded);
        }
    }
}
