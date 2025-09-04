using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Services.UI;

namespace GUZ.Core
{
    public interface IGlobalDataProvider
    {
        public SaveGameManager SaveGame { get; }
        public LoadingManager Loading { get; }
        public StaticCacheManager StaticCache { get; }

        public PlayerManager Player { get; }
        public MarvinManager Marvin { get; }
        public SkyService Sky { get; }
        public GameTimeService Time { get; }
        public VideoManager Video { get; }
        public RoutineManager Routines { get; }
        public TextureManager Textures { get; }
        public StationaryLightsManager Lights { get; }
        public StoryManager Story { get; }
        public VobManager Vobs { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
