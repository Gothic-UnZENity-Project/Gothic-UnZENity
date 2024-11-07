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
using ZenKit;
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
        private SaveGameManager _save;
        private TextureManager _textureManager;
        private FontManager _fontManager;
        private StoryManager _story;

        public GameSettings Settings => _settings;
        public SaveGameManager SaveGame => _save;
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
        public StoryManager Story => _story;
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
            _save = new SaveGameManager();
            _story = new StoryManager(Config);
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
            _save.LoadNewGame();
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
            BootstrapPlayer();
        }

        private void BootstrapPlayer()
        {
            // Add Missions and Notes
            {
                _story.ExtLogCreateTopic("Mission number 1", SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus("Mission number 1", SaveTopicStatus.Running);
                    _story.ExtLogAddEntry("Mission number 1", "Mission number 1 entry 1");
                    _story.ExtLogAddEntry("Mission number 1", "Mission number 1 entry 2");
                    _story.ExtLogAddEntry("Mission number 1", "Mission number 1 entry 3");
                }
                _story.ExtLogCreateTopic("Mission number 2", SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus("Mission number 2", SaveTopicStatus.Running);
                    _story.ExtLogAddEntry("Mission number 2", "Mission number 2 entry 1");
                    _story.ExtLogAddEntry("Mission number 2", "Mission number 2 entry 2");
                    _story.ExtLogAddEntry("Mission number 2", "Mission number 2 entry 3");
                }
                _story.ExtLogCreateTopic("Mission number 3", SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus("Mission number 3", SaveTopicStatus.Running);
                    _story.ExtLogAddEntry("Mission number 3", "Mission number 3 entry 1");
                    _story.ExtLogAddEntry("Mission number 3", "Mission number 3 entry 2");
                    _story.ExtLogAddEntry("Mission number 3", "Mission number 3 entry 3");
                }
                _story.ExtLogCreateTopic("Mission number 4", SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus("Mission number 4", SaveTopicStatus.Failure);
                    _story.ExtLogAddEntry("Mission number 4", "Mission number 4 entry 1");
                    _story.ExtLogAddEntry("Mission number 4", "Mission number 4 entry 2");
                    _story.ExtLogAddEntry("Mission number 4", "Mission number 4 entry 3");
                }
                _story.ExtLogCreateTopic("Mission number 5", SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus("Mission number 5", SaveTopicStatus.Failure);
                    _story.ExtLogAddEntry("Mission number 5", "Mission number 5 entry 1");
                    _story.ExtLogAddEntry("Mission number 5", "Mission number 5 entry 2");
                    _story.ExtLogAddEntry("Mission number 5", "Mission number 5 entry 3");
                }
                _story.ExtLogCreateTopic("Note number 1", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 1", "Note number 1 entry 1");
                    _story.ExtLogAddEntry("Note number 1", "Note number 1 entry 2");
                    _story.ExtLogAddEntry("Note number 1", "Note number 1 entry 3");
                    _story.ExtLogAddEntry("Note number 1", "Note number 1 entry 4");
                    _story.ExtLogAddEntry("Note number 1", "Note number 1 entry 5");
                }
                _story.ExtLogCreateTopic("Note number 2", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 2", "Note number 2 entry 1");
                    _story.ExtLogAddEntry("Note number 2", "Note number 2 entry 2");
                    _story.ExtLogAddEntry("Note number 2", "Note number 2 entry 3");
                }
                _story.ExtLogCreateTopic("Note number 3", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 3", "Note number 3 entry 1");
                    _story.ExtLogAddEntry("Note number 3", "Note number 3 entry 2");
                }
                _story.ExtLogCreateTopic("Note number 4", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 4", "Note number 4 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 5", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 5", "Note number 5 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 6", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 6", "Note number 6 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 7", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 7", "Note number 7 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 8", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 8", "Note number 8 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 9", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 9", "Note number 9 entry 1");
                }
                _story.ExtLogCreateTopic("Note number 10", SaveTopicSection.Notes);
                {
                    _story.ExtLogAddEntry("Note number 10", "Note number 10 entry 1");
                }
            }
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
