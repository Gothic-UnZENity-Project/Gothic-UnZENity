using GUZ.Core;
using GUZ.Core.Adapters.Npc;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Const;
using GUZ.Core.Models.Container;
using GUZ.Core.Services.Npc;
using GUZ.VR.Services;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    public class VRWeaponAttackCollider : MonoBehaviour
    {
        [Inject] private readonly VRWeaponService _vrWeaponService;
        [Inject] private readonly AnimationService _animationService;

        private NpcContainer _npcContainer;
        
        private void Start()
        {
            _npcContainer = GetComponentInParent<NpcLoader>().Container;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != Constants.VobItemLayer)
                return;

            var vobContainer = other.GetComponentInParent<VobLoader>()?.Container;

            // DEBUG
            var hitPosition0 = other.ClosestPoint(transform.position);
            GlobalEventDispatcher.FightHit.Invoke(_npcContainer, vobContainer, hitPosition0);
            
            if (!_vrWeaponService.IsWeaponInAttackWindow(vobContainer))
                return;

            var hitPosition = other.ClosestPoint(transform.position);
            GlobalEventDispatcher.FightHit.Invoke(_npcContainer, vobContainer, hitPosition);
        }
    }
}
