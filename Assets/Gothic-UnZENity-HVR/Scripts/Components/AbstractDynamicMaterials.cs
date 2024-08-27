using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public abstract class AbstractDynamicMaterials : MonoBehaviour
    {
        // Some objects (like NPCs) have multiple meshes. We therefore assume HVRFocus is added at level of first renderer.
        // Then we do a lookup into its children to show focus effect on all meshes.
        private List<Renderer> _renderers;
        private List<Material> _defaultMaterials;
        private List<Material> _dynamicMaterials;


        /// <summary>
        /// e.g. alter float values to change shader settings for this object.
        /// </summary>
        protected abstract void PrepareDynamicMaterial(Material mat);


        /// <summary>
        /// If we interact with this object for the first time, set its values.
        /// </summary>
        protected void InitiallyPrepareMaterials()
        {
            if (_renderers != null)
            {
                return;
            }

            _renderers = transform.GetComponentsInChildren<Renderer>().ToList();
            _defaultMaterials = transform.GetComponentsInChildren<Renderer>()
                .Select(i => i.sharedMaterial)
                .ToList();

            _dynamicMaterials = new();
            foreach (var mat in _defaultMaterials)
            {
                var newMaterial = new Material(Constants.ShaderSingleMeshLitDynamic)
                {
                    mainTexture = mat.mainTexture,
                    renderQueue = Constants.ShaderTypeTransparent
                };

                PrepareDynamicMaterial(newMaterial);

                _dynamicMaterials.Add(newMaterial);
            }
        }

        protected void ActivateDynamicMaterial()
        {
            for (var i = 0; i < _renderers.Count; i++)
            {
                _renderers[i].sharedMaterial = _dynamicMaterials[i];
            }
        }

        protected void DeactivateDynamicMaterial()
        {
            for (var i = 0; i < _renderers.Count; i++)
            {
                _renderers[i].sharedMaterial = _defaultMaterials[i];
            }
        }

        /// <summary>
        /// Reset everything (e.g. when GO is culled out.)
        /// </summary>
        protected virtual void OnDisable()
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
            _dynamicMaterials?.ForEach(Destroy);

            _renderers = null;
            _defaultMaterials = null;
            _dynamicMaterials = null;
        }
    }
}
