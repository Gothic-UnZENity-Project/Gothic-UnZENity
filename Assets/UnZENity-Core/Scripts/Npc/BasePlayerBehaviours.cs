using System;
using GUZ.Core._Npc2;
using GUZ.Core.Extensions;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc
{
    public abstract class BasePlayerBehaviour : MonoBehaviour
    {
        [NonSerialized]
        public GameObject RootGo;
        [NonSerialized]
        public NpcContainer2 NpcData;

        public NpcProperties2 Properties => NpcData.Properties;
        public NpcInstance NpcInstance => NpcData.Instance;
        public GameObject Go => NpcData.Go;

        protected virtual void Awake()
        {
            var lazyComp = GetComponentInParent<NpcLoader2>();

            // As we lazy load NPCs, the NpcInstance is always set inside NpcLoader before we initialize this prefab!
            RootGo = lazyComp.gameObject;
            NpcData = lazyComp.Npc.GetUserData2();
        }
    }
}
