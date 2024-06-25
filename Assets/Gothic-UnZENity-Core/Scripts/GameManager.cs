using System;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;
using GUZ.Core;
using GUZ.Core.Debugging;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class GameManager : MonoBehaviour, ICoroutineManager, IGlobalDataProvider
    {
        [field: SerializeField] public GameConfiguration Config { get; set; }

        public GameObject xrInteractionManager;
        public GameObject invalidInstallationPathMessage;

        private BarrierManager _barrierManager;
        private XRDeviceSimulatorManager _xrSimulatorManager;
        private MusicManager _gameMusicManager;
        private LoadingManager _gameLoadingManager;
        private FileLoggingHandler _fileLoggingHandler;

        public GameSettings Settings { get; private set; }
        private bool _isInitialised = false;

        public SkyManager Sky { get; private set; }

        public GameTime Time { get; private set; }

        public RoutineManager Routines { get; private set; }

        public TextureManager Textures { get; private set; }

        public GUZSceneManager Scene { get; private set; }

        public FontManager Font { get; private set; }

        public StationaryLightsManager Lights { get; private set; }

        public VobMeshCullingManager MeshCulling { get; private set; }

        public VobSoundCullingManager SoundCulling { get; private set; }

        // ReSharper disable Unity.PerformanceAnalysis
        private void Load()
        {
            // If the Gothic installation directory is not set, show an error message and exit.
            if (!Settings.CheckIfGothic1InstallationExists())
            {
                invalidInstallationPathMessage.SetActive(true);
                return;
            }

            // Otherwise, continue loading Gothic.
            ResourceLoader.Init(Settings.GothicIPath);

            _gameMusicManager.Init();

            GUZBootstrapper.BootGothicUnZENity(Config, Settings.GothicIPath, Settings.GothicILanguage);
            Scene.LoadStartupScenes();

            if (Config.enableBarrierVisual)
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
            MeshCulling = new VobMeshCullingManager(Config, this);
            SoundCulling = new VobSoundCullingManager(Config);
            _barrierManager = new BarrierManager(Config);
            Lights = new StationaryLightsManager();
            _xrSimulatorManager = new XRDeviceSimulatorManager(Config);
            Time = new GameTime(Config, this);
            Sky = new SkyManager(Config, Time, Settings);
            _gameMusicManager = new MusicManager(Config);
            Scene = new GUZSceneManager(Config, _gameLoadingManager, xrInteractionManager);
            Routines = new RoutineManager(Config);
        }

        private void Start()
        {
            ZenKit.Logger.Set(Config.zenkitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.directMusicLogLevel, Logging.OnDirectMusicLogMessage);

            _fileLoggingHandler.Init();
            _gameLoadingManager.Init();
            MeshCulling.Init();
            SoundCulling.Init();
            Time.Init();
            Sky.Init();
            _xrSimulatorManager.Init();
            Scene.Init();
            Routines.Init();

            // Just in case we forgot to disable it in scene view. ;-)
            invalidInstallationPathMessage.SetActive(false);

            // Load the player controller upon MainMenu loaded
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(delegate
            {
                GUZContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            });
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
            MeshCulling?.OnDrawGizmos();
        }

        public void OnDestroy()
        {
            MeshCulling.Destroy();
            SoundCulling.Destroy();
            _fileLoggingHandler.Destroy();

            Settings = null;
            _gameLoadingManager = null;
            MeshCulling = null;
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
            GUZBootstrapper.OnApplicationQuit();
        }
    }
}