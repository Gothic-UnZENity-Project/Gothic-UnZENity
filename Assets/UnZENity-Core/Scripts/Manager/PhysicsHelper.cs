using GUZ.Core.Adapters.Npc;
using GUZ.Core.Npc;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public static class PhysicsHelper
    {
        public static void DisablePhysicsForNpc(NpcPrefabProperties prefabProps)
        {
            prefabProps.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = true;
        }

        public static void EnablePhysicsForNpc(NpcPrefabProperties prefabProps)
        {
            prefabProps.ColliderRootMotion.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
