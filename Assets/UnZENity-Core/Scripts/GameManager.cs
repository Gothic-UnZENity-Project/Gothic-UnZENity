using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Debugging;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using GUZ.Core.World;
using UnityEngine;
using Logger = ZenKit.Logger;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class GameManager : MonoBehaviour, ICoroutineManager, IGlobalDataProvider
    {
        [field: SerializeField] public GameConfiguration Config { get; set; }

        public GameObject XRInteractionManager;

        public GameObject InvalidInstallationPathMessage;

        private FileLoggingHandler _fileLoggingHandler;
        private BarrierManager _barrierManager;
        private PlayerManager _playerManager;
        private VRDeviceSimulatorManager _vrSimulatorManager;
        private MusicManager _gameMusicManager;
        private LoadingManager _gameLoadingManager;

        public GameSettings Settings { get; private set; }

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
        

        private void Awake()
        {
            _fileLoggingHandler = new FileLoggingHandler();

            GameGlobals.Instance = this;
            
            // Set Context as early as possible to ensure everything else boots based on the activated modules.
            GuzContext.SetContext(Config.GameControls, Config.GameVersion);
            Settings = GameSettings.Load(Config.GameVersion);
            
            MultiTypeCache.Init();

            Textures = GetComponent<TextureManager>();
            Font = GetComponent<FontManager>();
            _gameLoadingManager = new LoadingManager();
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
            Scene = new GuzSceneManager(Config, _gameLoadingManager, XRInteractionManager);
            Story = new StoryManager(Config);
            Routines = new RoutineManager(Config);
        }

        private void Start()
        {
            Logger.Set(Config.ZenKitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.DirectMusicLogLevel, Logging.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init(Settings);
            _gameLoadingManager.Init();
            VobMeshCulling.Init();
            NpcMeshCulling.Init();
            SoundCulling.Init();
            Time.Init();
            Video.Init();
            Sky.Init();
            _playerManager.Init();
            _vrSimulatorManager.Init();
            Scene.Init();
            Routines.Init();

            // Just in case we forgot to disable it in scene view. ;-)
            InvalidInstallationPathMessage.SetActive(false);

            Load(GuzContext.GameVersionAdapter.RootPath);
        }
        
        private void Load(string gothicRootPath)
        {
            // If the Gothic installation directory is not set, show an error message and exit.
            if (!Settings.CheckIfGothicInstallationExists(gothicRootPath))
            {
                InvalidInstallationPathMessage.SetActive(true);
                return;
            }

            // Otherwise, continue loading Gothic.
            
            ResourceLoader.Init(gothicRootPath);

            _gameMusicManager.Init();

            GuzBootstrapper.BootGothicUnZeNity(Config, gothicRootPath);
            Scene.LoadStartupScenes();

            if (Config.EnableBarrierVisual)
            {
                GlobalEventDispatcher.WorldSceneLoaded.AddListener(() => { _barrierManager.CreateBarrier(); });
            }
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
            _gameLoadingManager = null;
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
