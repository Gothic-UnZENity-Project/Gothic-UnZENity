using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Data object containing references to information needed on Unity side, but not stored on any ZenKit object.
    /// </summary>
    public class NpcContainer2
    {
        public NpcInstance Instance;
        public ZenKit.Vobs.Npc Vob;
        public NpcProperties2 Properties;
        public GameObject Go => Properties.gameObject;
    }
}
