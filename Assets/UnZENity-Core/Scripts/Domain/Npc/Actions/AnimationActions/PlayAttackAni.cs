using GUZ.Core.Extensions;
using GUZ.Core.Models.Container;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Basically PlayAni with special Attack handling.
    /// </summary>
    public class PlayAttackAni : PlayAni
    {
        private FightAiMove _move => (FightAiMove)Action.Int0;

        public PlayAttackAni(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Tick()
        {
            base.Tick();

            if (_move == FightAiMove.Run)
            {
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
}
