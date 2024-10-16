using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Data
{
    /// <summary>
    /// Container class for NPC data. It includes references to ZenKit
    ///   1. NpcInstance with default data
    ///   2. Vobs.Npc for data to save in a save game
    /// and UnZENity:
    ///   1. NpcProperties - Component storing data next to its GameObject
    /// </summary>
    public class NpcData
    {
        public NpcInstance Instance;
        public ZenKit.Vobs.Npc Npc;
        public NpcProperties Properties;
        public GameObject Go => Properties.gameObject;
    }
}
