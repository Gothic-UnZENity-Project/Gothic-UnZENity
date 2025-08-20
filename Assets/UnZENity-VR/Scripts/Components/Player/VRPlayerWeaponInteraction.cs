#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using GUZ.Core.Data.Container;
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

        private VobContainer _leftHandWeapon;
        private VobContainer _rightHandWeapon;

        private const int _twoHandedFlags = (int)VmGothicEnums.ItemFlags.Item2HdAxe | (int)VmGothicEnums.ItemFlags.Item2HdSwd;

        
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

            var rigidBody = vobContainer.Go.GetComponentInChildren<Rigidbody>();
            // Logger.LogWarning("Grabbed - " + ((HVRHandGrabber)hand).HandSide, LogCat.VR);
            if (((HVRHandGrabber)hand).HandSide == HVRHandSide.Left)
            {
                _leftHandWeapon = vobContainer;

                // Check if we already have a weaponFightHandler for this weapon active.
                if (_rightHandWeapon == _leftHandWeapon)
                {
                    // Only add information that we hold weapons in two hands now.
                    _rightHandPlayerWeaponFightHandler.AddLeftHand();
                    _leftHandPlayerWeaponFightHandler = _rightHandPlayerWeaponFightHandler;
                }
                else
                {
                    // Item is in first hand.
                    _leftHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(rigidBody, HVRHandSide.Left, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime, _velocityCheckDuration, _velocitySampleCount);
                }
            }
            else
            {
                _rightHandWeapon = vobContainer;

                // Check if we already have a weaponFightHandler for this weapon active.
                if (_leftHandWeapon == _rightHandWeapon)
                {
                    // Only add information that we hold weapons in two hands now.
                    _leftHandPlayerWeaponFightHandler.AddRightHand();
                    _rightHandPlayerWeaponFightHandler = _leftHandPlayerWeaponFightHandler;
                }
                else
                {
                    _rightHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(rigidBody, HVRHandSide.Right, _weaponVelocityThreshold, _weaponVelocityDropPercentage, _weaponAttackWindowTime, _weaponComboWindowTime, _weaponCooldownWindowTime, _velocityCheckDuration, _velocitySampleCount);
                }
            }
            
            AlterWeaponWeights();
        }

        public void OnReleased(HVRGrabberBase hand, HVRGrabbable item)
        {
            // Logger.LogWarning("Released - " + ((HVRHandGrabber)hand).HandSide, LogCat.VR);
            if (((HVRHandGrabber)hand).HandSide == HVRHandSide.Left)
            {
                _leftHandPlayerWeaponFightHandler?.RemoveLeftHand();
                _leftHandWeapon = null;

                // If we grabbed the item twice before, we simply change the hand which is the "owning" hand of attack movement.
                if (_rightHandWeapon == _leftHandWeapon)
                {
                    _rightHandPlayerWeaponFightHandler = _leftHandPlayerWeaponFightHandler;
                }

                _leftHandPlayerWeaponFightHandler = null;
            }
            else
            {
                _rightHandPlayerWeaponFightHandler?.RemoveRightHand();
                _rightHandWeapon = null;

                // If we grabbed the item twice before, we simply change the hand which is the "owning" hand of attack movement.
                if (_leftHandWeapon == _rightHandWeapon)
                {
                    _leftHandPlayerWeaponFightHandler = _rightHandPlayerWeaponFightHandler;
                }

                _rightHandPlayerWeaponFightHandler = null;
            }

            AlterWeaponWeights();
        }

        /// <summary>
        /// Set mass of weapons based on 1HD / 2HD types and amount of hands holding it.
        ///
        /// FIXME - Mass values (2/25) are hard coded. We need to make them dynamic inside INI.
        /// </summary>
        private void AlterWeaponWeights()
        {
            // We have one weapon in both hands
            if (_leftHandWeapon != null && _leftHandWeapon == _rightHandWeapon)
            {
                _leftHandWeapon.Go.GetComponentInChildren<Rigidbody>().mass = 2;
                return;
            }

            if (_leftHandWeapon != null)
            {
                var is2HD = (_leftHandWeapon.GetItemInstance().Flags & _twoHandedFlags) != 0;
                _leftHandWeapon.Go.GetComponentInChildren<Rigidbody>().mass = is2HD ? 25 : 2;
            }

            if (_rightHandWeapon != null)
            {
                var is2HD = (_rightHandWeapon.GetItemInstance().Flags & _twoHandedFlags) != 0;
                _rightHandWeapon.Go.GetComponentInChildren<Rigidbody>().mass = is2HD ? 25 : 2;
            }
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
