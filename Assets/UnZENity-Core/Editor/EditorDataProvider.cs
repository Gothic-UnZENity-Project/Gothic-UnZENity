using GUZ.Core.Domain.Animations;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.StaticCache;
using GUZ.Core.Services.UI;
using GUZ.Core.Services.World;
using GUZ.Services.UI;

namespace GUZ.Core.Editor
{
    /// <summary>
    /// During Editor tool usage, we execute normal game logic. We therefore need to set some properties.
    /// </summary>
    public class EditorDataProvider : IGlobalDataProvider
    {
        public SaveGameService SaveGame { get; }
        public PlayerManager Player { get; }
        public MarvinManager Marvin { get; }
        public SkyService Sky { get; }
        public GameTimeService Time { get; }
        public VideoService Video { get; }
        public RoutineManager Routines { get; }
        public StoryService Story { get; }
        public VobManager Vobs { get; }
        public NpcService Npcs { get; }
        public NpcAiService NpcAi { get; }
        public AnimationService Animations { get; }
        public VobMeshCullingService VobMeshCulling { get; }
        public NpcMeshCullingService NpcMeshCulling { get; }
        public SpeechToTextService SpeechToText { get; }
    }
}
