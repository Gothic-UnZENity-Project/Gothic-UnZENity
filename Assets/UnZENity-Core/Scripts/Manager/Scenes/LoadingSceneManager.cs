using GUZ.Core.Adapter.UI.LoadingBars;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class LoadingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField]
        private AbstractLoadingBarHandler _loadingBarHandler;
        
        public void Init()
        {
            GameGlobals.Loading.InitLoading(_loadingBarHandler);

            GameContext.ContextInteractionService.TeleportPlayerTo(_loadingBarHandler.transform.position);
            
            GlobalEventDispatcher.LoadingSceneLoaded.Invoke();

            // Start loading world!
            GameManager.I.LoadScene(GameGlobals.SaveGame.CurrentWorldName);
        }
    }
}
