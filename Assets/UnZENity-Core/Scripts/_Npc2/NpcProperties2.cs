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
        public List<InfoInstance> Dialogs = new();

        // NPC items/talents/...
        public Dictionary<VmGothicEnums.Talent, int> Talents = new();
        public string UsedItemSlot;
        public List<ItemInstance> EquippedItems = new();
        public Dictionary<uint, int> Items = new(); // itemId => amount
        public VmGothicEnums.WeaponState WeaponState;


        // Visual
        public string MdmName;
        public string MdsNameMdhBase;
        public string MdsMdhOverlayName;

        // An MDS file has always an MDH file named identically
        public string MdhBaseName => MdsNameMdhBase;
        public string MdhOverlayName => MdsMdhOverlayName;

        public VmGothicExternals.ExtSetVisualBodyData BodyData;
        public Transform Head;
    }
}
