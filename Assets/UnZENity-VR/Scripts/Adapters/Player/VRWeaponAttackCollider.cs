using GUZ.Core.Adapters.Npc;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Const;
using GUZ.Core.Domain.Npc.Actions.AnimationActions;
using GUZ.Core.Logging;
using GUZ.Core.Models.Container;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services.Npc;
using GUZ.VR.Services;
using Reflex.Attributes;
using UnityEngine;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.VR.Adapters.Player
{
    public class VRWeaponAttackCollider : MonoBehaviour
    {
        [Inject] private readonly VRWeaponService _vrWeaponService;
        [Inject] private readonly AnimationService _animationService;
        [Inject] private readonly FightService _fightService;

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

            if (!_vrWeaponService.IsWeaponInAttackWindow(vobContainer))
                return;


            _fightService.ExecuteHit(_npcContainer);
            _vrWeaponService.HitDone(vobContainer);
        }
    }
}
