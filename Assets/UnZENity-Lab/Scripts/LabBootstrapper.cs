using System;
using System.Globalization;
using System.Threading.Tasks;
using GUZ.Core;
using GUZ.Core.Domain;
using GUZ.Core.Extensions;
using GUZ.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Models.Config;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.StaticCache;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using GUZ.Lab.Handler;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.Lab
{
    public class LabBootstrapper : MonoBehaviour
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


        [Inject] private readonly VideoService _videoService;
        [Inject] private readonly SaveGameService _saveGameService;
        [Inject] private readonly StaticCacheService _staticCacheService;
        [Inject] private readonly StoryService _storyService;
        [Inject] private readonly VobService _vobService;
        [Inject] private readonly NpcService _npcService;
        [Inject] private readonly GameStateService _gameStateService;
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly GameTimeService _gameTimeService;
        [Inject] private readonly ContextInteractionService _contextInteractionService;
        [Inject] private readonly ContextGameVersionService _contextGameVersionService;
        [Inject] private readonly MeshService _meshService;
        [Inject] private readonly TextureService _textureService;
        [Inject] private readonly VmCacheService _vmCacheService;
        [Inject] private readonly TextureCacheService _textureCacheService;
        [Inject] private readonly MorphMeshCacheService _morphMeshCacheService;
        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly ResourceCacheService _resourceCacheService;
        [Inject] private readonly UnityMonoService _unityMonoService;
        [Inject] private readonly SkyService _skyService;

        private BootstrapDomain _bootstrapDomain;

        private void Awake()
        {
            _bootstrapDomain = new BootstrapDomain().Inject();
            _configService.SetDeveloperConfig(DeveloperConfig);

            // In lab, we can safely say: VR only!
            GlobalEventDispatcher.RegisterControlsService.Invoke(DeveloperConfig.GameControls);
            GlobalEventDispatcher.RegisterGameVersionService.Invoke(DeveloperConfig.GameVersion);
            _contextInteractionService.SetupPlayerController(DeveloperConfig);

            // We need to set culture to this, otherwise e.g. polish numbers aren't parsed correct.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            _unityMonoService.SetMonoBehaviour(this);

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
            _configService.LoadRootJson();
            _configService.LoadGothicInis(GameVersion.Gothic1);

            _skyService.InitWorld(); // Add lighting to world. (otherwise VOBs/NPCs are black).

            ZenKit.Logger.Set(_configService.Dev.ZenKitLogLevel, Logger.OnZenKitLogMessage);
            DirectMusic.Logger.Set(_configService.Dev.DirectMusicLogLevel, Logger.OnDirectMusicLogMessage);

            _resourceCacheService.Init(_configService.Root.Gothic1Path);

            _audioService.InitMusic();
            _staticCacheService.Init();
            _textureService.Init();
            _npcService.Init();
            _vobService.Init();

            _videoService.InitVideos();
            _saveGameService.LoadNewGame();
        }

        private async Task InitLab()
        {
            // TODO - Broken. Fix before use.
            // NpcHelper.CacheHero();

            _bootstrapDomain.Boot();

            if (!_staticCacheService.DoGlobalCacheFilesExist())
            {
                Logger.LogErrorEditor("Please load game once to create global cache first!", LogCat.Debug);
                throw new SystemException("Please load game once to create global cache first!");
            }
            await _staticCacheService.LoadGlobalCache().AwaitAndLog();
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

            _contextInteractionService.InitUIInteraction(); // For (e.g.) QuestLog to enable hand pointer.
            BootstrapPlayer();
        }

        private void BootstrapPlayer()
        {
            // Add Missions and Notes
            {
                string topic;
                topic = "0 - The Lost Artifact";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "I venture into the ruins of an ancient temple to recover a powerful artifact believed to be hidden within.");
                    _storyService.ExtLogAddEntry(topic, "I must beware of the traps and guardians that protect it.");
                    _storyService.ExtLogAddEntry(topic, "As I explore, I uncover ancient writings that hint at the artifact's true power.");
                    _storyService.ExtLogAddEntry(topic, "I encounter rival treasure hunters who will stop at nothing to claim the artifact for themselves.");
                    _storyService.ExtLogAddEntry(topic, "I must solve intricate puzzles that guard the inner sanctum of the temple.");
                    _storyService.ExtLogAddEntry(topic, "The deeper I go, the more I feel the weight of history pressing down on me.");
                    _storyService.ExtLogAddEntry(topic, "Each encounter reveals different perspectives on what heroism truly means.");
                    _storyService.ExtLogAddEntry(topic, "Ultimately, my search becomes not just about finding one person but rediscovering what it means to be brave.");
                    _storyService.ExtLogAddEntry(topic, "In confronting challenges along the way, I may find my own path as a hero in this world.");
                    _storyService.ExtLogAddEntry(topic, "I aim to uncover the truth behind these ghostly echoes and their connection to the camp's dark history.");
                    _storyService.ExtLogAddEntry(topic, "I speak with locals who share chilling tales of past events that haunt the area.");
                    _storyService.ExtLogAddEntry(topic, "As night falls, I set up camp to witness the phenomena firsthand.");
                    _storyService.ExtLogAddEntry(topic, "I discover a hidden chamber that holds secrets long forgotten by time.");
                    _storyService.ExtLogAddEntry(topic, "The whispers grow louder, leading me to confront a lingering spirit seeking closure.");
                    _storyService.ExtLogAddEntry(topic, "I face moral dilemmas when encountering creatures that guard these rare items.");
                    _storyService.ExtLogAddEntry(topic, "My choices could either empower him or lead to disastrous consequences for the camp.");
                    _storyService.ExtLogAddEntry(topic, "Ultimately, I must weigh my own desires against the potential fallout of his spell.");
                }

                topic = "1 - Echoes of the Past";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "I investigate strange occurrences in the Old Camp, where villagers report hearing whispers at night.");
                }

                topic = "2 - The Price of Power";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "A local mage seeks my help in gathering rare ingredients for a spell that promises great power.");
                    _storyService.ExtLogAddEntry(topic, "I must decide whether to assist him or expose his dangerous ambitions to the camp leaders.");
                    _storyService.ExtLogAddEntry(topic, "As I gather ingredients, I learn about the mage's troubled past and his obsession with power.");
                }

                topic = "3 - Beneath the Surface";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "I explore the depths of a long-abandoned mine rumored to be haunted.");
                    _storyService.ExtLogAddEntry(topic, "I need to discover what lies beneath and confront the malevolent force that has kept it hidden for so long.");
                    _storyService.ExtLogAddEntry(topic, "As I descend, I encounter remnants of miners who vanished without a trace.");
                    _storyService.ExtLogAddEntry(topic, "Strange noises echo through the tunnels, heightening my sense of dread.");
                    _storyService.ExtLogAddEntry(topic, "I find clues suggesting that dark rituals were once performed here.");
                    _storyService.ExtLogAddEntry(topic, "Confronting whatever lurks in the shadows will test my courage and resolve.");
                }

                topic = "4 - A Thief's Redemption";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "I help a reformed thief prove his innocence after being accused of stealing from the camp's treasury.");
                    _storyService.ExtLogAddEntry(topic, "I gather evidence and confront those who seek to frame him.");
                    _storyService.ExtLogAddEntry(topic, "My investigation leads me through back alleys and hidden corners of the camp.");
                    _storyService.ExtLogAddEntry(topic, "Along the way, I meet other characters who have their own motives regarding the theft.");
                    _storyService.ExtLogAddEntry(topic, "Each piece of evidence brings me closer to uncovering a larger conspiracy at play.");
                    _storyService.ExtLogAddEntry(topic, "In a final confrontation, I must decide whether justice or mercy prevails.");
                }

                topic = "5 - The Beast Within";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "A series of brutal attacks on livestock has left the villagers terrified.");
                    _storyService.ExtLogAddEntry(topic, "I hunt down the creature responsible and uncover whether it's merely a beast or something more sinister.");
                    _storyService.ExtLogAddEntry(topic, "Villagers share their encounters with fear in their eyes, painting a vivid picture of terror.");
                    _storyService.ExtLogAddEntry(topic, "As I track the creature, I discover signs that suggest it may be more than just an animal attack.");
                    _storyService.ExtLogAddEntry(topic, "Clues lead me into dark woods where shadows seem to move on their own.");
                    _storyService.ExtLogAddEntry(topic, "When I finally confront the beast, its true nature reveals a tragic story that challenges my perception.");
                }

                topic = "6 - Allies in Shadows";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, "I join forces with a secretive faction operating in the shadows.");
                    _storyService.ExtLogAddEntry(topic, "I complete tasks that test my loyalty and skills, ultimately deciding whether to support their cause or betray them.");
                    _storyService.ExtLogAddEntry(topic, "The faction’s goals are shrouded in mystery, making me question their true intentions.");
                    _storyService.ExtLogAddEntry(topic, "As missions unfold, I uncover secrets about powerful figures within the camp that they aim to undermine.");
                    _storyService.ExtLogAddEntry(topic, "My actions can either strengthen or weaken their influence in our world.");
                    _storyService.ExtLogAddEntry(topic, "In a climactic moment, I'm faced with a choice that could change everything for me and those around me.");
                }

                for (int i = 7; i < 40; i++)
                {
                    topic = $"{i} - Mission";
                    _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Running);
                    _storyService.ExtLogAddEntry(topic, $"This is a placeholder for mission {i}.");
                }

                topic = "1 - Trial by Fire";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Failure);
                    _storyService.ExtLogAddEntry(topic, "I participate in a series of trials set by the camp's leaders to prove my worth as a warrior.");
                    _storyService.ExtLogAddEntry(topic, "I must overcome challenges that will test my combat skills and strategic thinking.");
                    _storyService.ExtLogAddEntry(topic, "Each trial is designed not only for strength but also for wisdom and resilience under pressure.");
                    _storyService.ExtLogAddEntry(topic, "Fellow challengers become both allies and rivals as we compete for honor and recognition.");
                    _storyService.ExtLogAddEntry(topic, "The outcome of these trials will determine my standing within the camp’s hierarchy.");
                    _storyService.ExtLogAddEntry(topic, "Ultimately, my performance could lead to unexpected alliances or bitter enmities.");
                }

                topic = "2 - The Forgotten Heirloom";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Failure);
                    _storyService.ExtLogAddEntry(topic, "I assist an elderly woman in recovering a family heirloom lost during her youth.");
                    _storyService.ExtLogAddEntry(topic, "The journey takes me through treacherous terrain and reveals long-buried family secrets.");
                    _storyService.ExtLogAddEntry(topic, "Clues lead us to forgotten places tied to her family's history, each revealing more about her past.");
                    _storyService.ExtLogAddEntry(topic, "Along our journey, we encounter obstacles that test our resolve and resourcefulness together.");
                    _storyService.ExtLogAddEntry(topic, "The heirloom itself holds significance beyond its material value; it symbolizes lost connections and memories.");
                    _storyService.ExtLogAddEntry(topic, "In recovering it, we not only restore her legacy but also heal old wounds.");
                }

                topic = "1 - The Call of Adventure";
                _storyService.ExtLogCreateTopic(topic, SaveTopicSection.Missions);
                {
                    _storyService.ExtLogSetTopicStatus(topic, SaveTopicStatus.Obsolete);
                    _storyService.ExtLogAddEntry(topic, "I embark on a quest to find a legendary hero who vanished years ago.");
                    _storyService.ExtLogAddEntry(topic, "Following clues across various camps leads me into uncharted territories filled with danger.");
                    _storyService.ExtLogAddEntry(topic, "Along my journey, stories about this hero inspire hope among those who have lost faith.");
                }

                _storyService.ExtLogCreateTopic("Note number 1", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 1", "Note number 1 entry 1");
                    _storyService.ExtLogAddEntry("Note number 1", "Note number 1 entry 2");
                    _storyService.ExtLogAddEntry("Note number 1", "Note number 1 entry 3");
                    _storyService.ExtLogAddEntry("Note number 1", "Note number 1 entry 4");
                    _storyService.ExtLogAddEntry("Note number 1", "Note number 1 entry 5");
                }
                _storyService.ExtLogCreateTopic("Note number 2", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 2", "Note number 2 entry 1");
                    _storyService.ExtLogAddEntry("Note number 2", "Note number 2 entry 2");
                    _storyService.ExtLogAddEntry("Note number 2", "Note number 2 entry 3");
                }
                _storyService.ExtLogCreateTopic("Note number 3", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 3", "Note number 3 entry 1");
                    _storyService.ExtLogAddEntry("Note number 3", "Note number 3 entry 2");
                }
                _storyService.ExtLogCreateTopic("Note number 4", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 4", "Note number 4 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 5", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 5", "Note number 5 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 6", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 6", "Note number 6 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 7", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 7", "Note number 7 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 8", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 8", "Note number 8 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 9", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 9", "Note number 9 entry 1");
                }
                _storyService.ExtLogCreateTopic("Note number 10", SaveTopicSection.Notes);
                {
                    _storyService.ExtLogAddEntry("Note number 10", "Note number 10 entry 1");
                }
            }
        }

        private void OnDestroy()
        {
            _gameStateService.Dispose();
            _vmCacheService.Dispose();
            _textureCacheService.Dispose();
            _multiTypeCacheService.Dispose();
            _morphMeshCacheService.Dispose();
        }
    }
}
