#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
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
    public class HVRFocus : MonoBehaviour
    {
        private static Camera _mainCamera;

        private static bool _featureBrightenUp;
        private static bool _featureShowName;

        [SerializeField] private AbstractProperties _properties;
        [SerializeField] private GameObject _nameCanvas;

        // Some objects (like NPCs) have multiple meshes. We therefore assume HVRFocus is added at level of first renderer.
        // Then we do a lookup into its children to show focus effect on all meshes.
        private List<Renderer> _renderers;
        private List<Material> _defaultMaterials;
        private List<Material> _focusedMaterials;

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

            // If we hover this object for the first time, set its values.
            if (_renderers == null)
            {
                _renderers = transform.GetComponentsInChildren<Renderer>().ToList();
                _defaultMaterials = transform.GetComponentsInChildren<Renderer>()
                    .Select(i => i.sharedMaterial)
                    .ToList();

                _focusedMaterials = new();
                foreach (var mat in _defaultMaterials)
                {
                    var newFocusMaterial = new Material(Constants.ShaderSingleMeshLitDynamic)
                    {
                        mainTexture = mat.mainTexture,
                        renderQueue = Constants.ShaderTypeTransparent
                    };

                    newFocusMaterial.SetFloat(
                        Constants.ShaderPropertyFocusBrightness,
                        Constants.ShaderPropertyFocusBrightnessValue);

                    _focusedMaterials.Add(newFocusMaterial);
                }
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
                for (var i = 0; i < _renderers.Count; i++)
                {
                    _renderers[i].sharedMaterial = _focusedMaterials[i];
                }
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
                for (var i = 0; i < _renderers.Count; i++)
                {
                    _renderers[i].sharedMaterial = _defaultMaterials[i];
                }
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
            // Reset material.
            if (_renderers != null)
            {
                for (var i = 0; i < _renderers.Count; i++)
                {
                    _renderers[i].sharedMaterial = _defaultMaterials[i];
                }
            }

            // We need to destroy our Material manually otherwise it won't be GC'ed by Unity (as stated in the docs).
            if (_focusedMaterials != null)
            {
                _focusedMaterials.ForEach(Destroy);
            }

            _renderers = null;
            _defaultMaterials = null;
            _focusedMaterials = null;
            _isHovered = false;
        }
    }
}
#endif
