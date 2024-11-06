using System.Collections;
using GUZ.Core;
using GUZ.Core.Caches;
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
        public LabSoundHandler LabSoundHandler;
        public LabVideoHandler LabVideoHandler;
        public LabNpcDialogHandler NpcDialogHandler;
        public LabInteractableHandler InteractableHandler;
        public LabLadderLabHandler LadderLabHandler;
        public LabVobItemHandler VobItemHandler;
        public LabNpcAnimationHandler LabNpcAnimationHandler;
        public LabLockHandler LabLockHandler;
        private MusicManager _gameMusicManager;
        private VideoManager _videoManager;
        private RoutineManager _npcRoutineManager;
        private GameSettings _settings;
        private TextureManager _textureManager;
        private FontManager _fontManager;

        public GameSettings Settings => _settings;
        public SaveGameManager SaveGame => null;
        public LoadingManager Loading => null;
        public PlayerManager Player => null;
        public SkyManager Sky => null;
        public GameTime Time => null;
        public RoutineManager Routines => _npcRoutineManager;
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
            StartCoroutine(BootLab());
        }

        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private IEnumerator BootLab()
        {
            yield return new WaitForSeconds(0.5f);

            InitManager();
            InitLab();
        }

        private void InitManager()
        {
            GameGlobals.Instance = this;

            Logger.Set(Config.ZenKitLogLevel, Logging.OnZenKitLogMessage);
            DirectMusic.Logger.Set(Config.DirectMusicLogLevel, Logging.OnDirectMusicLogMessage);
            _settings = GameSettings.Load(Config.GameVersion);
            _textureManager = GetComponent<TextureManager>();
            _fontManager = GetComponent<FontManager>();
            _npcRoutineManager = new RoutineManager(Config);
            _gameMusicManager = new MusicManager(Config);
            _videoManager = new VideoManager(Config);

            ResourceLoader.Init(_settings.Gothic1Path);

            GameContext.SetControlContext(Config.GameControls);
            GameContext.SetGameVersionContext(Config.GameVersion);

            _gameMusicManager.Init();
            _npcRoutineManager.Init();
            _videoManager.Init();
            _textureManager.Init();
        }

        private void InitLab()
        {

            GuzBootstrapper.BootGothicUnZeNity(Config, _settings.Gothic1Path);

            GameContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            GameContext.InteractionAdapter.CreateVRDeviceSimulator();
            // TODO - Broken. Fix before use.
            // NpcHelper.CacheHero();

            LabNpcAnimationHandler.Bootstrap();
            LabMusicHandler.Bootstrap();
            LabSoundHandler.Bootstrap();
            LabVideoHandler.Bootstrap();
            // TODO - Broken. Fix before use.
            // NpcDialogHandler.Bootstrap();
            InteractableHandler.Bootstrap();
            LadderLabHandler.Bootstrap();
            VobItemHandler.Bootstrap();
            LabLockHandler.Bootstrap();

            GameContext.InteractionAdapter.InitUIInteraction(); // For (e.g.) QuestLog to enable hand pointer.
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
