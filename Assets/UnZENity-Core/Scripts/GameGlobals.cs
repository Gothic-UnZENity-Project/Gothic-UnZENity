using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.World;

namespace GUZ.Core
{
    public static class GameGlobals
    {
        public static IGlobalDataProvider Instance;

        public static PlayerManager Player => Instance.Player;
        public static GameTimeService Time => Instance.Time;
        public static RoutineManager Routines => Instance.Routines;
        public static StoryService Story => Instance.Story;
        public static VobManager Vobs => Instance.Vobs;
        public static VobMeshCullingService VobMeshCulling => Instance.VobMeshCulling;
        public static NpcMeshCullingService NpcMeshCulling => Instance.NpcMeshCulling;
        public static SpeechToTextService SpeechToText => Instance.SpeechToText;
    }
}
