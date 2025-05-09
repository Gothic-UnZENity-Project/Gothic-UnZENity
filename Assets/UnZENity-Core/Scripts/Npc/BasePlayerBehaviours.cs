using System;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc
{
    public abstract class BasePlayerBehaviour : MonoBehaviour
    {
        [NonSerialized]
        public NpcContainer NpcData;

        public NpcProperties Properties => NpcData.Props;
        public NpcPrefabProperties PrefabProps => NpcData.PrefabProps;
        public NpcInstance NpcInstance => NpcData.Instance;
        public GameObject Go => NpcData.Go;

        protected virtual void Awake()
        {
            var lazyComp = GetComponentInParent<NpcLoader>();

            // As we lazy load NPCs, the NpcInstance is always set inside NpcLoader before we initialize this prefab!
            NpcData = lazyComp.Npc.GetUserData();
        }
    }
}
