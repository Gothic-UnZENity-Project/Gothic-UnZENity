using UnityEngine;

namespace GUZ.Core.Npc
{
    public class RootCollisionHandler : BasePlayerBehaviour
    {
        protected override void Awake()
        {
            base.Awake();

            // Cached object which will be used later.
            NpcData.Properties.NpcPrefabProperties.ColliderRootMotion = gameObject.transform;
        }

        private void Update()
        {
            // As we use legacy animations, we can't use RootMotion. We therefore need to rebuild it.

            /*
             * NPC GO hierarchy:
             *
             * root
             *  /BIP01/ <- animation root
             *    /RootCollisionHandler <- Moved with animation as inside BIP01, but physics are applied and merged to root
             *    /... <- animation bones
             */

            var collisionTransform = transform;

            // Apply physics based position change to root.
            RootGo.transform.localPosition += collisionTransform.localPosition;

            // Empty physics based diff. Next frame physics will be recalculated.
            collisionTransform.localPosition = Vector3.zero;
        }
    }
}
