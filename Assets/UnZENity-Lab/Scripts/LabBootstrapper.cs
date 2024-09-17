using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.Core.World;
using GUZ.Lab.Handler;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = ZenKit.Logger;

namespace GUZ.Lab
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class LabBootstrapper : MonoBehaviour, IGlobalDataProvider, ICoroutineManager
    {
        [field: SerializeField]
        public GameConfiguration Config { get; private set; }

        public LabMusicHandler LabMusicHandler;
        public LabVideoHandler LabVideoHandler;
        public LabNpcDialogHandler NpcDialogHandler;
        public LabInteractableHandler InteractableHandler;
        public LabLadderLabHandler LadderLabHandler;
        public LabVobItemHandler VobItemHandler;
        public LabNpcAnimationHandler LabNpcAnimationHandler;
        public LabLockHandler LabLockHandler;
        
        private VRDeviceSimulatorManager _deviceSimulatorManager;
        private MusicManager _gameMusicManager;
        private VideoManager _videoManager;
        private RoutineManager _npcRoutineManager;
        private GameSettings _settings;
        private GuzSceneManager _sceneManager;
        private TextureManager _textureManager;
        private FontManager _fontManager;
        private bool _isBooted;

        public GameSettings Settings => _settings;
        public SkyManager Sky => null;
        public GameTime Time => null;
        public RoutineManager Routines => _npcRoutineManager;
        public GuzSceneManager Scene => _sceneManager;
        public TextureManager Textures => _textureManager;
        public FontManager Font => _fontManager;
        public StationaryLightsManager Lights => null;
        public VobMeshCullingManager VobMeshCulling => null;
        public NpcMeshCullingManager NpcMeshCulling => null;
        public VobSoundCullingManager SoundCulling => null;
        public StoryManager Story => null;
        public VideoManager Video => _videoManager;


        private void Awake()
        {
            GameGlobals.Instance = this;

            Logger.Set(Config.ZenKitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.DirectMusicLogLevel, Logging.OnDirectMusicLogMessage);
            
            _settings = GameSettings.Load();
            _textureManager = GetComponent<TextureManager>();
            _fontManager = GetComponent<FontManager>();
            _sceneManager = new GuzSceneManager(Config, null, null);
            _deviceSimulatorManager = new VRDeviceSimulatorManager(Config);
            _npcRoutineManager = new RoutineManager(Config);
            _gameMusicManager = new MusicManager(Config);
            _videoManager = new VideoManager(Config);

            ResourceLoader.Init(_settings.GothicIPath);
            _sceneManager.Init();
            _gameMusicManager.Init();
            _npcRoutineManager.Init();
            _videoManager.Init();
        }

        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private void Update()
        {
            if (_isBooted)
            {
                return;
            }

            _isBooted = true;

            var settings = _settings;
            GuzBootstrapper.BootGothicUnZeNity(Config, settings.GothicIPath);

            BootLab();

            LabNpcAnimationHandler.Bootstrap();
            LabMusicHandler.Bootstrap();
            LabVideoHandler.Bootstrap();
            NpcDialogHandler.Bootstrap();
            InteractableHandler.Bootstrap();
            LadderLabHandler.Bootstrap();
            VobItemHandler.Bootstrap();
            LabLockHandler.Bootstrap();
        }

        private void BootLab()
        {
            var playerGo = GuzContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            _deviceSimulatorManager.AddVRDeviceSimulator();
            NpcHelper.CacheHero(playerGo);
        }

        private void OnDestroy()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            MultiTypeCache.Dispose();
            MorphMeshCache.Dispose();
        }
    }
}
