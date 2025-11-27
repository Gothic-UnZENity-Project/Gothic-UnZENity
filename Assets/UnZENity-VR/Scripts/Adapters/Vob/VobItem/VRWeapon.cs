using GUZ.Core.Logging;
using GUZ.Core.Services.Meshes;
using Reflex.Attributes;
using UnityEngine;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.VR.Adapters.Vob.VobItem
{
    public class VRWeapon : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trailRenderer;

        [Inject] private TextureService _textureService;

        
        private void Start()
        {
            _trailRenderer.material = _textureService.WeaponTrailMaterial;
            SetTrailWidth();
        }
        
        /// <summary>
        /// Apply trail width based on Bounds of Weapon mesh.
        /// </summary>
        private void SetTrailWidth()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var bounds = meshFilter.sharedMesh.bounds;
                var weaponHeight = bounds.size.x;
                
                weaponHeight *= transform.parent.localScale.x;
                
                _trailRenderer.startWidth = weaponHeight;
                _trailRenderer.endWidth = weaponHeight;
                
                // Align the TrailRenderer position to the mesh bounds center
                _trailRenderer.transform.localPosition = bounds.center;
            }
            else
            {
                Logger.LogWarning("No MeshFilter found on weapon parent!", LogCat.Fight);
            }
        }
    }
}
