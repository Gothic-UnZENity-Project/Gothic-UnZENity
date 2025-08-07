#if GUZ_HVR_INSTALLED
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using GUZ.VR.Components.VobItem;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.VR.Components.Player
{
    public class VRPlayerWeaponInteraction : MonoBehaviour
    {
        // FIXME - All of these values will be dynamic in the future. Based on skill level and weapon type.
        
        // if something in hand. Activate VRWeaponFighting
        [Header("Weapon Velocity Settings")]
        [SerializeField] private float _weaponVelocityThreshold = 5.0f;
        [SerializeField] private float _weaponVelocityDropPercentage = 0.1f; // 10% drop
        
        [Header("Weapon Attack Window Timing")]
        [SerializeField] private float _weaponAttackWindowTime = 1.0f;
        [SerializeField] private float _weaponComboWindowTime = 0.5f;
        [SerializeField] private float _weaponCooldownWindowTime = 2.0f;
        
        private VRPlayerWeaponTimeHandler _leftHandPlayerWeaponFightHandler;
        private VRPlayerWeaponTimeHandler _rightHandPlayerWeaponFightHandler;

        
        public void OnGrabbed(HVRGrabberBase hand, HVRGrabbable item)
        {
            var vobItem = item.GetComponentInParent<VobLoader>();
            var vobContainer = vobItem?.Container;
            
            // We grab something that is not handled as VobItem.
            if (vobContainer == null || vobContainer.Vob.Type != VirtualObjectType.oCItem)
                return;

            // Currently we handle melee weapons only.
            if (vobContainer.GetItemInstance()!.MainFlag != (int)VmGothicEnums.ItemFlags.ItemKatNf)
                return;
            
            if (((HVRHandGrabber)hand).HandSide == HVRHandSide.Left)
                _leftHandPlayerWeaponFightHandler = new VRPlayerWeaponTimeHandler(null, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime);
            else
                _rightHandPlayerWeaponFightHandler = new VRPlayerWeaponTimeHandler(null, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime);
        }

        public void OnReleased(HVRGrabberBase hand, HVRGrabbable item)
        {
            if (((HVRHandGrabber)hand).HandSide == HVRHandSide.Left)
                _leftHandPlayerWeaponFightHandler = null;
            else
                _rightHandPlayerWeaponFightHandler = null;
        }
        
        private void FixedUpdate()
        {
            _leftHandPlayerWeaponFightHandler?.FixedUpdate();
            _rightHandPlayerWeaponFightHandler?.FixedUpdate();
        }
    }
}
#endif
