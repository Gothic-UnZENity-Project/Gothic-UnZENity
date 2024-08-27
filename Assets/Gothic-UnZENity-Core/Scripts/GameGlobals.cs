using System;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Culling;
using GUZ.Core.Manager.Settings;
using GUZ.Core.World;

namespace GUZ.Core
{
    public static class GameGlobals
    {
        public static IGlobalDataProvider Instance;

        [Obsolete("Don't use globals.")] public static GameConfiguration Config => Instance.Config;
        [Obsolete("Don't use globals.")] public static GameSettings Settings => Instance.Settings;
        [Obsolete("Don't use globals.")] public static SkyManager Sky => Instance.Sky;
        [Obsolete("Don't use globals.")] public static GameTime Time => Instance.Time;
        [Obsolete("Don't use globals.")] public static RoutineManager Routines => Instance.Routines;
        [Obsolete("Don't use globals.")] public static TextureManager Textures => Instance.Textures;
        [Obsolete("Don't use globals.")] public static GuzSceneManager Scene => Instance.Scene;
        [Obsolete("Don't use globals.")] public static StoryManager Story => Instance.Story;
        [Obsolete("Don't use globals.")] public static FontManager Font => Instance.Font;
        [Obsolete("Don't use globals.")] public static StationaryLightsManager Lights => Instance.Lights;
        [Obsolete("Don't use globals.")] public static VobMeshCullingManager VobMeshCulling => Instance.VobMeshCulling;
        [Obsolete("Don't use globals.")] public static NpcMeshCullingManager NpcMeshCulling => Instance.NpcMeshCulling;
        [Obsolete("Don't use globals.")] public static VobSoundCullingManager SoundCulling => Instance.SoundCulling;
    }
}
