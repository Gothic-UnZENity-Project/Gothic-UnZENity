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
            var temp = destinationTransform.position - NpcGo.transform.position;
            return Quaternion.LookRotation(temp, Vector3.up);
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }
    }
}
