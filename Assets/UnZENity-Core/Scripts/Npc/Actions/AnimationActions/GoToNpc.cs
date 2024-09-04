using GUZ.Core.Caches;
using GUZ.Core.Data.ZkEvents;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToNpc : AbstractWalkAnimationAction
    {
        private Transform _destinationTransform;
        private int OtherId => Action.Int0;
        private int OtherIndex => Action.Int1;

        public GoToNpc(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            base.Start();

            _destinationTransform = LookupCache.NpcCache[OtherIndex].properties.transform;
        }

        protected override Vector3 GetWalkDestination()
        {
            return _destinationTransform.position;
        }


        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }

        protected override void OnDestinationReached()
        {
            AnimationEndEventCallback(new SerializableEventEndSignal(""));

            State = WalkState.Done;
            IsFinishedFlag = true;
        }
    }
}
