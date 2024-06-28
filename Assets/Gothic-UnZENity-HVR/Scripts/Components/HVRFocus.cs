using GUZ.Core.Globals;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GVR.HVR.Components
{
    public class HVRFocus : MonoBehaviour
    {
        private static readonly int _focusBrightness = Shader.PropertyToID("_FocusBrightness");

        private Material _defaultMaterial;
        private Material _focusedMaterial;

        public void OnHoverEnter(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_defaultMaterial == null)
            {
                _defaultMaterial = transform.GetComponentInChildren<Renderer>().sharedMaterial;
                _focusedMaterial = new Material(_defaultMaterial);
                _focusedMaterial.SetFloat(_focusBrightness, Constants.ShaderPropertyFocusBrightness);
            }

            transform.GetComponentInChildren<Renderer>().sharedMaterial = _focusedMaterial;
        }

        public void OnHoverExit(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            transform.GetComponentInChildren<Renderer>().sharedMaterial = _defaultMaterial;
        }

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        private void OnDisable()
        {
            transform.GetComponentInChildren<Renderer>().sharedMaterial = _defaultMaterial;

            // We need to destroy our Material manually otherwise it won't be GC'ed by Unity (as stated in the docs).
            Destroy(_focusedMaterial);

            _defaultMaterial = null;
            _focusedMaterial = null;
        }
    }
}
