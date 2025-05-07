using GUZ.Core.Npc;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Data.Container
{
    /// <summary>
    /// Data object containing references to information needed on ZenKit and Unity side.
    /// </summary>
    public class NpcContainer
    {
        // ZenKit data
        public NpcInstance Instance;
        public ZenKit.Vobs.Npc Vob;

        // Unity Data
        public GameObject Go;
        /// <summary>
        /// Unity Properties which are loaded from Daedalus and won't be stored on ZenKit data.
        /// </summary>
        public NpcProperties Props;

        // Cache objects from Prefab
        public NpcPrefabProperties PrefabProps;

    }
}
