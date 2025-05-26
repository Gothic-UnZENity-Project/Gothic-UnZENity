using System.Collections.Generic;
using System.Text;
using GUZ.Core.Data;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.Vob.WayNet;
using GUZ.Core.World;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using WayNet_WayPoint = GUZ.Core.Vob.WayNet.WayPoint;

namespace GUZ.Core.Globals
{
    public static class GameData
    {
        /// <summary>
        /// Represents the currently installed Gothic language (windows-1250,1251,1252)
        /// </summary>
        public static Encoding Encoding;
        public static string Language;
        public static DaedalusVm GothicVm;
        public static DaedalusVm MenuVm;
        public static DaedalusVm SfxVm; // Sound FX
        public static DaedalusVm PfxVm; // Particle FX

        // Lookup optimized WayNet data
        public static readonly Dictionary<string, WayNet_WayPoint> WayPoints = new();
        public static readonly Dictionary<string, FreePoint> FreePoints = new();

        // Reorganized waypoints from world data.
        public static Dictionary<string, DijkstraWaypoint> DijkstraWaypoints = new();
        
        // [IInteractiveObject] => VisualScheme (aka vob.Visual.Name.SubString("_");
        public static readonly Dictionary<string, List<(IInteractiveObject Vob, GameObject Go)>> VobsInteractable = new();

        /// <summary>
        /// Store and update global NPC information about dialog options already listened to.
        /// </summary>
        public static HashSet<int> KnownDialogInfos = new();
        
        public static class Dialogs
        {
            public static List<InfoInstance> Instances = new();
            public static bool IsInDialog;

            public static CutsceneLibrary CutsceneLibrary;

            public static int GestureCount;

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

        public static int GuildTableSize;
        public static int GuildCount;
        public static int[] GuildAttitudes;

        public static GuildValuesInstance cGuildValue;

        // FIXME Find a better place for the NPC routines. E.g. on the NPCs itself? But we e.g. need to have a static NPCObject List to do so.
        public static Dictionary<int, List<RoutineData>> NpcRoutines = new();

        public static bool InGameAndAlive = false;

        public static void Reset()
        {
            WayPoints.Clear();
            FreePoints.Clear();
            VobsInteractable.Clear();
        }

        public static void Dispose()
        {
            // Needs to be reset as Unity won't clear static variables when closing game in EditorMode.
            GothicVm = null;
            SfxVm = null;
            WayPoints.Clear();
            FreePoints.Clear();
            VobsInteractable.Clear();

            Dialogs.Dispose();
        }
    }
}
