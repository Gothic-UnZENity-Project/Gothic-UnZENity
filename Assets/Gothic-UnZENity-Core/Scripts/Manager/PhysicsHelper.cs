using GUZ.Core.Properties;
using UnityEngine;

namespace GUZ.Core.Manager
{
    public static class PhysicsHelper
    {
        public static void DisablePhysicsForNpc(NpcProperties props)
        {
            props.colliderRootMotion.GetComponent<Rigidbody>().isKinematic = true;
        }

        public static void EnablePhysicsForNpc(NpcProperties props)
        {
            props.colliderRootMotion.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
