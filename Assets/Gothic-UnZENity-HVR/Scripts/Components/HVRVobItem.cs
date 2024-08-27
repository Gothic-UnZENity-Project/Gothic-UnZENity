#if GUZ_HVR_INSTALLED
using System.Collections;
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.HVR.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;
using UnityEngine.Animations;

namespace GUZ.HVR.Components
{
    public class HVRVobItem : MonoBehaviour
    {
        [SerializeField] private HVRVobItemProperties _properties;
        [SerializeField] private MeshCollider _meshCollider;

        // We pre-allocate enough entries to fetch at least one entry which is not ourselves.
        private readonly Collider[] _overlapColliders = new Collider[1];
        private static int _ignoreLayerCollisionCheck;


        private struct OverlapCheckData
        {
            public Axis MaxAxis; // Used for P0 and P1 calculation
            public  Axis SecondMaxAxis; // Used for radius calculation
            public  Vector3 Center;
            public  Vector3 OverlapPoint0; // First point where the sphere will start to draw its closing circle
            public  Vector3 OverlapPoint1; // Second point with the closing circle calculation. Will be opposite of first one based on mainAxis
            public  float OverlapRadius; // Radius is calculated as /2 calculation from size of secondMaxAxis
        }


        private void Awake()
        {
            // Pre-fill calculation data before being used every frame.
            if (_ignoreLayerCollisionCheck == 0)
            {
                _ignoreLayerCollisionCheck = Constants.VobItemNoCollision | Constants.HandLayer;
            }
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // OnGrabbed is normally called multiple times. Even after an object is already socketed. If so, then let's stop Grab behaviour.
            if (_properties.IsSocketed)
            {
                return;
            }

            // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items into our hands.
            transform.GetComponent<Rigidbody>().isKinematic = false;

            // Stop collisions while being dragged around (at least shortly; otherwise e.g. items might stick inside chests when pulled out).
            gameObject.layer = Constants.VobItemNoCollision;

            // At least until object isn't colliding with anything any longer, the object will be a ghost (i.e. no collision + transparency activated)
            DynamicMaterialManager.SetDynamicValue(gameObject, Constants.ShaderPropertyTransparency, Constants.ShaderPropertyTransparencyValue);

            // If we want Item collisions, we just temporarily deactivate them until the item is free of collisions.
            if (PlayerPrefsManager.ItemCollisionWhileDragged)
            {
                StartCoroutine(ReEnableCollisionRoutine());
            }

            GameGlobals.VobMeshCulling?.StartTrackVobPositionUpdates(gameObject);
        }

        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            gameObject.layer = Constants.GrabbableLayer; // Back to HVR default

            GameGlobals.VobMeshCulling?.StopTrackVobPositionUpdates(gameObject);

            // Disable "ghostification" of object.
            DynamicMaterialManager.ResetDynamicValue(gameObject, Constants.ShaderPropertyTransparency, Constants.ShaderPropertyTransparencyDefault);
        }

        /// <summary>
        /// Draw Debug Gizmos for Physics.OverlapCapsule calculations and render them kind of visible. ;-)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!GameGlobals.Config.ShowCapsuleOverlapGizmos)
            {
                return;
            }

            var overlapData = CalculateOverlap();

            // The red line indicates the main axis and it's length.
            Gizmos.color = Color.red;
            Gizmos.DrawLine(overlapData.Center, overlapData.OverlapPoint1);

            // The green line indicates the second max axis (used for radius in OverlapCapsule check)
            Gizmos.color = Color.green;
            switch (overlapData.SecondMaxAxis)
            {
                case Axis.X:
                    Gizmos.DrawLine(overlapData.Center, overlapData.Center + new Vector3(overlapData.OverlapRadius, 0, 0));
                    break;
                case Axis.Y:
                    Gizmos.DrawLine(overlapData.Center, overlapData.Center + new Vector3(0, overlapData.OverlapRadius, 0));
                    break;
                default:
                    Gizmos.DrawLine(overlapData.Center, overlapData.Center + new Vector3(0, 0, overlapData.OverlapRadius));
                    break;
            }

            var mainMeshCollider = GetComponent<MeshCollider>();
            // Same calculation as in IsColliderOverlapping(). Used to print sphere's on the touch points for a visible check.
            var overlapColliders = Physics.OverlapCapsule(
                overlapData.OverlapPoint0,
                overlapData.OverlapPoint1,
                overlapData.OverlapRadius,
                Constants.VobItemNoCollision | Constants.HandLayer);

            foreach (var overlapCollider in overlapColliders)
            {
                if (overlapCollider == mainMeshCollider)
                {
                    continue;
                }

                // Yellow indicates touch points between colliders. With this you can check if they collide at the right spot.
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(overlapCollider.ClosestPointOnBounds(overlapData.Center), 0.05f);
            }
        }

        /// <summary>
        /// Check every n-th frame if the object has no collisions any longer. Then re-enable collisions.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReEnableCollisionRoutine()
        {
            while (IsColliderOverlapping())
            {
                yield return new WaitForFixedUpdate();
            }

            // Re-enable collisions
            gameObject.layer = Constants.GrabbableLayer;

            // Disable "ghostification" of object.
            DynamicMaterialManager.ResetDynamicValue(gameObject, Constants.ShaderPropertyTransparency, Constants.ShaderPropertyTransparencyDefault);
        }

        /// <summary>
        /// Physics.OverlapCapsule() check if the item is free of collisions and therefore its collisions can be re-activated again.
        /// </summary>
        private bool IsColliderOverlapping()
        {
            var overlapData = CalculateOverlap();

            // Check for overlapping objects except our own Layer and Hands.
            var colliderCount = Physics.OverlapCapsuleNonAlloc(
                overlapData.OverlapPoint0,
                overlapData.OverlapPoint1,
                overlapData.OverlapRadius,
                _overlapColliders,
                Constants.VobItemNoCollision | Constants.HandLayer);

            return colliderCount > 0;
        }

        /// <summary>
        /// Calculate OverlapCapsule() information based on bounds of MeshCollider.
        /// This is:
        /// * MainAxis - will be used for Point0 and Point1 calculation
        /// * SecondMainAxis - will be used for Capsule radius
        /// * Point0 and Point1 - calculated end-positions between min and max of mesh collider's points
        /// * Radius - used from second max axis (Third/lowest axis value isn't needed as it is always included in the radius dimension)
        /// * Center - used from bounds center
        /// </summary>
        private OverlapCheckData CalculateOverlap()
        {
            var result = new OverlapCheckData();
            var mainBounds = _meshCollider.bounds;

            result.Center = mainBounds.center;

            if (mainBounds.size.x > mainBounds.size.y)
            {
                if (mainBounds.size.x > mainBounds.size.z)
                {
                    result.MaxAxis = Axis.X;
                    result.SecondMaxAxis = mainBounds.size.y > mainBounds.size.z ? Axis.Y : Axis.Z;
                }
                else
                {
                    result.MaxAxis = Axis.Z;
                    result.SecondMaxAxis = Axis.X;
                }
            }
            else
            {
                if (mainBounds.size.y > mainBounds.size.z)
                {
                    result.MaxAxis = Axis.Y;
                    result.SecondMaxAxis = mainBounds.size.x > mainBounds.size.z ? Axis.X : Axis.Z;
                }
                else
                {
                    result.MaxAxis = Axis.Z;
                    result.SecondMaxAxis = Axis.Y;
                }
            }

            switch (result.MaxAxis)
            {
                case Axis.X:
                    result.OverlapPoint0 = mainBounds.center - new Vector3(mainBounds.size.x / 2, 0, 0);
                    result.OverlapPoint1 = mainBounds.center + new Vector3(mainBounds.size.x / 2, 0, 0);
                    break;
                case Axis.Y:
                    result.OverlapPoint0 = mainBounds.center - new Vector3(0, mainBounds.size.y / 2, 0);
                    result.OverlapPoint1 = mainBounds.center + new Vector3(0, mainBounds.size.y / 2, 0);
                    break;
                default:
                    result.OverlapPoint0 = mainBounds.center - new Vector3(0, 0, mainBounds.size.z / 2);
                    result.OverlapPoint1 = mainBounds.center + new Vector3(0, 0, mainBounds.size.z / 2);
                    break;
            }

            switch (result.SecondMaxAxis)
            {
                case Axis.X:
                    result.OverlapRadius = mainBounds.size.x / 2;
                    break;
                case Axis.Y:
                    result.OverlapRadius = mainBounds.size.y / 2;
                    break;
                default:
                    result.OverlapRadius = mainBounds.size.z / 2;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        private void OnDisable()
        {
            DynamicMaterialManager.ResetAllDynamicValues(gameObject);
        }
    }
}
#endif
