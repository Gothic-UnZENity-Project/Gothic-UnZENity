using GUZ.Core._Npc2;
using GUZ.Core.Config;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.World;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public ConfigManager Config { get; }
        public SaveGameManager SaveGame { get; }
        public LoadingManager Loading { get; }
        public StaticCacheManager StaticCache { get; }

        public PlayerManager Player { get; }
        public SkyManager Sky { get; }
        public GameTime Time { get; }
        public VideoManager Video { get; }
        public RoutineManager Routines { get; }
        public TextureManager Textures { get; }
        public FontManager Font { get; }
        public StationaryLightsManager Lights { get; }
        public StoryManager Story { get; }
        public VobManager Vobs { get; }
        public NpcManager2 Npcs { get; }
        public NpcAiManager2 NpcAi { get; }
        public VobMeshCullingManager VobMeshCulling { get; }
        public NpcMeshCullingManager NpcMeshCulling { get; }
        public VobSoundCullingManager SoundCulling { get; }
    }
}
