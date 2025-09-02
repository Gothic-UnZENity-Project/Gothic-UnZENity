using System;
using System.Globalization;
using System.Threading.Tasks;
using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Models.Config;
using GUZ.Core.Npc;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Culling;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.Lab.Handler;
using GUZ.Manager;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Lab
{
    [RequireComponent(typeof(TextureManager), typeof(FontManager))]
    public class LabBootstrapper : MonoBehaviour, IGlobalDataProvider, ICoroutineManager
    {
        public DeveloperConfig DeveloperConfig;

        public LabMusicHandler LabMusicHandler;
        public LabSoundHandler LabSoundHandler;
        public LabVideoHandler LabVideoHandler;
        public LabNpcDialogHandler NpcDialogHandler;
        public LabInteractableHandler InteractableHandler;
        public LabLadderLabHandler LadderLabHandler;
        public LabVobItemHandler VobItemHandler;
        public LabNpcAnimationHandler LabNpcAnimationHandler;
        public LabLockHandler LabLockHandler;

        private LocalizationManager _localizationManager;
        private VideoManager _videoManager;
        private RoutineManager _npcRoutineManager;
        private SaveGameManager _save;
        private StaticCacheManager _staticCacheManager;
        private TextureManager _textureManager;
        private FontManager _fontManager;
        private StoryManager _story;
        private VobManager _vobManager;
        private NpcManager _npcManager;
        private MarvinManager _marvinManager;

        public LocalizationManager Localization => _localizationManager;
        public SaveGameManager SaveGame => _save;
        public LoadingManager Loading => null;
        public StaticCacheManager StaticCache => _staticCacheManager;
        public PlayerManager Player => null;
        public MarvinManager Marvin => _marvinManager;
        public SkyService Sky => null;
        public GameTimeService Time => _gameTimeService;
        public AudioService Audio => Audio;
        public RoutineManager Routines => _npcRoutineManager;
        public TextureManager Textures => _textureManager;
        public FontManager Font => _fontManager;
        public StationaryLightsManager Lights => null;
        public VobManager Vobs => _vobManager;
        public NpcManager Npcs => _npcManager;
        public NpcAiManager NpcAi => null;
        public VobMeshCullingService VobMeshCulling => null;
        public NpcMeshCullingService NpcMeshCulling => null;
        public StoryManager Story => _story;
        public VideoManager Video => _videoManager;
        public SpeechToTextService SpeechToText => null;


        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly GameTimeService _gameTimeService;
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly ContextGameVersionService _contextGameVersionService;
        [Inject] private readonly MeshService _meshService;
        [Inject] private readonly TextureCacheService _textureCacheService;


        private void Awake()
        {
            GameContext.IsLab = true;
            
            // We need to set culture to this, otherwise e.g. polish numbers aren't parsed correct.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            BootLab().AwaitAndLog();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        /// <summary>
        /// It's easiest to wait for Start() to initialize all the MonoBehaviours first.
        /// </summary>
        private async Task BootLab()
        {
            // We need to wait for e.g., 0.5 seconds to ensure, that Unity properly set up all MonoBehavior components and their properties.
            await Task.Delay(500);

            InitManager();
            await InitLab().AwaitAndLog();
        }

        private void InitManager()
        {
            GameGlobals.Instance = this;

            _configService.LoadRootJson();
            _configService.SetDeveloperConfig(DeveloperConfig);
            _configService.LoadGothicInis(GameVersion.Gothic1);

            ZenKit.Logger.Set(_configService.Dev.ZenKitLogLevel, Logger.OnZenKitLogMessage);
            DirectMusic.Logger.Set(_configService.Dev.DirectMusicLogLevel, Logger.OnDirectMusicLogMessage);
            _localizationManager = new LocalizationManager();
            _save = new SaveGameManager();
            _staticCacheManager = new StaticCacheManager();
            _story = new StoryManager();
            _textureManager = GetComponent<TextureManager>();
            _fontManager = GetComponent<FontManager>();
            _npcRoutineManager = new RoutineManager(_configService.Dev);
            _videoManager = new VideoManager();
            _npcManager = new NpcManager();
            _vobManager = new VobManager();
            _marvinManager = new MarvinManager();

            ResourceLoader.Init(_configService.Root.Gothic1Path);

            // In lab, we can safely say: VR only!
            GameContext.ContextInteractionService = _contextInteractionService;
            GameContext.ContextGameVersionService = _contextGameVersionService;
            GlobalEventDispatcher.RegisterControlsService.Invoke(DeveloperConfig.GameControls);
            GlobalEventDispatcher.RegisterGameVersionService.Invoke(DeveloperConfig.GameVersion);

            _audioService.InitMusic();
            _npcRoutineManager.Init();
            _staticCacheManager.Init(_configService.Dev);
            _textureManager.Init();
            _npcManager.Init();
            _vobManager.Init();

            _videoManager.InitVideos();
            _save.LoadNewGame();
        }

        private async Task InitLab()
        {

            _contextInteractionService.SetupPlayerController(DeveloperConfig);

            // TODO - Broken. Fix before use.
            // NpcHelper.CacheHero();

            Bootstrapper.Boot();

            if (!_staticCacheManager.DoGlobalCacheFilesExist())
            {
                Logger.LogErrorEditor("Please load game once to create global cache first!", LogCat.Debug);
                throw new SystemException("Please load game once to create global cache first!");
            }
            await _staticCacheManager.LoadGlobalCache().AwaitAndLog();
            await _meshService.CreateTextureArray().AwaitAndLog();

            LabNpcAnimationHandler.Bootstrap();
            LabMusicHandler.Bootstrap();
            LabSoundHandler.Bootstrap();
            LabVideoHandler.Bootstrap();
            NpcDialogHandler.Bootstrap();
            InteractableHandler.Bootstrap();
            LadderLabHandler.Bootstrap();
            VobItemHandler.Bootstrap();
            LabLockHandler.Bootstrap();

            GameContext.ContextInteractionService.InitUIInteraction(); // For (e.g.) QuestLog to enable hand pointer.
            BootstrapPlayer();
        }

        private void BootstrapPlayer()
        {
            // Add Missions and Notes
            {
                string topic;
                topic = "0 - The Lost Artifact";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "I venture into the ruins of an ancient temple to recover a powerful artifact believed to be hidden within.");
                    _story.ExtLogAddEntry(topic, "I must beware of the traps and guardians that protect it.");
                    _story.ExtLogAddEntry(topic, "As I explore, I uncover ancient writings that hint at the artifact's true power.");
                    _story.ExtLogAddEntry(topic, "I encounter rival treasure hunters who will stop at nothing to claim the artifact for themselves.");
                    _story.ExtLogAddEntry(topic, "I must solve intricate puzzles that guard the inner sanctum of the temple.");
                    _story.ExtLogAddEntry(topic, "The deeper I go, the more I feel the weight of history pressing down on me.");
                    _story.ExtLogAddEntry(topic, "Each encounter reveals different perspectives on what heroism truly means.");
                    _story.ExtLogAddEntry(topic, "Ultimately, my search becomes not just about finding one person but rediscovering what it means to be brave.");
                    _story.ExtLogAddEntry(topic, "In confronting challenges along the way, I may find my own path as a hero in this world.");
                    _story.ExtLogAddEntry(topic, "I aim to uncover the truth behind these ghostly echoes and their connection to the camp's dark history.");
                    _story.ExtLogAddEntry(topic, "I speak with locals who share chilling tales of past events that haunt the area.");
                    _story.ExtLogAddEntry(topic, "As night falls, I set up camp to witness the phenomena firsthand.");
                    _story.ExtLogAddEntry(topic, "I discover a hidden chamber that holds secrets long forgotten by time.");
                    _story.ExtLogAddEntry(topic, "The whispers grow louder, leading me to confront a lingering spirit seeking closure.");
                    _story.ExtLogAddEntry(topic, "I face moral dilemmas when encountering creatures that guard these rare items.");
                    _story.ExtLogAddEntry(topic, "My choices could either empower him or lead to disastrous consequences for the camp.");
                    _story.ExtLogAddEntry(topic, "Ultimately, I must weigh my own desires against the potential fallout of his spell.");
                }

                topic = "1 - Echoes of the Past";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "I investigate strange occurrences in the Old Camp, where villagers report hearing whispers at night.");
                }

                topic = "2 - The Price of Power";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "A local mage seeks my help in gathering rare ingredients for a spell that promises great power.");
                    _story.ExtLogAddEntry(topic, "I must decide whether to assist him or expose his dangerous ambitions to the camp leaders.");
                    _story.ExtLogAddEntry(topic, "As I gather ingredients, I learn about the mage's troubled past and his obsession with power.");
                }

                topic = "3 - Beneath the Surface";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "I explore the depths of a long-abandoned mine rumored to be haunted.");
                    _story.ExtLogAddEntry(topic, "I need to discover what lies beneath and confront the malevolent force that has kept it hidden for so long.");
                    _story.ExtLogAddEntry(topic, "As I descend, I encounter remnants of miners who vanished without a trace.");
                    _story.ExtLogAddEntry(topic, "Strange noises echo through the tunnels, heightening my sense of dread.");
                    _story.ExtLogAddEntry(topic, "I find clues suggesting that dark rituals were once performed here.");
                    _story.ExtLogAddEntry(topic, "Confronting whatever lurks in the shadows will test my courage and resolve.");
                }

                topic = "4 - A Thief's Redemption";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "I help a reformed thief prove his innocence after being accused of stealing from the camp's treasury.");
                    _story.ExtLogAddEntry(topic, "I gather evidence and confront those who seek to frame him.");
                    _story.ExtLogAddEntry(topic, "My investigation leads me through back alleys and hidden corners of the camp.");
                    _story.ExtLogAddEntry(topic, "Along the way, I meet other characters who have their own motives regarding the theft.");
                    _story.ExtLogAddEntry(topic, "Each piece of evidence brings me closer to uncovering a larger conspiracy at play.");
                    _story.ExtLogAddEntry(topic, "In a final confrontation, I must decide whether justice or mercy prevails.");
                }

                topic = "5 - The Beast Within";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "A series of brutal attacks on livestock has left the villagers terrified.");
                    _story.ExtLogAddEntry(topic, "I hunt down the creature responsible and uncover whether it's merely a beast or something more sinister.");
                    _story.ExtLogAddEntry(topic, "Villagers share their encounters with fear in their eyes, painting a vivid picture of terror.");
                    _story.ExtLogAddEntry(topic, "As I track the creature, I discover signs that suggest it may be more than just an animal attack.");
                    _story.ExtLogAddEntry(topic, "Clues lead me into dark woods where shadows seem to move on their own.");
                    _story.ExtLogAddEntry(topic, "When I finally confront the beast, its true nature reveals a tragic story that challenges my perception.");
                }

                topic = "6 - Allies in Shadows";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, "I join forces with a secretive faction operating in the shadows.");
                    _story.ExtLogAddEntry(topic, "I complete tasks that test my loyalty and skills, ultimately deciding whether to support their cause or betray them.");
                    _story.ExtLogAddEntry(topic, "The faction’s goals are shrouded in mystery, making me question their true intentions.");
                    _story.ExtLogAddEntry(topic, "As missions unfold, I uncover secrets about powerful figures within the camp that they aim to undermine.");
                    _story.ExtLogAddEntry(topic, "My actions can either strengthen or weaken their influence in our world.");
                    _story.ExtLogAddEntry(topic, "In a climactic moment, I'm faced with a choice that could change everything for me and those around me.");
                }

                for (int i = 7; i < 40; i++)
                {
                    topic = $"{i} - Mission";
                    _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _story.ExtLogAddEntry(topic, $"This is a placeholder for mission {i}.");
                }

                topic = "1 - Trial by Fire";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Failure);
                    _story.ExtLogAddEntry(topic, "I participate in a series of trials set by the camp's leaders to prove my worth as a warrior.");
                    _story.ExtLogAddEntry(topic, "I must overcome challenges that will test my combat skills and strategic thinking.");
                    _story.ExtLogAddEntry(topic, "Each trial is designed not only for strength but also for wisdom and resilience under pressure.");
                    _story.ExtLogAddEntry(topic, "Fellow challengers become both allies and rivals as we compete for honor and recognition.");
                    _story.ExtLogAddEntry(topic, "The outcome of these trials will determine my standing within the camp’s hierarchy.");
                    _story.ExtLogAddEntry(topic, "Ultimately, my performance could lead to unexpected alliances or bitter enmities.");
                }

                topic = "2 - The Forgotten Heirloom";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Failure);
                    _story.ExtLogAddEntry(topic, "I assist an elderly woman in recovering a family heirloom lost during her youth.");
                    _story.ExtLogAddEntry(topic, "The journey takes me through treacherous terrain and reveals long-buried family secrets.");
                    _story.ExtLogAddEntry(topic, "Clues lead us to forgotten places tied to her family's history, each revealing more about her past.");
                    _story.ExtLogAddEntry(topic, "Along our journey, we encounter obstacles that test our resolve and resourcefulness together.");
                    _story.ExtLogAddEntry(topic, "The heirloom itself holds significance beyond its material value; it symbolizes lost connections and memories.");
                    _story.ExtLogAddEntry(topic, "In recovering it, we not only restore her legacy but also heal old wounds.");
                }

                topic = "1 - The Call of Adventure";
                _story.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _story.ExtLogSetTopicStatus(topic, SaveTopicStatus.Obsolete);
                    _story.ExtLogAddEntry(topic, "I embark on a quest to find a legendary hero who vanished years ago.");
                    _story.ExtLogAddEntry(topic, "Following clues across various camps leads me into uncharted territories filled with danger.");
                    _story.ExtLogAddEntry(topic, "Along my journey, stories about this hero inspire hope among those who have lost faith.");
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
            _textureCacheService.Dispose();
            MultiTypeCache.Dispose();
            MorphMeshCache.Dispose();
        }
    }
}
