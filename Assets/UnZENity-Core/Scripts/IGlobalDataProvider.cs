using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Vobs;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public VobMeshCullingService VobMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
