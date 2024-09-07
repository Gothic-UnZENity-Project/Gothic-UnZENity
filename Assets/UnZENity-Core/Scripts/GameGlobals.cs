using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;

namespace GUZ.Core
{
    public static class GameGlobals
    {
        public static IGlobalDataProvider Instance;

        public static GameConfiguration Config => Instance.Config;
        public static GameSettings Settings => Instance.Settings;
        public static SkyManager Sky => Instance.Sky;
        public static GameTime Time => Instance.Time;
        public static RoutineManager Routines => Instance.Routines;
        public static TextureManager Textures => Instance.Textures;
        public static GuzSceneManager Scene => Instance.Scene;
        public static StoryManager Story => Instance.Story;
        public static FontManager Font => Instance.Font;
        public static StationaryLightsManager Lights => Instance.Lights;
        public static VobMeshCullingManager VobMeshCulling => Instance.VobMeshCulling;
        public static NpcMeshCullingManager NpcMeshCulling => Instance.NpcMeshCulling;
        public static VobSoundCullingManager SoundCulling => Instance.SoundCulling;
    }
}
