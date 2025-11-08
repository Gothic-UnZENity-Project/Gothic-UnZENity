using GUZ.Core.Adapters.Vob;
using GUZ.Core.Const;
using GUZ.VR.Services;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    public class VRWeaponAttackCollider : MonoBehaviour
    {
        [Inject]
        private readonly VRWeaponService _vrWeaponService;
            
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != Constants.VobItemLayer)
                return;


            var vobContainer = other.GetComponentInParent<VobLoader>()?.Container;
            if (!_vrWeaponService.IsWeaponInAttackWindow(vobContainer))
                return;

            _vrWeaponService.HitDone(vobContainer);
            
            

        }
    }
}
