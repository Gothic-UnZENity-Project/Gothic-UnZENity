using GUZ.Core._Npc2;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToNpc : AbstractWalkAnimationAction
    {
        private Transform _destinationTransform;

        public GoToNpc(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            base.Start();

            _destinationTransform = Action.Instance0.GetUserData2().Go.transform;
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
