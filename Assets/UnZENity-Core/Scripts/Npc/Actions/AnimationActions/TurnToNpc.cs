using GUZ.Core._Npc2;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class TurnToNpc : AbstractRotateAnimationAction
    {
        private int OtherId => Action.Int0;
        private int OtherIndex => Action.Int1;

        public TurnToNpc(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            var destinationTransform = Action.Instance0.GetUserData2().Go.transform;
            // var temp = destinationTransform.position - NpcGo.transform.position;
            // return Quaternion.LookRotation(temp, Vector3.up);
            // }
            var direction = destinationTransform.position - NpcGo.transform.position;

            // Ensure the direction only affects horizontal rotation (Y-axis)
            direction.y = 0;

            // Check if the direction vector is not zero, to prevent zero-length rotation issues
            if (direction.sqrMagnitude > 0.0001f)
            {
                // Return the rotation required to face the player, constrained to the Y-axis
                return Quaternion.LookRotation(direction, Vector3.up);
            }
            else
            {
                // If the player is directly at the same position, maintain the current rotation
                return NpcGo.transform.rotation;
            }
        }

        protected override void AnimationEnd()
        {
            base.AnimationEnd();

            IsFinishedFlag = false;
        }
    }
}
