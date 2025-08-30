using GUZ.Core.Animations;
using GUZ.Core.Config;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Npc;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.World;
using GUZ.Manager;

namespace GUZ.Core.Editor
{
    /// <summary>
    /// During Editor tool usage, we execute normal game logic. We therefore need to set some properties.
    /// </summary>
    public class EditorDataProvider : IGlobalDataProvider
    {
        public ConfigManager Config { get; set; }
        public LocalizationManager Localization { get; set; }
        public SaveGameManager SaveGame { get; }
        public LoadingManager Loading { get; }
        public StaticCacheManager StaticCache { get; set; }
        public PlayerManager Player { get; }
        public MarvinManager Marvin { get; }
        public SkyManager Sky { get; }
        public GameTimeService Time { get; }
        public VideoManager Video { get; }
        public MusicService Music { get; }
        public RoutineManager Routines { get; }
        public TextureManager Textures { get; }
        public FontManager Font { get; }
        public StationaryLightsManager Lights { get; }
        public StoryManager Story { get; }
        public VobManager Vobs { get; }
        public NpcManager Npcs { get; }
        public NpcAiManager NpcAi { get; }
        public AnimationManager Animations { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
