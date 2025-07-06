using System.Diagnostics;
using System.Globalization;
using GUZ.Core.Animations;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Scenes;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Npc;
using GUZ.Core.UnZENity_Core.Scripts.Manager;
using GUZ.Core.Util;
using GUZ.Core.World;
using GUZ.Manager;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager), typeof(FrameSkipper))]
    public class GameManager : SingletonBehaviour<GameManager>, ICoroutineManager, IGlobalDataProvider
    {
        public DeveloperConfig DeveloperConfig;

        private FileLoggingHandler _fileLoggingHandler;
        private FrameSkipper _frameSkipper;
        private BarrierManager _barrierManager;

        public ConfigManager Config { get; private set; }
        public LocalizationManager Localization { get; private set; }

        public SaveGameManager SaveGame { get; private set; }

        public LoadingManager Loading { get; private set; }
        public StaticCacheManager StaticCache { get; private set; }

        public PlayerManager Player { get; private set; }
        public SkyManager Sky { get; private set; }

        public GameTime Time { get; private set; }
        
        public VideoManager Video { get; private set; }
        
        public MusicManager Music { get; private set; }

        public RoutineManager Routines { get; private set; }

        public TextureManager Textures { get; private set; }

        public StoryManager Story { get; private set; }

        public FontManager Font { get; private set; }

        public StationaryLightsManager Lights { get; private set; }

        public VobManager Vobs { get; private set; }
        public NpcManager Npcs { get; private set; }
        public NpcAiManager NpcAi { get; private set; }
        public AnimationManager Animations { get; private set; }
        public VobMeshCullingManager VobMeshCulling { get; private set; }
        public NpcMeshCullingManager NpcMeshCulling { get; private set; }
        public VobSoundCullingManager SoundCulling { get; private set; }
        public VoiceManager Voice { get; private set; }


        protected override void Awake()
        {
            base.Awake();

            GameContext.IsLab = false;
            
            // We need to set culture to this, otherwise e.g. polish numbers aren't parsed correct.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Config = new ConfigManager();
            Config.LoadRootJson();
            Config.SetDeveloperConfig(DeveloperConfig);

            _fileLoggingHandler = new FileLoggingHandler();
            _frameSkipper = GetComponent<FrameSkipper>();

            GameGlobals.Instance = this;
            
            // Set Context as early as possible to ensure everything else boots based on the activated modules.
            GameContext.SetControlContext(Config.Dev.GameControls);

            MultiTypeCache.Init();

            Localization = new LocalizationManager();
            SaveGame = new SaveGameManager();
            Textures = GetComponent<TextureManager>();
            Font = GetComponent<FontManager>();
            Loading = new LoadingManager();
            StaticCache = new StaticCacheManager();
            Vobs = new VobManager();
            Npcs = new NpcManager();
            NpcAi = new NpcAiManager();
            Animations = new AnimationManager();
            VobMeshCulling = new VobMeshCullingManager(DeveloperConfig, this);
            NpcMeshCulling = new NpcMeshCullingManager(DeveloperConfig);
            SoundCulling = new VobSoundCullingManager(DeveloperConfig);
            _barrierManager = new BarrierManager(DeveloperConfig);
            Lights = new StationaryLightsManager();
            Player = new PlayerManager(DeveloperConfig);
            Time = new GameTime(DeveloperConfig, this);
            Video = new VideoManager(DeveloperConfig);
            Sky = new SkyManager(DeveloperConfig, Time);
            Music = new MusicManager(DeveloperConfig);
            Story = new StoryManager(DeveloperConfig);
            Routines = new RoutineManager(DeveloperConfig);
            Voice = new VoiceManager();
        }

        private void Start()
        {
            InitPhase1();
        }

        /// <summary>
        /// Init when game starts and Controls are set already, but no Gothic game version is selected so far.
        /// </summary>
        private void InitPhase1()
        {
            // Call init function of BootstrapSceneManager directly as it kicks off cleaning up of further loaded scenes.
            SceneManager.GetActiveScene().GetComponentInChildren<BootstrapSceneManager>()!.Init();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            ZenKit.Logger.Set(Config.Dev.ZenKitLogLevel, Logger.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.Dev.DirectMusicLogLevel, Logger.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init(Config.Root);
            _frameSkipper.Init();
            Loading.Init();
            Lights.Init();
            VobMeshCulling.Init();
            NpcMeshCulling.Init();
            SoundCulling.Init();
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

            Music.Init();
            StaticCache.Init(DeveloperConfig);
            Textures.Init();
            Vobs.Init(this);
            Npcs.Init(this);

            Bootstrapper.Boot();
            Voice.Init(); // Init after language set.

            GlobalEventDispatcher.LevelChangeTriggered.AddListener((world, spawn) =>
            {
                Player.LastLevelChangeTriggerVobName = spawn;
                LoadWorld(world, SaveGameManager.SlotId.WorldChangeOnly, SceneManager.GetActiveScene().name);
            });

            watch.Log("Phase2 (mostly ZenKit) initialized in");
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

        private void Update()
        {
            NpcMeshCulling.Update();
            Loading.Update();
        }

        private void FixedUpdate()
        {
            _barrierManager.FixedUpdate();
        }

        private void LateUpdate()
        {
            Lights.LateUpdate();
        }

        private void OnValidate()
        {
            Sky?.OnValidate();
        }

        private void OnDrawGizmos()
        {
            VobMeshCulling?.OnDrawGizmos();
        }

        public void OnDestroy()
        {
            VobMeshCulling.Destroy();
            NpcMeshCulling.Destroy();
            SoundCulling.Destroy();
            _fileLoggingHandler.Destroy();

            Loading = null;
            VobMeshCulling = null;
            NpcMeshCulling = null;
            SoundCulling = null;
            _barrierManager = null;
            Lights = null;
            Time = null;
            Sky = null;
            Music = null;
            Routines = null;
        }

        private void OnApplicationQuit()
        {
            Bootstrapper.OnApplicationQuit();
            
            Voice?.Dispose();
        }
    }
}
