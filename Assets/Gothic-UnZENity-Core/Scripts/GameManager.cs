using GUZ.Core.Caches;
using GUZ.Core.Debugging;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using GUZ.Core.World;
using UnityEngine;
using UnityEngine.Serialization;
using Logger = ZenKit.Logger;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class GameManager : MonoBehaviour, ICoroutineManager, IGlobalDataProvider
    {
        [field: SerializeField] public GameConfiguration Config { get; set; }

        [FormerlySerializedAs("xrInteractionManager")]
        public GameObject XRInteractionManager;

        [FormerlySerializedAs("invalidInstallationPathMessage")]
        public GameObject InvalidInstallationPathMessage;

        private BarrierManager _barrierManager;
        private XRPlayerManager _xrPlayerManager;
        private XRDeviceSimulatorManager _xrSimulatorManager;
        private MusicManager _gameMusicManager;
        private LoadingManager _gameLoadingManager;
        private FileLoggingHandler _fileLoggingHandler;

        public GameSettings Settings { get; private set; }
        private bool _isInitialised;

        public SkyManager Sky { get; private set; }

        public GameTime Time { get; private set; }

        public RoutineManager Routines { get; private set; }

        public TextureManager Textures { get; private set; }

        public GuzSceneManager Scene { get; private set; }

        public FontManager Font { get; private set; }

        public StationaryLightsManager Lights { get; private set; }

        public VobMeshCullingManager VOBMeshCulling { get; private set; }

        public NPCMeshCullingManager NPCMeshCulling { get; private set; }

        public VobSoundCullingManager SoundCulling { get; private set; }

        // ReSharper disable Unity.PerformanceAnalysis
        private void Load()
        {
            // If the Gothic installation directory is not set, show an error message and exit.
            if (!Settings.CheckIfGothic1InstallationExists())
            {
                InvalidInstallationPathMessage.SetActive(true);
                return;
            }

            // Otherwise, continue loading Gothic.
            ResourceLoader.Init(Settings.GothicIPath);

            _gameMusicManager.Init();

            GuzBootstrapper.BootGothicUnZeNity(Config, Settings.GothicIPath, Settings.GothicILanguage);
            Scene.LoadStartupScenes();

            if (Config.EnableBarrierVisual)
            {
                GlobalEventDispatcher.WorldSceneLoaded.AddListener(() => { _barrierManager.CreateBarrier(); });
            }
        }

        private void Awake()
        {
            GameGlobals.Instance = this;
            LookupCache.Init();

            Textures = GetComponent<TextureManager>();
            Font = GetComponent<FontManager>();
            Settings = GameSettings.Load();
            _fileLoggingHandler = new FileLoggingHandler(Settings);
            _gameLoadingManager = new LoadingManager();
            VOBMeshCulling = new VobMeshCullingManager(Config, this);
            NPCMeshCulling = new NPCMeshCullingManager(Config, this);
            SoundCulling = new VobSoundCullingManager(Config);
            _barrierManager = new BarrierManager(Config);
            Lights = new StationaryLightsManager();
            _xrPlayerManager = new XRPlayerManager(Config);
            _xrSimulatorManager = new XRDeviceSimulatorManager(Config);
            Time = new GameTime(Config, this);
            Sky = new SkyManager(Config, Time, Settings);
            _gameMusicManager = new MusicManager(Config);
            Scene = new GuzSceneManager(Config, _gameLoadingManager, XRInteractionManager);
            Routines = new RoutineManager(Config);
        }

        private void Start()
        {
            Logger.Set(Config.ZenKitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.DirectMusicLogLevel, Logging.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init();
            _gameLoadingManager.Init();
            VOBMeshCulling.Init();
            NPCMeshCulling.Init();
            SoundCulling.Init();
            Time.Init();
            Sky.Init();
            _xrPlayerManager.Init();
            _xrSimulatorManager.Init();
            Scene.Init();
            Routines.Init();

            // Just in case we forgot to disable it in scene view. ;-)
            InvalidInstallationPathMessage.SetActive(false);
        }

        private void Update()
        {
            Scene.Update();

            if (_isInitialised)
            {
                return;
            }

            _isInitialised = true;
            Load();
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
            VOBMeshCulling?.OnDrawGizmos();
        }

        public void OnDestroy()
        {
            VOBMeshCulling.Destroy();
            NPCMeshCulling.Destroy();
            SoundCulling.Destroy();
            _fileLoggingHandler.Destroy();

            Settings = null;
            _gameLoadingManager = null;
            VOBMeshCulling = null;
            NPCMeshCulling = null;
            SoundCulling = null;
            _barrierManager = null;
            Lights = null;
            _xrSimulatorManager = null;
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
