using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Vobs;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public VobManager Vobs { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
