using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.World;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public GameConfiguration Config { get; }
        public GameSettings Settings { get; }
        public LoadingManager Loading { get; }
        public GltManager Glt { get; }
        public PlayerManager Player { get; }
        public SkyManager Sky { get; }
        public GameTime Time { get; }
        public VideoManager Video { get; }
        public RoutineManager Routines { get; }
        public TextureManager Textures { get; }
        public FontManager Font { get; }
        public VobManager Vobs { get; }
        public StationaryLightsManager Lights { get; }
        public StoryManager Story { get; }
        public VobMeshCullingManager VobMeshCulling { get; }
        public NpcMeshCullingManager NpcMeshCulling { get; }
        public VobSoundCullingManager SoundCulling { get; }
    }
}
