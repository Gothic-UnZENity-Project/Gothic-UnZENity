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

            Logger.LogEditor("Attack started!", LogCat.VR);
            
            _vrWeaponService.HitDone(vobContainer);
            
            var animName = _animationService.GetAnimationName(VmGothicEnums.AnimationType.StumbleA, _npcContainer);
            _npcContainer.Props.CurrentAction.StopImmediately();
            _npcContainer.Props.AnimationQueue.Enqueue(new PlayAni(new(animName), _npcContainer));
        }
    }
}
