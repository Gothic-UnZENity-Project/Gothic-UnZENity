#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
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
    public class HVRFocus : MonoBehaviour
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
            // GameObjects are loaded while Loading.scene is active (different Camera), but not the General.scene.
            // We therefore need to set the camera at this time earliest.
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;

                // Features also need to be fetched once only.
                _featureBrightenUp = GameGlobals.Config.BrightenUpHoveredVOBs;
                _featureShowName = GameGlobals.Config.ShowNamesOnHoveredVOBs;
            }

            if (_featureBrightenUp)
            {
                DynamicMaterialManager.SetDynamicValue(gameObject, Constants.ShaderPropertyFocusBrightness, Constants.ShaderPropertyFocusBrightnessValue);
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
                DynamicMaterialManager.ResetDynamicValue(gameObject, Constants.ShaderPropertyFocusBrightness, Constants.ShaderPropertyFocusBrightnessDefault);
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

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        private void OnDisable()
        {
            DynamicMaterialManager.ResetAllDynamicValues(gameObject);

            _isHovered = false;
        }
    }
}
#endif
