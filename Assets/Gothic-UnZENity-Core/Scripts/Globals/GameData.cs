using System.Collections.Generic;
using GUZ.Core.Data;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.World;
using GUZ.Core.World.WayNet;
using UnityEngine.SceneManagement;
using ZenKit;
using ZenKit.Daedalus;
using WayPoint = GUZ.Core.World.WayNet.WayPoint;

namespace GUZ.Core.Globals
{
    public static class GameData
    {
        public static Vfs Vfs;
        public static DaedalusVm GothicVm;
        public static DaedalusVm SfxVm; // Sound FX
        public static DaedalusVm PfxVm; // Particle FX
        public static DaedalusVm MusicVm;

        public static WorldData World;

        // Lookup optimized WayNet data
        public static readonly Dictionary<string, WayPoint> WayPoints = new();
        public static readonly Dictionary<string, WayPoint> FreePoints = new();

        // Reorganized waypoints from world data.
        public static Dictionary<string, DijkstraWayPoint> DijkstraWaypoints = new();
        public static readonly List<VobProperties> VobsInteractable = new();

        public static class Dialogs
        {
            public static List<InfoInstance> Instances = new();
            public static bool IsInDialog;

            public static int GestureCount = 0;
            public static class CurrentDialog
            {
                public static InfoInstance Instance;
                public static List<DialogOption> Options = new();
            }

            public static void Dispose()
            {
                IsInDialog = false;
                CurrentDialog.Instance = null;
                CurrentDialog.Options.Clear();
                GestureCount = 0;
            }
        }

        // FIXME Find a better place for the NPC routines. E.g. on the NPCs itself? But we e.g. need to have a static NPCObject List to do so.
        public static Dictionary<int, List<RoutineData>> npcRoutines = new();

        public static Scene? WorldScene;

        public static void Reset()
        {
            World = null;
            WayPoints.Clear();
            FreePoints.Clear();
            VobsInteractable.Clear();
        }

        public static void Dispose()
        {
            // Needs to be reset as Unity won't clear static variables when closing game in EditorMode.
            Vfs = null;
            World = null;
            GothicVm = null;
            SfxVm = null;
            MusicVm = null;
            WayPoints.Clear();
            FreePoints.Clear();
            VobsInteractable.Clear();

            Dialogs.Dispose();
        }
    }
}
