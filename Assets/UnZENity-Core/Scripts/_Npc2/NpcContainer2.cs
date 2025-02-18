using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Data object containing references to information needed on ZenKit and Unity side.
    /// </summary>
    public class NpcContainer2
    {
        // ZenKit data
        public NpcInstance Instance;
        public ZenKit.Vobs.Npc Vob;

        // Unity Data
        public GameObject Go;
        /// <summary>
        /// Unity Properties which are loaded from Daedalus and won't be stored on ZenKit data.
        /// </summary>
        public NpcProperties2 Properties;
    }
}
