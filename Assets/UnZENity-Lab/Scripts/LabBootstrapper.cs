using System.Collections;
using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Vm;
using GUZ.Core.World;
using GUZ.Lab.Handler;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private MusicManager _gameMusicManager;
        private VideoManager _videoManager;
        private RoutineManager _npcRoutineManager;
        private GameSettings _settings;
        private TextureManager _textureManager;
        private FontManager _fontManager;

        public GameSettings Settings => _settings;
        public LoadingManager Loading => null;
        public GltManager Glt => null;
        public PlayerManager Player => null;
        public SkyManager Sky => null;
        public GameTime Time => null;
        public RoutineManager Routines => _npcRoutineManager;
        public TextureManager Textures => _textureManager;
        public FontManager Font => _fontManager;
        public VobManager Vobs => null;
        public StationaryLightsManager Lights => null;
        public TextureArrayManager TextureArray => null;
        public VobMeshCullingManager VobMeshCulling => null;
        public NpcMeshCullingManager NpcMeshCulling => null;
        public VobSoundCullingManager SoundCulling => null;
        public StoryManager Story => null;
        public VideoManager Video => _videoManager;


        private void Awake()
        {
            GameGlobals.Instance = this;

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

            StartCoroutine(BootLab());
        }

        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private IEnumerator BootLab()
        {
            yield return new WaitForSeconds(0.5f);

            var settings = _settings;
            GuzBootstrapper.BootGothicUnZeNity(Config, settings.Gothic1Path);

            var playerGo = GameContext.InteractionAdapter.CreatePlayerController(SceneManager.GetActiveScene());
            GameContext.InteractionAdapter.CreateVRDeviceSimulator();
            NpcHelper.CacheHero();

            LabNpcAnimationHandler.Bootstrap();
            LabMusicHandler.Bootstrap();
            LabVideoHandler.Bootstrap();
            NpcDialogHandler.Bootstrap();
            InteractableHandler.Bootstrap();
            LadderLabHandler.Bootstrap();
            VobItemHandler.Bootstrap();
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
