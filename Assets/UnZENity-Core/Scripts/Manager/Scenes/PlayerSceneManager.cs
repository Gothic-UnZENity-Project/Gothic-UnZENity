using GUZ.Core.Config;
using GUZ.Core.Globals;
using GUZ.Core.Services.Context;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class PlayerSceneManager : MonoBehaviour, ISceneManager
    {
        [Inject] private readonly ConfigManager _configManager;
        [Inject] private readonly ContextInteractionService _contextInteractionService;

        public void Init()
        {
            GameManager.I.InitPhase1();

            _contextInteractionService.SetupPlayerController(_configManager.Dev);

            GlobalEventDispatcher.PlayerSceneLoaded.Invoke();

            GameManager.I.LoadScene(Constants.SceneGameVersion);
        }
    }
}
