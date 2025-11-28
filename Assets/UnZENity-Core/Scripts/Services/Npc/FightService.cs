using GUZ.Core.Adapters.Vob.Item;
using GUZ.Core.Domain.Npc.Actions.AnimationActions;
using GUZ.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Models.Container;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services.Vm;
using Reflex.Attributes;
using UnityEngine;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.Core.Services.Npc
{
    public class FightService
    {
        [Inject] private AudioService _audioService;
        [Inject] private VmService _vmService;
        [Inject] private AnimationService _animationService;

        public void Init()
        {
            GlobalEventDispatcher.FightHit.AddListener(OnHit);
        }
        
        public void OnHit(NpcContainer target, VobContainer _, Vector3 __)
        {
            Logger.LogEditor("Attack started!", LogCat.Fight);

            // Stop current (attack) animation.
            target.Props.CurrentAction.StopImmediately();

            var animName = _animationService.GetAnimationName(VmGothicEnums.AnimationType.StumbleA, target);
            target.Props.AnimationQueue.Enqueue(new PlayAni(new(animName), target));

            // In G1, Humans needs to have stumble sound called via Aargh svm.
            var clip = _audioService.GetRandomSoundClip($"SVM_{target.Instance.Voice}_AARGH");

            // Monsters will have their stumble sound inside animations itself.
            if (clip == null)
                return;

            target.PrefabProps.NpcSound.PlayOneShot(clip);
        }
    }
}
