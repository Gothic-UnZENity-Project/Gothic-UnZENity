using UnityEngine;

namespace GUZ.Core.Npc
{
    public class RootCollisionHandler : BasePlayerBehaviour
    {
        protected override void Awake()
        {
            base.Awake();

            // Cached object which will be used later.
            NpcData.PrefabProps.ColliderRootMotion = gameObject.transform;
        }

        /// <summary>
        /// We need to apply physics on the NPC itself.
        /// General movement and animations are handled within AnimationSystem.cs. This Collider object is to add physics on top.
        /// </summary>
        private void Update()
        {
            /*
             * NPC GO hierarchy:
             *
             * root
             *  /BIP01/ <- animation root
             *    /RootCollisionHandler <- Moved with animation as inside BIP01, but physics are applied and merged to root
             *    /... <- animation bones
             */

            // Apply physics based position change to root.
            Go.transform.localPosition += transform.localPosition;

            // Empty physics based diff. Next frame physics will be recalculated.
            transform.localPosition = Vector3.zero;
        }
    }
}
