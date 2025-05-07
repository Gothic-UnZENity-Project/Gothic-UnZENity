using GUZ.Core.Data.Container;
using GUZ.Core.Manager;
using GUZ.Core.Vob.WayNet;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class GoToFp : AbstractWalkAnimationAction
    {
        private FreePoint _fp;

        private string Destination => Action.String0;

        private FreePoint _freePoint;

        public GoToFp(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            base.Start();

            var npcPos = NpcGo.transform.position;
            _fp = WayNetHelper.FindNearestFreePoint(npcPos, Destination);

            if (_fp == null)
            {
                IsFinishedFlag = true;
                return;
            }

            _fp.IsLocked = true;
            Props.CurrentFreePoint = _fp;
        }

        protected override void AnimationEnd()
        {
            base.AnimationEnd();

            IsFinishedFlag = false;
        }

        protected override Vector3 GetWalkDestination()
        {
            return _fp.Position;
        }

        protected override void OnDestinationReached()
        {
            Props.CurrentFreePoint = _fp;

            AnimationEnd();

            State = WalkState.Done;
            IsFinishedFlag = true;
        }
    }
}
