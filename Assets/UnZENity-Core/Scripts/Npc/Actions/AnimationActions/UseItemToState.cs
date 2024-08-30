using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class UseItemToState : AbstractAnimationAction
    {
        private const string _animationScheme = "T_{0}_{1}_2_{2}";
        private const string _loopAnimationScheme = "S_{0}_S{1}";

        private int ItemToUse => Action.Int0;
        private int DesiredState => Action.Int1;


        public UseItemToState(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            PlayTransitionAnimation();
        }

        private void PlayTransitionAnimation()
        {
            var oldItemAnimationState = Props.ItemAnimationState;
            int newItemAnimationState;
            if (DesiredState > Props.ItemAnimationState)
            {
                Props.HasItemEquipped = true;
                Props.CurrentItem = ItemToUse;
                newItemAnimationState = ++Props.ItemAnimationState;
            }
            else
            {
                Props.HasItemEquipped = false;
                Props.CurrentItem = -1;
                // e.g. Babe brush doesn't call it automatically. We therefore need to force remove the brush item from hand.
                // AnimationEventCallback(new() { Type = ZenKit.EventType.ItemDestroy });
                newItemAnimationState = --Props.ItemAnimationState;
            }

            var item = VmInstanceManager.TryGetItemData(ItemToUse);
            var oldState = oldItemAnimationState == -1 ? "STAND" : $"S{oldItemAnimationState}";
            var newState = newItemAnimationState == -1 ? "STAND" : $"S{newItemAnimationState}";

            // e.g. T_POTION_STAND_2_S0
            var animationName = string.Format(_animationScheme, item.SchemeName, oldState, newState);

            var animationFound = AnimationCreator.PlayAnimation(Props.MdsNames, animationName, NpcGo);

            // e.g. BABE-T_BRUSH_S1_2_S0.man doesn't exist, but we can skip and use next one (S0_2_Stand)
            if (!animationFound)
            {
                // Go on with next animation.
                PlayTransitionAnimation();
            }
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            if (Props.ItemAnimationState == DesiredState)
            {
                return;
            }

            PlayTransitionAnimation();

            IsFinishedFlag = false;
        }
    }
}
