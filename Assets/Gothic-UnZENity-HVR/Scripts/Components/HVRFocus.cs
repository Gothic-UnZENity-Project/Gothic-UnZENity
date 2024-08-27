#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.HVR.Components;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using TMPro;
using UnityEngine;

namespace GVR.HVR.Components
{
    /// <summary>
    /// Handles focus brightness and name visibility for VOBs and NPCs.
    /// We leverage HVRGrabbable's events to show canvas of object name and alter brightness on all objects' mesh renderers.
    ///
    /// Order of use is always:
    /// 1. HoverEnver -> We hover from far or near (e.g. grab distance without pulling towards us)
    /// 2. Grabbed -> Object is being Grabbed for movement/rotation
    /// 3. HoverExit -> We might still grab the object, but the Hover from our hand stops
    /// </summary>
    public class HVRFocus : AbstractDynamicMaterials
    {
        private static Camera _mainCamera;

        private static bool _featureBrightenUp;
        private static bool _featureShowName;

        [SerializeField] private AbstractProperties _properties;
        [SerializeField] private GameObject _nameCanvas;

        private bool _isHovered;


        private void Start()
        {
            _nameCanvas.SetActive(false);
        }

        public void OnHoverEnter(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            InitiallyPrepareMaterials();

            // GameObjects are loaded while Loading.scene is active (different Camera), but not the General.scene.
            // We therefore need to set the camera at this time earliest.
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;

                // Features also need to be fetched once only.
                _featureBrightenUp = GameGlobals.Config.BrightenUpHoveredVOBs;
                _featureShowName = GameGlobals.Config.ShowNamesOnHoveredVOBs;
            }

            // If the item is currently being grabbed, just stop execution of the shader.
            // This is a convenience feature as the dynamic shader can handle both: Alpha+FocusBrightness.
            // But it's easier for now to have only one component handling the shader value changes.
            if (grabbable.TryGetComponent(out HVRVobItem itemComp) && itemComp.GrabCount > 0)
            {
                return;
            }

            if (_featureBrightenUp)
            {
                ActivateDynamicMaterial();
            }

            if (_featureShowName)
            {
                _nameCanvas.GetComponentInChildren<TMP_Text>().text = _properties.GetFocusName();
                _nameCanvas.SetActive(true);
            }

            _isHovered = true;
        }

        public void OnHoverExit(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_featureBrightenUp)
            {
                DeactivateDynamicMaterial();
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

            if (_featureShowName)
            {
                _nameCanvas.transform.LookAt(_mainCamera.transform);
                _nameCanvas.transform.Rotate(0, 180, 0);
            }
        }

        protected override void PrepareDynamicMaterial(Material mat)
        {
            mat.SetFloat(
                Constants.ShaderPropertyFocusBrightness,
                Constants.ShaderPropertyFocusBrightnessValue);
        }

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            _isHovered = false;
        }
    }
}
#endif
