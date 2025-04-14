using UnityEngine;

namespace GUZ.Core.Npc
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class RootCollisionHandler : BasePlayerBehaviour
    {
        [SerializeField]
        private CapsuleCollider _capsuleCollider;

        private SkinnedMeshRenderer[] _meshRenderers;

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
            if (_meshRenderers == null)
            {
                _meshRenderers = Go.GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            var bbox = new Bounds();

            foreach (var rend in _meshRenderers)
            {
                bbox.Encapsulate(rend.localBounds);
            }

            // We only want to move the Collider to the center of the body in vertical orientation. A slight move left/right can be ignored.
            _capsuleCollider.center = new Vector3(0, bbox.center.y, 0);
            _capsuleCollider.height = bbox.size.y;

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
