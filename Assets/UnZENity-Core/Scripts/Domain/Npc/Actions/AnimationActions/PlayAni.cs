using GUZ.Core.Extensions;
using GUZ.Core.Models.Container;
using UnityEngine;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public class PlayAni : AbstractAnimationAction
    {
        private string _animName => Action.String0;

        public PlayAni(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var animFound = PrefabProps.AnimationSystem.PlayAnimation(_animName);

            if (!animFound)
            {
                IsFinishedFlag = true;
                return;
            }
            ActionEndEventTime = PrefabProps.AnimationSystem.GetAnimationDuration(_animName);
        }

        // FIXME - Move to PlayAttackAni() to distinguish easily between normal animations and special attack ones.
        public override void Tick()
        {
            base.Tick();
            if (Action.Instance0 == null)
                return;

            var myPosition = NpcContainer.Go.transform.position;
            var targetPosition = Action.Instance0.GetUserData()!.Go.transform.position;

            // Consider only horizontal distance (ignore Y-axis)
            var myPositionHorizontal = new Vector3(myPosition.x, 0, myPosition.z);
            var targetPositionHorizontal = new Vector3(targetPosition.x, 0, targetPosition.z);
            var distance = Vector3.Distance(myPositionHorizontal, targetPositionHorizontal);

            if (distance <= 1f)
            {
                PrefabProps.AnimationSystem.StopAllAnimations();
                IsFinishedFlag = true;
            }
        }
    }
}
