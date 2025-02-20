using GUZ.Core._Npc2;
using GUZ.Core.Properties;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public static class PhysicsHelper
    {
        public static void DisablePhysicsForNpc(NpcProperties2 props)
        {
            props.NpcPrefabProperties.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = true;
        }

        public static void EnablePhysicsForNpc(NpcProperties2 props)
        {
            props.NpcPrefabProperties.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
