using GUZ.Core.Const;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Adapters.Scenes
{
    public class PlayerScene : MonoBehaviour, IScene
    {
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly ContextInteractionService _contextInteractionService;

        public void Init()
        {
            GameManager.I.InitPhase1();

            _contextInteractionService.SetupPlayerController(_configService.Dev);

            GlobalEventDispatcher.PlayerSceneLoaded.Invoke();

            GameManager.I.LoadScene(Constants.SceneGameVersion);
        }
    }
}
