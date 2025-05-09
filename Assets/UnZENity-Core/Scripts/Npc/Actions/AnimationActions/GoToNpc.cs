using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToNpc : AbstractWalkAnimationAction
    {
        private Transform _destinationTransform;

        public GoToNpc(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
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


        protected override void AnimationEnd()
        {
            base.AnimationEnd();

            IsFinishedFlag = false;
        }

        protected override void OnDestinationReached()
        {
            base.OnDestinationReached();

            AnimationEnd();

            State = WalkState.Done;
            IsFinishedFlag = true;
        }
    }
}
