using System.Collections.Generic;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Vm;
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

        // Visual
        public string MdmName;
        public string MdsNameBase;
        public string MdsOverlayName;
        public VmGothicExternals.ExtSetVisualBodyData BodyData;
    }
}
