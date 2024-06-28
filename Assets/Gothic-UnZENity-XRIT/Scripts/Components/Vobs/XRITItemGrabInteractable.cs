using GUZ.Core;
using GUZ.Core.Manager.Culling;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace GUZ.XRIT.Components.Vobs
{
    [RequireComponent(typeof(Rigidbody))]
    public class XRITItemGrabInteractable : MonoBehaviour
    {
        public GameObject attachPoint1;
        public GameObject attachPoint2;

        public Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void SelectEntered(SelectEnterEventArgs args)
        {
            GameGlobals.MeshCulling.StartTrackVobPositionUpdates(gameObject);
        }

        /// <summary>
        /// Activate physics on object immediately after it's stopped being grabbed
        /// </summary>
        public void SelectExited(SelectExitEventArgs args)
        {
            if (rb.isKinematic)
                rb.isKinematic = false;
            GameGlobals.MeshCulling.StopTrackVobPositionUpdates(gameObject);
        }
    }
}
