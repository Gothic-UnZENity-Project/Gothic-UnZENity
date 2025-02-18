using System.Collections.Generic;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    public class NpcProperties2
    {
        public List<RoutineData> Routines = new();
        public RoutineData RoutineCurrent;

        // NPC items/talents/...
        public Dictionary<VmGothicEnums.Talent, int> Talents = new();
        public List<ItemInstance> EquippedItems = new();
        public Dictionary<uint, int> Items = new(); // itemId => amount


        // Visual
        public string MdmName;
        public string MdsNameBase;
        public string MdsOverlayName;
        public VmGothicExternals.ExtSetVisualBodyData BodyData;
        public Transform Head;
    }
}
