using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToNpc : AbstractWalkAnimationAction
    {
        private Transform _destinationTransform;

        public GoToNpc(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            base.Start();

            _destinationTransform = Action.Instance0.GetUserData().Go.transform;
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
