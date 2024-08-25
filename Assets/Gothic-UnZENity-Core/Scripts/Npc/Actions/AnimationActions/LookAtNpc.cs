using System;
using System.Collections.Generic;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class LookAtNpc : AbstractAnimationAction
    {
        private const string _animationName = "T_LOOK";

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
            if (Quaternion.Angle(NpcHeadTransform.rotation, _finalRotation) < 1f)
            {
                IsFinishedFlag = true;
                NpcHeadTransform.rotation = _finalRotation;
            }
            // if(!IsFinishedFlag){
            //     AnimationCreator.StopAnimation(NpcGo);
            // }
            AnimationCreator.BlendAnimation(Props.MdsNames, GetWalkModeAnimationString(), NpcGo, true, new List<string> { "BIP01 HEAD" });
        }

        private Quaternion GetDesiredHeadRotation()
        {
            var destination = LookupCache.NpcCache[OtherIndex].properties.transform.position;
            var lookRotationVector = destination - NpcHeadTransform.position;
            var lookRotation = Quaternion.LookRotation(lookRotationVector);

            var currentNpcRotationEuler = NpcHeadTransform.rotation.eulerAngles;
            var desiredYRotation = lookRotation.eulerAngles.y;

            // Constrain the Y rotation within a reasonable range (e.g., -90 to 90 degrees relative to the body)
            var bodyYRotation = NpcGo.transform.rotation.eulerAngles.y;
            var relativeYRotation = Mathf.DeltaAngle(bodyYRotation, desiredYRotation);

            relativeYRotation = Mathf.Clamp(relativeYRotation, -70f, 70f);

            // Apply the constrained Y rotation back to the head
            return Quaternion.Euler(currentNpcRotationEuler.x, bodyYRotation + relativeYRotation, currentNpcRotationEuler.z);
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
            // Gradually rotate the head towards the target rotation
            var currentRotation = Quaternion.RotateTowards(NpcHeadTransform.rotation, _finalRotation, Time.deltaTime * 150);
            NpcHeadTransform.rotation = currentRotation;

            // Calculate the angle to the target rotation
            var angleToTarget = Quaternion.Angle(NpcHeadTransform.rotation, _finalRotation);

            // Stop the animation and finalize if the rotation is close enough to the target
            if (angleToTarget < 1f)
            {
                // Ensure that animation continues blending smoothly
                // AnimationCreator.StopAnimation(NpcGo);
                IsFinishedFlag = true;
            }
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);
            IsFinishedFlag = false;
        }
    }
}
