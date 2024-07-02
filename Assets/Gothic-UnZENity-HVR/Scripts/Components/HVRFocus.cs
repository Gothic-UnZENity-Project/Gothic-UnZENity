using GUZ.Core.Globals;
using GUZ.Core.Vob;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using TMPro;
using UnityEngine;

namespace GVR.HVR.Components
{
    /// <summary>
    /// Handles focus brightness and name visibility for VOBs and NPCs.
    /// </summary>
    public class HVRFocus : MonoBehaviour
    {
        private static readonly int _focusBrightness = Shader.PropertyToID("_FocusBrightness");
        private static Camera _mainCamera;

        [SerializeField] private VobItemProperties _properties;
        [SerializeField] private GameObject _nameCanvas;

        private Material _defaultMaterial;
        private Material _focusedMaterial;

        private bool _isHovered;

        private void Start()
        {
            _nameCanvas.SetActive(false);
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items in(to) our hands.
            transform.GetComponent<Rigidbody>().isKinematic = false;
        }

        public void OnHoverEnter(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_defaultMaterial == null)
            {
                // Items are loaded while Loading.scene is active (different Camera), but not the General.scene. We therefore need to set the camera at this point.
                _mainCamera = Camera.main;

                _defaultMaterial = transform.GetComponentInChildren<Renderer>().sharedMaterial;
                _focusedMaterial = new Material(_defaultMaterial);
                _focusedMaterial.SetFloat(_focusBrightness, Constants.ShaderPropertyFocusBrightness);
            }
            transform.GetComponentInChildren<Renderer>().sharedMaterial = _focusedMaterial;

            _nameCanvas.SetActive(true);
            _nameCanvas.GetComponentInChildren<TMP_Text>().text = _properties.ItemData.Name;
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
