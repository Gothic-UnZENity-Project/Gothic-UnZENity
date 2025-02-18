using System.Collections.Generic;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;

namespace GUZ.Core._Npc2
{
    public class NpcProperties2 : VobProperties
    {
        public List<RoutineData> Routines = new();
        public RoutineData RoutineCurrent;
    }
}
