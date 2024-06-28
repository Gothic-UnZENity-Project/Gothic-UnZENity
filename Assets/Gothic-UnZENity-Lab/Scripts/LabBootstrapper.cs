using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Context;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Vm;
using GUZ.Core.World;
using GUZ.Core;
using GUZ.Core.Manager.Culling;
using GUZ.Lab.Handler;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Lab
{
	[RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class LabBootstrapper : MonoBehaviour, IGlobalDataProvider, ICoroutineManager
    {
        public GameConfiguration config;
        public LabMusicHandler labMusicHandler;
        public LabNpcDialogHandler npcDialogHandler;
        public LabLockableHandler lockableHandler;
        public LabLadderLabHandler ladderLabHandler;
        public LabVobHandAttachPointsLabHandler vobHandAttachPointsLabHandler;
        public LabNpcAnimationHandler labNpcAnimationHandler;

        private XRDeviceSimulatorManager _deviceSimulatorManager;
        private MusicManager _gameMusicManager;
        private RoutineManager _npcRoutineManager;
		private GameSettings _settings;
        private GUZSceneManager _sceneManager;
        private TextureManager _textureManager;
        private FontManager _fontManager;
        private bool _isBooted;

        public GameConfiguration Config => config;
        public GameSettings Settings => _settings;
        public SkyManager Sky => null;
		public GameTime Time => null;
		public RoutineManager Routines => _npcRoutineManager;
        public GUZSceneManager Scene => _sceneManager;
		public TextureManager Textures => _textureManager;
        public FontManager Font => _fontManager;
        public StationaryLightsManager Lights => null;
        public VobMeshCullingManager MeshCulling => null;
        public VobSoundCullingManager SoundCulling => null;


        private void Awake()
        {
            GameGlobals.Instance = this;
            
            _settings = GameSettings.Load();
            _textureManager = GetComponent<TextureManager>();
            _fontManager = GetComponent<FontManager>();
            _sceneManager = new GUZSceneManager(config, null, null);
            _deviceSimulatorManager = new XRDeviceSimulatorManager(config);
            _npcRoutineManager = new RoutineManager(config);
            _gameMusicManager = new MusicManager(config);
            
            ResourceLoader.Init(_settings.GothicIPath);
            _sceneManager.Init();
            _gameMusicManager.Init();
            _npcRoutineManager.Init();
        }

        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private void Update()
        {
            if (_isBooted)
                return;
            _isBooted = true;

            var settings = _settings;
            GUZBootstrapper.BootGothicUnZENity(config, settings.GothicIPath, settings.GothicILanguage);

            BootLab();

            labNpcAnimationHandler.Bootstrap();
            labMusicHandler.Bootstrap();
            npcDialogHandler.Bootstrap();
            lockableHandler.Bootstrap();
            ladderLabHandler.Bootstrap();
            vobHandAttachPointsLabHandler.Bootstrap();
        }

        private void BootLab()
        {
            var playerGo = GUZContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            _deviceSimulatorManager.AddXRDeviceSimulator();
            NpcHelper.CacheHero(playerGo);
        }

        private void OnDestroy()
        {
            GameData.Dispose();
            VmInstanceManager.Dispose();
            TextureCache.Dispose();
            LookupCache.Dispose();
            MorphMeshCache.Dispose();
        }
    }
}
