#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using TMPro;
using UnityEngine;

namespace GVR.HVR.Components
{
    /// <summary>
    /// Handles focus brightness and name visibility for VOBs and NPCs.
    ///
    /// Order of use is always:
    /// 1. HoverEnver -> We hover from far or near (e.g. grab distance without pulling towards us)
    /// 2. Grabbed -> Object is being Grabbed for movement/rotation
    /// 3. HoverExit -> We might still grab the object, but the Hover from our hand stops
    /// </summary>
    public class HVRFocus : MonoBehaviour
    {
        private static readonly int _focusBrightness = Shader.PropertyToID("_FocusBrightness");
        private static Camera _mainCamera;

        public bool ChangeKinematicOnGrab;
        [SerializeField] private VobProperties _properties;
        [SerializeField] private GameObject _nameCanvas;

        private Material _defaultMaterial;
        private Material _focusedMaterial;

        private bool _isHovered;

        private void Start()
        {
            _nameCanvas.SetActive(false);
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

            if (GameGlobals.Config.BrightenUpHoveredVOBs)
            {
                transform.GetComponentInChildren<Renderer>().sharedMaterial = _focusedMaterial;
            }

            if (GameGlobals.Config.ShowNamesOnHoveredVOBs)
            {
                _nameCanvas.SetActive(true);
                _nameCanvas.GetComponentInChildren<TMP_Text>().text = _properties.GetFocusName();
            }
            _isHovered = true;
        }

        // FIXME - Should later reside in another Component for Items only as the physical change is only for oCItems.
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (ChangeKinematicOnGrab)
            {
                // In Gothic, Items have no physics when lying around. We need to activate physics for HVR to properly move items in(to) our hands.
                transform.GetComponent<Rigidbody>().isKinematic = false;
            }
        }

        public void OnHoverExit(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameGlobals.Config.BrightenUpHoveredVOBs)
            {
                transform.GetComponentInChildren<Renderer>().sharedMaterial = _defaultMaterial;
            }

            _nameCanvas.SetActive(false);
            _isHovered = false;
        }

        private void LateUpdate()
        {
            if (!_isHovered)
            {
                return;
            }

            if (GameGlobals.Config.ShowNamesOnHoveredVOBs)
            {
                _nameCanvas.transform.LookAt(_mainCamera.transform);
                _nameCanvas.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        private void OnDisable()
        {
            // Reset material.
            var rend = transform.GetComponentInChildren<Renderer>();
            if (rend != null && _defaultMaterial != null)
            {
                transform.GetComponentInChildren<Renderer>().sharedMaterial = _defaultMaterial;
            }

            // We need to destroy our Material manually otherwise it won't be GC'ed by Unity (as stated in the docs).
            if (_focusedMaterial != null)
            {
                Destroy(_focusedMaterial);
            }

            _defaultMaterial = null;
            _focusedMaterial = null;
            _isHovered = false;
        }
    }
}
#endif
