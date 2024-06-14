using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Manager;
using GUZ.Core.World.WayNet;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToFp : AbstractWalkAnimationAction
    {
        private FreePoint fp;

        private string destination => Action.String0;

        private FreePoint freePoint;

        public GoToFp(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        public override void Start()
        {
            base.Start();

            var npcPos = NpcGo.transform.position;
            fp = WayNetHelper.FindNearestFreePoint(npcPos, destination);
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }

        protected override Vector3 GetWalkDestination()
        {
            return fp.Position;
        }

        protected override void OnDestinationReached()
        {
            Props.CurrentFreePoint = fp;
            fp.IsLocked = true;

            AnimationEndEventCallback(new SerializableEventEndSignal(nextAnimation: ""));

            walkState = WalkState.Done;
            IsFinishedFlag = true;
        }
    }
}
