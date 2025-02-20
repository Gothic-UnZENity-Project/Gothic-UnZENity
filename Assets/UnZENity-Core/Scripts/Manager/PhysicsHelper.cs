using GUZ.Core._Npc2;
using GUZ.Core.Properties;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public static class PhysicsHelper
    {
        public static void DisablePhysicsForNpc(NpcPrefabProperties2 prefabProps)
        {
            prefabProps.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = true;
        }

        public static void EnablePhysicsForNpc(NpcPrefabProperties2 prefabProps)
        {
            prefabProps.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
