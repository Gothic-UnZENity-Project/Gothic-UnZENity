using GUZ.Core.Animations;
using GUZ.Core.Config;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Npc;
using GUZ.Core.Services.Culling;
using GUZ.Core.UnZENity_Core.Scripts.Manager;
using GUZ.Core.World;
using GUZ.Manager;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public ConfigManager Config { get; }
        public LocalizationManager Localization { get; }
        public SaveGameManager SaveGame { get; }
        public LoadingManager Loading { get; }
        public StaticCacheManager StaticCache { get; }

        public PlayerManager Player { get; }
        public MarvinManager Marvin { get; }
        public SkyManager Sky { get; }
        public GameTime Time { get; }
        public VideoManager Video { get; }
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
        public VoiceManager Voice { get; }
    }
}
