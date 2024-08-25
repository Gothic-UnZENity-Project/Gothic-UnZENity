using GUZ.Core.Caches;
using GUZ.Core.Data.ZkEvents;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class TurnToNpc : AbstractRotateAnimationAction
    {
        private int OtherId => Action.Int0;
        private int OtherIndex => Action.Int1;

        public TurnToNpc(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            var destinationTransform = LookupCache.NpcCache[OtherIndex].properties.transform;
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

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }
    }
}
