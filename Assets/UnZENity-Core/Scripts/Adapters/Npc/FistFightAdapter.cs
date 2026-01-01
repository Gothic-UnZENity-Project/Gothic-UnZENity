using System.Collections.Generic;
using UnityEngine;

namespace GUZ.Core.Adapters.Npc
{
    /// <summary>
    /// Hint: This Component is attached to "BIP01 L/R HAND" which is parent of fingers.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class FistFightAdapter : MonoBehaviour
    {
        private SphereCollider _sphereCollider;
        private readonly List<Transform> _allFingerTransforms = new();

        // TODO - Checked with G1 Zombie. Check how it behaves with a troll hand etc.
        private const float _radiusMultiplier = 0.5f;


        private void Awake()
        {
            _sphereCollider = GetComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;

            GetAllChildrenRecursively(transform);
        }

        private void GetAllChildrenRecursively(Transform parent)
        {
            foreach (Transform child in parent)
            {
                _allFingerTransforms.Add(child);
                GetAllChildrenRecursively(child);
            }
        }
        
        private void Update()
        {
            var farthestFingerTransform = GetFarthestTransform(_allFingerTransforms);

            // The local position of the finger relative to the hand start
            var fingerLocalPos = transform.InverseTransformPoint(farthestFingerTransform.position);
            var handLength = fingerLocalPos.magnitude;

            // Place the sphere center between the hand and the fingertip
            _sphereCollider.center = fingerLocalPos / 2f;

            // Scale the radius based on the hand length
            // This ensures (e.g.) a Troll gets a massive sphere and a Human gets a small one
            _sphereCollider.radius = handLength * _radiusMultiplier;
        }
        
        /// <summary>
        /// During animations, the position of fingers related to the arm can change. (e.g. by a troll opening his hands).
        /// We therefore need to calculate the farthest finger each frame to span the bounds of fist hitbox collider correctly.
        /// </summary>
        private Transform GetFarthestTransform(List<Transform> transforms)
        {
            Transform farthest = null;
            var maxDistanceSqr = 0f;
            var rootPosition = transform.position;

            foreach (var t in transforms)
            {
                var distanceSqr = (t.position - rootPosition).sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                {
                    maxDistanceSqr = distanceSqr;
                    farthest = t;
                }
            }

            return farthest;
        }
    }
}
