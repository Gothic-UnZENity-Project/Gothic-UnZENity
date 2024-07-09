using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public GameConfiguration Config { get; }
        public GameSettings Settings { get; }
        public SkyManager Sky { get; }
        public GameTime Time { get; }
        public RoutineManager Routines { get; }
        public TextureManager Textures { get; }
        public GuzSceneManager Scene { get; }
        public FontManager Font { get; }
        public StationaryLightsManager Lights { get; }
        public VobMeshCullingManager VobMeshCulling { get; }
        public NpcMeshCullingManager NpcMeshCulling { get; }
        public VobSoundCullingManager SoundCulling { get; }
    }
}
