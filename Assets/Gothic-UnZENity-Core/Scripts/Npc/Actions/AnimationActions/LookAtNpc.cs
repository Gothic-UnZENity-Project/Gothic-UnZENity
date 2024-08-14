using System;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class LookAtNpc : AbstractAnimationAction
    {
        private const string _animationName = "S_TLOOK";

        private int OtherId => Action.Int0;
        private int OtherIndex => Action.Int1;

        private Transform NpcHeadTransform => Props.Head;
        private Quaternion _finalRotation;


        public LookAtNpc(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        public override void Start()
        {
            _finalRotation = GetDesiredHeadRotation();

            // Already aligned.
            if (Math.Abs(NpcHeadTransform.transform.eulerAngles.y - _finalRotation.y) < 1f)
            {
                IsFinishedFlag = true;
                return;
            }

            // Look animation. Will be used to transition from (e.g.) folded hands towards this animation later.
            AnimationCreator.PlayAnimation(Props.MdsNames, _animationName, NpcGo, true);
        }

        private Quaternion GetDesiredHeadRotation()
        {
            var destination = LookupCache.NpcCache[OtherIndex].properties.transform.position;
            var lookRotationVector = destination - NpcHeadTransform.position;
            var lookRotation = Quaternion.LookRotation(lookRotationVector);

            var currentNpcRotationEuler = NpcHeadTransform.rotation.eulerAngles;

            return Quaternion.Euler(currentNpcRotationEuler.x, lookRotation.eulerAngles.y, currentNpcRotationEuler.z);
        }

        public override void Tick()
        {
            base.Tick();

            HandleRotation();
        }

        /// <summary>
        /// Unfortunately it seems that G1 rotation animations have no root motions for the rotation (unlike walking).
        /// We therefore need to set it manually here.
        /// </summary>
        private void HandleRotation()
        {
            var currentRotation =
                Quaternion.RotateTowards(NpcHeadTransform.rotation, _finalRotation, Time.deltaTime * 100);

            // Check if rotation is done.
            if (Quaternion.Angle(NpcHeadTransform.rotation, _finalRotation) < 1f)
            {
                AnimationCreator.StopAnimation(NpcGo);
                IsFinishedFlag = true;
            }
            else
            {
                NpcHeadTransform.rotation = currentRotation;
            }
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);
            IsFinishedFlag = false;
        }
    }
}
