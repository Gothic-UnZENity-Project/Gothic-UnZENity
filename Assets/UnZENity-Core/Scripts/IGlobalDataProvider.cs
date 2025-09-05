using GUZ.Core.Manager;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public GameTimeService Time { get; }
        public StoryService Story { get; }
        public VobManager Vobs { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
