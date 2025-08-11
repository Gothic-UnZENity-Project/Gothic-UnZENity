#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using GUZ.Core.Marvin;
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
    public class VRPlayerWeaponInteraction : MonoBehaviour, IMarvinPropertyCollector
    {
        // FIXME - All of these values will be dynamic in the future. Based on skill level and weapon type.
        
        // if something in hand. Activate VRWeaponFighting
        [Header("Weapon Velocity Settings")]
        [SerializeField] private float _weaponVelocityThreshold = 2.0f;
        [SerializeField] private float _weaponVelocityDropPercentage = 0.1f; // 10% drop
        
        [Header("Weapon Attack Window Timing")]
        [SerializeField] private float _weaponAttackWindowTime = 1.0f;
        [SerializeField] private float _weaponComboWindowTime = 0.75f;
        [SerializeField] private float _weaponCooldownWindowTime = 2.0f;
        
        [SerializeField] private float _velocityCheckDuration = 0.5f;
        [SerializeField] private int _velocitySampleCount = 5;

        private VRPlayerWeaponAttackHandler _leftHandPlayerWeaponFightHandler;
        private VRPlayerWeaponAttackHandler _rightHandPlayerWeaponFightHandler;

        
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
                _leftHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(vobContainer.Go.GetComponentInChildren<Rigidbody>(), HVRHandSide.Left, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime, _velocityCheckDuration, _velocitySampleCount);
            else
                _rightHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(vobContainer.Go.GetComponentInChildren<Rigidbody>(), HVRHandSide.Right, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime, _velocityCheckDuration, _velocitySampleCount);
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

        public IEnumerable<object> CollectMarvinInspectorProperties()
        {
            return new List<object>
            {
                new MarvinPropertyHeader("Weapon Attack - Velocity"),
                new MarvinProperty<float>(
                    "Threshold",
                    () => _weaponVelocityThreshold,
                    value => _weaponVelocityThreshold = value,
                    0f, 10f),
                new MarvinProperty<float>(
                    "Drop Percentage",
                    () => _weaponVelocityDropPercentage,
                    value => _weaponVelocityDropPercentage = value,
                    0.05f, 0.5f),

                new MarvinPropertyHeader("Weapon Attack - Timing Windows"),
                new MarvinProperty<float>(
                    "Attack Window Time",
                    () => _weaponAttackWindowTime,
                    value => _weaponAttackWindowTime = value,
                    0f, 5f),
                new MarvinProperty<float>(
                    "Combo Window Time",
                    () => _weaponComboWindowTime,
                    value => _weaponComboWindowTime = value,
                    0f, 2f),
                new MarvinProperty<float>(
                    "Cooldown Window Time",
                    () => _weaponCooldownWindowTime,
                    value => _weaponCooldownWindowTime = value,
                    0f, 3f),

                new MarvinPropertyHeader("Weapon Attack - Velocity Check Settings"),
                new MarvinProperty<float>(
                    "Duration",
                    () => _velocityCheckDuration,
                    value => _velocityCheckDuration = value,
                    0.1f, 1f),
                new MarvinProperty<int>(
                    "Sample Count",
                    () => _velocitySampleCount,
                    value => _velocitySampleCount = value,
                    1, 20)
            };
        }
    }
}
#endif
