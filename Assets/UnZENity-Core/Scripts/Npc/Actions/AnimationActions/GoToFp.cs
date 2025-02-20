using GUZ.Core._Npc2;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Manager;
using GUZ.Core.Vob.WayNet;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToFp : AbstractWalkAnimationAction
    {
        private FreePoint _fp;

        private string Destination => Action.String0;

        private FreePoint _freePoint;

        public GoToFp(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            base.Start();

            var npcPos = NpcGo.transform.position;
            _fp = WayNetHelper.FindNearestFreePoint(npcPos, Destination);
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);

            IsFinishedFlag = false;
        }

        protected override Vector3 GetWalkDestination()
        {
            return _fp.Position;
        }

        protected override void OnDestinationReached()
        {
            Props.CurrentFreePoint = _fp;
            _fp.IsLocked = true;

            AnimationEndEventCallback(new SerializableEventEndSignal(""));

            State = WalkState.Done;
            IsFinishedFlag = true;
        }
    }
}
