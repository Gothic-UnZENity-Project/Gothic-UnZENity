using GUZ.Core.Globals;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using TMPro;
using UnityEngine;

namespace GVR.HVR.Components
{
    public class HVRFocus : MonoBehaviour
    {
        private static readonly int _focusBrightness = Shader.PropertyToID("_FocusBrightness");
        private static Camera _mainCamera;

        [SerializeField] private VobProperties _properties;
        [SerializeField] private GameObject _nameCanvas;

        private Material _defaultMaterial;
        private Material _focusedMaterial;

        private bool _isHovered;

        private void Start()
        {
            _mainCamera = Camera.main;
            _nameCanvas.SetActive(false);
        }

        public void OnHoverEnter(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_defaultMaterial == null)
            {
                _defaultMaterial = transform.GetComponentInChildren<Renderer>().sharedMaterial;
                _focusedMaterial = new Material(_defaultMaterial);
                _focusedMaterial.SetFloat(_focusBrightness, Constants.ShaderPropertyFocusBrightness);
            }
            transform.GetComponentInChildren<Renderer>().sharedMaterial = _focusedMaterial;

            _nameCanvas.SetActive(true);
            _nameCanvas.GetComponentInChildren<TMP_Text>().text = _properties.name; // FIXME - Needs to be altered to the language agnostic name.
            _isHovered = true;
        }

        public void OnHoverExit(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            transform.GetComponentInChildren<Renderer>().sharedMaterial = _defaultMaterial;

            _nameCanvas.SetActive(false);
            _isHovered = false;
        }

        private void LateUpdate()
        {
            if (!_isHovered)
            {
                return;
            }

            _nameCanvas.transform.LookAt(_mainCamera.transform);
            _nameCanvas.transform.Rotate(0, 180, 0);
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
            _isHovered = false;
        }
    }
}
