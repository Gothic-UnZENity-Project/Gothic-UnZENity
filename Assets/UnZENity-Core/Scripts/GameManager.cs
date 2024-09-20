using GUZ.Core.Caches;
using GUZ.Core.Debugging;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Scenes;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using GUZ.Core.World;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit;
using Debug = UnityEngine.Debug;
using Logger = ZenKit.Logger;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class GameManager : SingletonBehaviour<GameManager>, ICoroutineManager, IGlobalDataProvider
    {
        [field: SerializeField] public GameConfiguration Config { get; set; }

        public GameObject XRInteractionManager;

        private FileLoggingHandler _fileLoggingHandler;
        private BarrierManager _barrierManager;
        private PlayerManager _playerManager;
        private VRDeviceSimulatorManager _vrSimulatorManager;
        private MusicManager _gameMusicManager;

        public GameSettings Settings { get; private set; }
        
        public LoadingManager Loading { get; private set; }

        public SkyManager Sky { get; private set; }

        public GameTime Time { get; private set; }
        
        public VideoManager Video { get; private set; }

        public RoutineManager Routines { get; private set; }

        public TextureManager Textures { get; private set; }

        public GuzSceneManager Scene { get; private set; }

        public StoryManager Story { get; private set; }

        public FontManager Font { get; private set; }

        public StationaryLightsManager Lights { get; private set; }

        public VobMeshCullingManager VobMeshCulling { get; private set; }

        public NpcMeshCullingManager NpcMeshCulling { get; private set; }

        public VobSoundCullingManager SoundCulling { get; private set; }
        

        protected override void Awake()
        {
            base.Awake();

            _fileLoggingHandler = new FileLoggingHandler();

            GameGlobals.Instance = this;
            
            // Set Context as early as possible to ensure everything else boots based on the activated modules.
            GameContext.SetControlContext(Config.GameControls);

            Settings = GameSettings.Load(Config.GameVersion);
            
            MultiTypeCache.Init();

            Textures = GetComponent<TextureManager>();
            Font = GetComponent<FontManager>();
            Loading = new LoadingManager();
            VobMeshCulling = new VobMeshCullingManager(Config, this);
            NpcMeshCulling = new NpcMeshCullingManager(Config);
            SoundCulling = new VobSoundCullingManager(Config);
            _barrierManager = new BarrierManager(Config);
            Lights = new StationaryLightsManager();
            _playerManager = new PlayerManager(Config);
            _vrSimulatorManager = new VRDeviceSimulatorManager(Config);
            Time = new GameTime(Config, this);
            Video = new VideoManager(Config);
            Sky = new SkyManager(Config, Time, Settings);
            _gameMusicManager = new MusicManager(Config);
            Scene = new GuzSceneManager(Config, Loading, XRInteractionManager, Settings);
            Story = new StoryManager(Config);
            Routines = new RoutineManager(Config);
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

            Logger.Set(Config.ZenKitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.DirectMusicLogLevel, Logging.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init(Settings);
            Loading.Init();
            VobMeshCulling.Init();
            NpcMeshCulling.Init();
            SoundCulling.Init();
            Time.Init();
            Sky.Init();
            _playerManager.Init();
            _vrSimulatorManager.Init();
            // Scene.Init();
            Routines.Init();
        }

        /// <summary>
        /// Once Gothic version is selected, we can now initialize remaining managers.
        /// </summary>
        public void InitPhase2(GameVersion version)
        {
            GameContext.SetGameVersionContext(version);

            var gothicRootPath = GameContext.GameVersionAdapter.RootPath;

            // Otherwise, continue loading Gothic.
            Debug.Log($"Initializing Gothic installation at: {gothicRootPath}");
            ResourceLoader.Init(gothicRootPath);

            _gameMusicManager.Init();
            Textures.Init();
            Video.Init();

            GuzBootstrapper.BootGothicUnZeNity(Config, gothicRootPath);

            if (Config.EnableBarrierVisual)
            {
                GlobalEventDispatcher.WorldSceneLoaded.AddListener(() => { _barrierManager.CreateBarrier(); });
            }
        }

        public void LoadScene(string sceneName, string unloadScene = null)
        {
            if (unloadScene.NotNullOrEmpty())
            {
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName(unloadScene));
            }

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        public WorldSpawnInformation CurrentWorldSpawnInformation;

        public struct WorldSpawnInformation
        {
            public Vector3 PlayerStartPosition;
            public Quaternion PlayerStartRotation;
            public string StartVobAfterLoading;
        }

        /// <summary>
        /// saveGameId - 0==newGame (Gothic saves start with number 1)
        /// </summary>
        public void LoadWorld(string worldName, int saveGameId, string sceneToUnload = null)
        {
            // Pre-load ZenKit savegame data now. Can be reused by LoadingSceneManager later.
            if (saveGameId < 1)
            {
                SaveGameManager.LoadNewGame();
            }
            else
            {
                SaveGameManager.LoadSavedGame(saveGameId);
            }
            SaveGameManager.ChangeWorld(worldName);

            LoadScene(Constants.SceneLoading, sceneToUnload);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}");

            // Newly created scenes are always the ones which we set as main scenes (i.e. new GameObjects will spawn in here automatically)
            SceneManager.SetActiveScene(scene);

            var sceneManager = scene.GetComponentInChildren<ISceneManager>();
            if (sceneManager == null)
            {
                Debug.LogError($"{nameof(ISceneManager)} for scene >{scene.name}< not found. Game won't proceed as bootstrapper for scene is invalid/non-existent.");
                return;
            }
            sceneManager.Init();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"Scene unloaded: {scene.name}");
        }

        private void Update()
        {
            NpcMeshCulling.Update();
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

            Settings = null;
            Loading = null;
            VobMeshCulling = null;
            NpcMeshCulling = null;
            SoundCulling = null;
            _barrierManager = null;
            Lights = null;
            _vrSimulatorManager = null;
            Time = null;
            Sky = null;
            _gameMusicManager = null;
            Scene = null;
            Routines = null;
        }

        private void OnApplicationQuit()
        {
            GuzBootstrapper.OnApplicationQuit();
        }
    }
}
