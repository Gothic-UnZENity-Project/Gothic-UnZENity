using GUZ.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace GUZ.XRIT.Components.Vobs
{
    [RequireComponent(typeof(Rigidbody))]
    public class XritItemGrabInteractable : MonoBehaviour
    {
        [FormerlySerializedAs("attachPoint1")] public GameObject AttachPoint1;
        [FormerlySerializedAs("attachPoint2")] public GameObject AttachPoint2;

        [FormerlySerializedAs("rb")] public Rigidbody Rb;

        private void Start()
        {
            Rb = GetComponent<Rigidbody>();
        }

        public void SelectEntered(SelectEnterEventArgs args)
        {
            GameGlobals.VobMeshCulling.StartTrackVobPositionUpdates(gameObject);
        }

        /// <summary>
        /// Activate physics on object immediately after it's stopped being grabbed
        /// </summary>
        public void SelectExited(SelectExitEventArgs args)
        {
            if (Rb.isKinematic)
            {
                Rb.isKinematic = false;
            }

            GameGlobals.VobMeshCulling.StopTrackVobPositionUpdates(gameObject);
        }
    }
}
