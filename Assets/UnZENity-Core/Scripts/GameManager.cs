using System.Diagnostics;
using System.Globalization;
using GUZ.Core.Animations;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Domain.Culling;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Scenes;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Npc;
using GUZ.Core.Services;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Culling;
using GUZ.Core.Util;
using GUZ.Manager;
using MyBox;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit;
// deprecated
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager), typeof(FrameSkipper))]
    public class GameManager : SingletonBehaviour<GameManager>, ICoroutineManager, IGlobalDataProvider
    {
        public DeveloperConfig DeveloperConfig;

        private FileLoggingHandler _fileLoggingHandler;
        private FrameSkipper _frameSkipper;

        public ConfigManager Config { get; private set; }
        public LocalizationManager Localization { get; private set; }

        public SaveGameManager SaveGame { get; private set; }

        public LoadingManager Loading { get; private set; }
        public StaticCacheManager StaticCache { get; private set; }

        public PlayerManager Player { get; private set; }
        public MarvinManager Marvin { get; private set;  }
        public SkyManager Sky { get; private set; }

        public GameTimeService Time { get; private set; }
        
        public VideoManager Video { get; private set; }
        
        public RoutineManager Routines { get; private set; }

        public TextureManager Textures { get; private set; }

        public StoryManager Story { get; private set; }

        public FontManager Font { get; private set; }

        public StationaryLightsManager Lights { get; private set; }

        public VobManager Vobs { get; private set; }
        public NpcManager Npcs { get; private set; }
        public NpcAiManager NpcAi { get; private set; }
        public AnimationManager Animations { get; private set; }
        public VobMeshCullingService VobMeshCulling { get; private set; }
        public NpcMeshCullingService NpcMeshCulling { get; private set; }
        public SpeechToTextService SpeechToText { get; private set; }


        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly ContextMenuService _contextMenuService;
        [Inject] private readonly ContextDialogService _contextDialogService;

        [Inject] private readonly UnityMonoService _unityMonoService;
        [Inject] private readonly MusicService _musicService;
        [Inject] private readonly SpeechToTextService _speechToTextService;
        [Inject] private readonly GameTimeService _gameTimeService;

        [Inject] private readonly NpcMeshCullingService _npcMeshCullingService;
        [Inject] private readonly VobMeshCullingService _vobMeshCullingService;
        [Inject] private readonly VobSoundCullingService _vobSoundCullingService;
        [Inject] private readonly NpcMeshCullingDomain _npcMeshCullingDomain;
        [Inject] private readonly VobMeshCullingDomain _vobMeshCullingDomain;
        [Inject] private readonly VobSoundCullingDomain _vobSoundCullingDomain;

        [Inject] private readonly SkyManager _skyManager;
        [Inject] private readonly BarrierManager _barrierManager;
        [Inject] private readonly LoadingManager _loadingManager;

        [Inject] private readonly VobManager _vobManager;
        [Inject] private readonly ConfigManager _configManager;

        protected override void Awake()
        {
            base.Awake();

            GameContext.IsLab = false;

            // FIXME - Hack for now. Once we get rid of the GameContext global, we will remove these lines.
            GameContext.ContextInteractionService = _contextInteractionService;
            GameContext.ContextMenuService = _contextMenuService;
            GameContext.ContextDialogService = _contextDialogService;
            
            // We need to set culture to this, otherwise e.g. polish numbers aren't parsed correct.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            // Simply set "any" MonoBehaviour (like this) as object to use within game logic.
            _unityMonoService.SetMonoBehaviour(this);

            Config = _configManager;
            Config.LoadRootJson();
            Config.SetDeveloperConfig(DeveloperConfig);

            _fileLoggingHandler = new FileLoggingHandler();
            _frameSkipper = GetComponent<FrameSkipper>();

            GameGlobals.Instance = this;
            
            MultiTypeCache.Init();

            Localization = new LocalizationManager();
            SaveGame = new SaveGameManager();
            Textures = GetComponent<TextureManager>();
            Font = GetComponent<FontManager>();
            Loading = _loadingManager;
            StaticCache = new StaticCacheManager();
            Vobs = _vobManager;
            Npcs = new NpcManager();
            NpcAi = new NpcAiManager();
            Animations = new AnimationManager();
            VobMeshCulling = _vobMeshCullingService;
            NpcMeshCulling = _npcMeshCullingService;
            Lights = new StationaryLightsManager();
            Player = new PlayerManager(DeveloperConfig);
            Marvin = new MarvinManager();
            Time = _gameTimeService;
            Video = new VideoManager(DeveloperConfig);
            Sky = _skyManager;
            Story = new StoryManager(DeveloperConfig);
            Routines = new RoutineManager(DeveloperConfig);
            SpeechToText = _speechToTextService;

            ZenKit.Logger.Set(Config.Dev.ZenKitLogLevel, Logger.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.Dev.DirectMusicLogLevel, Logger.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init(Config.Root);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Call init function of BootstrapSceneManager directly as it kicks off cleaning up of further loaded scenes.
            SceneManager.GetActiveScene().GetComponentInChildren<BootstrapSceneManager>()!.Init();
        }

        /// <summary>
        /// Init when game starts and Controls are set already, but no Gothic game version is selected so far.
        /// </summary>
        public void InitPhase1()
        {
            _frameSkipper.Init();
            Loading.Init();
            Lights.Init();
            VobMeshCulling.Init();
            NpcMeshCulling.Init();
            _vobSoundCullingService.Init();
            Time.Init();
            Player.Init();
            Routines.Init();
        }

        /// <summary>
        /// Once Gothic version is selected, we can now initialize remaining managers.
        /// </summary>
        public void InitPhase2(GameVersion version)
        {
            var watch = Stopwatch.StartNew();

            Config.LoadGothicInis(version);
            GameContext.SetGameVersionContext(version);

            var gothicRootPath = GameContext.GameVersionAdapter.RootPath;

            // Otherwise, continue loading Gothic.
            Logger.Log($"Initializing Gothic installation at: {gothicRootPath}", LogCat.Loading);
            ResourceLoader.Init(gothicRootPath);

            _musicService.Init();
            StaticCache.Init(DeveloperConfig);
            Textures.Init();
            Vobs.Init(this);
            Npcs.Init(this);

            Bootstrapper.Boot();
            SpeechToText.Init(); // Init after language set.

            GlobalEventDispatcher.LevelChangeTriggered.AddListener((world, spawn) =>
            {
                Player.LastLevelChangeTriggerVobName = spawn;
                LoadWorld(world, SaveGameManager.SlotId.WorldChangeOnly, SceneManager.GetActiveScene().name);
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
        public void LoadWorld(string worldName, SaveGameManager.SlotId saveGameId, string sceneToUnload = null)
        {
            // We need to add .zen as early as possible as all related data needs the file ending.
            worldName += worldName.EndsWithIgnoreCase(".zen") ? "" : ".zen";

            // Pre-load ZenKit save game data now. Can be reused by LoadingSceneManager later.
            if (saveGameId == SaveGameManager.SlotId.NewGame)
            {
                SaveGame.LoadNewGame();
            }
            else if (saveGameId > 0)
            {
                SaveGame.LoadSavedGame(saveGameId);
            }
            else
            {
                // If we have saveGameId -1 that means to just change the world and keep the same data.
            }
            SaveGame.ChangeWorld(worldName);

            LoadScene(Constants.SceneLoading, sceneToUnload);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.Log($"Scene loaded: {scene.name}", LogCat.Loading);

            // Newly created scenes are always the ones which we set as main scenes (i.e. new GameObjects will spawn in here automatically)
            SceneManager.SetActiveScene(scene);

            var sceneManager = scene.GetComponentInChildren<ISceneManager>();
            if (sceneManager == null)
            {
                Logger.LogError($"{nameof(ISceneManager)} for scene >{scene.name}< not found. Game won't proceed as " +
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
            NpcMeshCulling = null;
            Lights = null;
            Time = null;
            Routines = null;
        }
    }
}
