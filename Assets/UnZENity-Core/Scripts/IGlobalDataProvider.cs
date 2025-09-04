using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.World;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public PlayerManager Player { get; }
        public MarvinManager Marvin { get; }
        public GameTimeService Time { get; }
        public RoutineManager Routines { get; }
        public StoryService Story { get; }
        public VobManager Vobs { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
