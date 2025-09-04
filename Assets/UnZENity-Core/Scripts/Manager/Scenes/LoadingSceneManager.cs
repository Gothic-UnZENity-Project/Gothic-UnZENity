using GUZ.Core.Adapters.UI.LoadingBars;
using GUZ.Core.Services.World;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class LoadingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private AbstractLoadingBarHandler _loadingBarHandler;
        [Inject] private readonly SaveGameService _saveGameService;

        public void Init()
        {
            GameGlobals.Loading.InitLoading(_loadingBarHandler);

            GameContext.ContextInteractionService.TeleportPlayerTo(_loadingBarHandler.transform.position);
            
            GlobalEventDispatcher.LoadingSceneLoaded.Invoke();

            // Start loading world!
            GameManager.I.LoadScene(_saveGameService.CurrentWorldName);
        }
    }
}
