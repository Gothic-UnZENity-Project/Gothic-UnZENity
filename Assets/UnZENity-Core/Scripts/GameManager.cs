using System.Diagnostics;
using System.Globalization;
using GUZ.Core.Adapters.Scenes;
using GUZ.Core.Const;
using GUZ.Core.Core.Logging;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Models.Config;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.StaticCache;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using GUZ.Core.Util;
using MyBox;
using Reflex.Attributes;
using UnityEngine.SceneManagement;
using ZenKit;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core
{
    public class GameManager : SingletonBehaviour<GameManager>
    {
        public DeveloperConfig DeveloperConfig;

        private FileLoggingHandler _fileLoggingHandler;


        public VobMeshCullingService VobMeshCulling { get; private set; }
        public SpeechToTextService SpeechToText { get; private set; }


        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly ContextMenuService _contextMenuService;
        [Inject] private readonly ContextDialogService _contextDialogService;
        [Inject] private readonly ContextGameVersionService _contextGameVersionService;

        [Inject] private readonly UnityMonoService _unityMonoService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly SpeechToTextService _speechToTextService;
        [Inject] private readonly GameTimeService _gameTimeService;

        [Inject] private readonly NpcMeshCullingService _npcMeshCullingService;
        [Inject] private readonly VobMeshCullingService _vobMeshCullingService;
        [Inject] private readonly VobSoundCullingService _vobSoundCullingService;

        [Inject] private readonly SkyService _skyService;
        [Inject] private readonly BarrierService _barrierService;
        [Inject] private readonly LoadingService _loadingService;
        [Inject] private readonly TextureService _textureService;
        [Inject] private readonly SaveGameService _saveGameService;
        [Inject] private readonly PlayerService _playerService;
        
        [Inject] private readonly FrameSkipperService _frameSkipperService;
        [Inject] private readonly VobService _vobService;
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly NpcService _npcService;
        [Inject] private readonly NpcAiService _npcAiService;
        [Inject] private readonly RoutineService _routineService;
        
        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly StaticCacheService _staticCacheService;


        protected override void Awake()
        {
            base.Awake();

            GameContext.IsLab = false;

            // FIXME - Hack for now. Once we get rid of the GameContext global, we will remove these lines.
            GameContext.ContextInteractionService = _contextInteractionService;

            // We need to set culture to this, otherwise e.g. polish numbers aren't parsed correct.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            // Simply set "any" MonoBehaviour (like this) as object to use within game logic.
            _unityMonoService.SetMonoBehaviour(this);

            _configService.LoadRootJson();
            _configService.SetDeveloperConfig(DeveloperConfig);

            _fileLoggingHandler = new FileLoggingHandler();

            _multiTypeCacheService.Init();

            VobMeshCulling = _vobMeshCullingService;
            SpeechToText = _speechToTextService;

            ZenKit.Logger.Set(_configService.Dev.ZenKitLogLevel, Logger.OnZenKitLogMessage);
            DirectMusic.Logger.Set(_configService.Dev.DirectMusicLogLevel, Logger.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init(_configService.Root);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Call init function of BootstrapSceneManager directly as it kicks off cleaning up of further loaded scenes.
            SceneManager.GetActiveScene().GetComponentInChildren<BootstrapScene>()!.Init();
        }

        /// <summary>
        /// Init when game starts and Controls are set already, but no Gothic game version is selected so far.
        /// </summary>
        public void InitPhase1()
        {
            GlobalEventDispatcher.RegisterControlsService.Invoke(_configService.Dev.GameControls);

            _frameSkipperService.Init();
            VobMeshCulling.Init();
            _npcMeshCullingService.Init();
            _vobSoundCullingService.Init();
            _gameTimeService.Init();
            _routineService.Init();
        }

        /// <summary>
        /// Once Gothic version is selected, we can now initialize remaining managers.
        /// </summary>
        public void InitPhase2(GameVersion version)
        {
            var watch = Stopwatch.StartNew();

            GlobalEventDispatcher.RegisterGameVersionService.Invoke(version);
            GameContext.ContextGameVersionService = _contextGameVersionService;

            _configService.LoadGothicInis(version);

            var gothicRootPath = GameContext.ContextGameVersionService.RootPath;

            // Otherwise, continue loading Gothic.
            Logger.Log($"Initializing Gothic installation at: {gothicRootPath}", LogCat.Loading);
            ResourceLoader.Init(gothicRootPath);

            _audioService.InitMusic();
            _staticCacheService.Init(DeveloperConfig);
            _textureService.Init();
            _vobService.Init();
            _npcService.Init();

            Bootstrapper.Boot();
            SpeechToText.Init(); // Init after language set.

            GlobalEventDispatcher.LevelChangeTriggered.AddListener((world, spawn) =>
            {
                _playerService.LastLevelChangeTriggerVobName = spawn;
                LoadWorld(world, SaveGameService.SlotId.WorldChangeOnly, SceneManager.GetActiveScene().name);
            });

            watch.Log("Phase2 done. (mostly ZenKit initialized)");
        }

        public void LoadScene(string sceneName, string unloadScene = null)
        {
            if (unloadScene.NotNullOrEmpty())
            {
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(unloadScene));
            }

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>
        /// Gothic saves start with number 1
        /// saveGameId = 0 -> New Game
        /// saveGameId = -1 -> Change World
        /// </summary>
        /// <param name="saveGameId">-1-15</param>
        public void LoadWorld(string worldName, SaveGameService.SlotId saveGameId, string sceneToUnload = null)
        {
            // We need to add .zen as early as possible as all related data needs the file ending.
            worldName += worldName.EndsWithIgnoreCase(".zen") ? "" : ".zen";

            // Pre-load ZenKit save game data now. Can be reused by LoadingSceneManager later.
            if (saveGameId == SaveGameService.SlotId.NewGame)
            {
                _saveGameService.LoadNewGame();
            }
            else if (saveGameId > 0)
            {
                _saveGameService.LoadSavedGame(saveGameId);
            }
            else
            {
                // If we have saveGameId -1 that means to just change the world and keep the same data.
            }
            _saveGameService.ChangeWorld(worldName);

            LoadScene(Constants.SceneLoading, sceneToUnload);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.Log($"Scene loaded: {scene.name}", LogCat.Loading);

            // Newly created scenes are always the ones which we set as main scenes (i.e. new GameObjects will spawn in here automatically)
            SceneManager.SetActiveScene(scene);

            var sceneManager = scene.GetComponentInChildren<IScene>();
            if (sceneManager == null)
            {
                Logger.LogError($"{nameof(IScene)} for scene >{scene.name}< not found. Game won't proceed as " +
                                "bootstrapper for scene is invalid/non-existent.", LogCat.Loading);
                return;
            }
            sceneManager.Init();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Logger.Log($"Scene unloaded: {scene.name}", LogCat.Loading);
        }

        public void OnDestroy()
        {
            _fileLoggingHandler.Destroy();

            VobMeshCulling = null;
        }
    }
}
