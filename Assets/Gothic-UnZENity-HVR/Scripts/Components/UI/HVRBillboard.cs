using UnityEngine;

namespace GUZ.HVR.Components.UI
{
    public class HVRBillboard : MonoBehaviour
    {
        private Transform _cameraTransform;

        // Inversion is needed for UI elements to show text correctly.
        private Quaternion yAxisInversion = Quaternion.Euler(0f, 180f, 0f);

        private void Start()
        {
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main!.transform;
            }
        }

        private void Update()
        {
            // Calculate the direction to look at
            var directionToLookAt = _cameraTransform.position - transform.position;

            // Create a rotation quaternion that ignores y-axis.
            var rotation = Quaternion.LookRotation(new Vector3(directionToLookAt.x, 0f, directionToLookAt.z));

            transform.rotation = rotation * yAxisInversion;
        }
    }
}
