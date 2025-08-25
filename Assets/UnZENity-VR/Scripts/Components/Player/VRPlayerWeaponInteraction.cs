#if GUZ_HVR_INSTALLED
using System;
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
        [Header("Weapon weight")]
        [SerializeField] private float _mass2HOneHanded = 15f;
        [SerializeField] private float _mass1HAnyHand2HTwoHanded = 2f;
        [SerializeField] private float _linearDamping2HOneHanded = 0f;
        [SerializeField] private float _linearDamping1HAnyHand2HTwoHanded = 0f;
        [SerializeField] private float _angularDamping2HOneHanded = 0f;
        [SerializeField] private float _angularDamping1HAnyHand2HTwoHanded = 0f;
        
        
        // if something in hand. Activate VRWeaponFighting
        [Header("Weapon Velocity Settings")]
        [SerializeField] private float _weaponVelocityThreshold = 2.0f;
        [SerializeField] private float _weaponVelocityDropPercentage = 0.1f; // 10% drop
        
        [Header("Weapon Attack Velocity Settings")]
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
                    _leftHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(
                        rigidBody, Is2HD(_leftHandWeapon), HVRHandSide.Left, _weaponVelocityThreshold,
                        _weaponVelocityDropPercentage, _velocityCheckDuration, _velocitySampleCount);
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
                    _rightHandPlayerWeaponFightHandler = new VRPlayerWeaponAttackHandler(
                        rigidBody, Is2HD(_rightHandWeapon), HVRHandSide.Right, _weaponVelocityThreshold,
                        _weaponVelocityDropPercentage, _velocityCheckDuration, _velocitySampleCount);
                }
            }
            
            AlterWeaponWeights();
        }

        private void OnDrawGizmos()
        {
            if (_rightHandPlayerWeaponFightHandler == null)
                return;

            foreach (var weaponCollider in _rightHandWeapon.Go.GetComponentsInChildren<Collider>())
            {
                switch (weaponCollider)
                {
                    case BoxCollider boxCollider:
                        _rightHandPlayerWeaponFightHandler.CalculateBoxColliderOverlap(boxCollider, out var point, out var size, out var rotation);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawCube(point, rotation * size);
                        
                        break;
                    case CapsuleCollider capsuleCollider:
                        _rightHandPlayerWeaponFightHandler.CalculateCapsuleOverlap(capsuleCollider, out var point0, out var point1, out var radius);
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(point0, radius);
                        Gizmos.DrawWireSphere(point1, radius);

                        break;
                }
            }
        }

        private bool Is2HD(VobContainer weapon)
        {
            return (weapon.GetItemInstance().Flags & _twoHandedFlags) != 0;
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
        /// </summary>
        private void AlterWeaponWeights()
        {
            var leftRigidbody = _leftHandWeapon?.Go.GetComponentInChildren<Rigidbody>();
            var rightRigidbody = _rightHandWeapon?.Go.GetComponentInChildren<Rigidbody>();
            
            // We have one weapon in both hands
            if (_leftHandWeapon != null && _leftHandWeapon == _rightHandWeapon)
            {
                leftRigidbody!.mass = _mass1HAnyHand2HTwoHanded;
                leftRigidbody.linearDamping = _linearDamping1HAnyHand2HTwoHanded;
                leftRigidbody.angularDamping = _angularDamping1HAnyHand2HTwoHanded;
                return;
            }

            if (_leftHandWeapon != null)
            {
                var is2HD = Is2HD(_leftHandWeapon);
                leftRigidbody!.mass = is2HD ? _mass2HOneHanded : _mass1HAnyHand2HTwoHanded;
                leftRigidbody.linearDamping = is2HD ? _linearDamping2HOneHanded : _linearDamping1HAnyHand2HTwoHanded;
                leftRigidbody.angularDamping = is2HD ? _angularDamping2HOneHanded : _angularDamping1HAnyHand2HTwoHanded;
            }

            if (_rightHandWeapon != null)
            {
                var is2HD = Is2HD(_rightHandWeapon);
                rightRigidbody!.mass = is2HD ? _mass2HOneHanded : _mass1HAnyHand2HTwoHanded;
                rightRigidbody.linearDamping = is2HD ? _linearDamping2HOneHanded : _linearDamping1HAnyHand2HTwoHanded;
                rightRigidbody.angularDamping = is2HD ? _angularDamping2HOneHanded : _angularDamping1HAnyHand2HTwoHanded;
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
                new MarvinPropertyHeader("VRWeapon - RigidBody settings"),
                new MarvinProperty<float>(
                    "Mass one handed",
                    () => _mass2HOneHanded,
                    value => _mass2HOneHanded = value,
                    0f, 50f),
                new MarvinProperty<float>(
                    "Mass two handed",
                    () => _mass1HAnyHand2HTwoHanded,
                    value => _mass1HAnyHand2HTwoHanded = value,
                    0f, 50f),
                new MarvinProperty<float>(
                    "Move damping one handed",
                    () => _linearDamping2HOneHanded,
                    value => _linearDamping2HOneHanded = value,
                    0f, 25f),
                new MarvinProperty<float>(
                    "Move damping two handed",
                    () => _linearDamping1HAnyHand2HTwoHanded,
                    value => _linearDamping1HAnyHand2HTwoHanded = value,
                    0f, 25f),
                new MarvinProperty<float>(
                    "Rotation damping one handed",
                    () => _angularDamping2HOneHanded,
                    value => _angularDamping2HOneHanded = value,
                    0f, 25f),
                new MarvinProperty<float>(
                    "Rotation damping two handed",
                    () => _angularDamping1HAnyHand2HTwoHanded,
                    value => _angularDamping1HAnyHand2HTwoHanded = value,
                    0f, 25f),
                
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
