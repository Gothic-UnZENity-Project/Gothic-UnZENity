#if GUZ_HVR_INSTALLED
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.VR.Domain.Player;
using GUZ.VR.Model.Vob;
using HurricaneVR.Framework.Shared;
using Reflex.Attributes;

namespace GUZ.VR.Services
{
    /// <summary>
    /// Logic goes like this:
    /// * At first, our logic is executed in _firstAttackDomain. Either one handed or two handed.
    /// * If we grab another weapon which is not handled by the first Domain already, then let's handle it by the _second one.
    /// </summary>
    public class VRWeaponService
    {
        private readonly VrWeaponAttackDomain _firstAttackDomain = new VrWeaponAttackDomain().Inject();
        private readonly VrWeaponAttackDomain _secondAttackDomain = new VrWeaponAttackDomain().Inject();


        public void OnGrabbed(HVRHandSide handSide, VobContainer vobContainer, WeaponPhysicsConfig weaponConfig)
        {
            if (!_firstAttackDomain.TryHandle(vobContainer, weaponConfig, handSide))
            {
                // If we can't handle with the first handler, then it's a second weapon grabbed with another hand.
                _secondAttackDomain.TryHandle(vobContainer, weaponConfig, handSide);
            }
        }

        public void OnReleased(HVRHandSide handSide, WeaponPhysicsConfig weaponConfig)
        {
            if (!_firstAttackDomain.TryUnHandle(weaponConfig, handSide))
            {
                // If we can't handle with the first handler, then it's a second weapon released from another hand.
                _secondAttackDomain.TryUnHandle(weaponConfig, handSide);
            }
        }

        public void FixedUpdate()
        {
            _firstAttackDomain.FixedUpdate();
            _secondAttackDomain.FixedUpdate();
        }
    }
}
#endif
