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
        private int _otherId => Action.Int0;
        private int _otherIndex => Action.Int1;

        private Transform _npcHeadTransform => Props.Head;
        private Quaternion _finalRotation;


        public LookAtNpc(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        { }

        public override void Start()
        {
            _finalRotation = GetDesiredHeadRotation();

            // Already aligned.
            if (Quaternion.Angle(_npcHeadTransform.rotation, _finalRotation) < 1f)
            {
                IsFinishedFlag = true;
                _npcHeadTransform.rotation = _finalRotation;
            }

            AnimationCreator.BlendAnimation(Props.MdsNames, GetWalkModeAnimationString(), NpcGo, true, new List<string> { "BIP01 HEAD" });
        }

        private Quaternion GetDesiredHeadRotation()
        {
            var destination = LookupCache.NpcCache[_otherIndex].properties.transform.position;
            var lookRotationVector = destination - _npcHeadTransform.position;
            var lookRotation = Quaternion.LookRotation(lookRotationVector);

            var currentNpcRotationEuler = _npcHeadTransform.rotation.eulerAngles;
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
            var currentRotation = Quaternion.RotateTowards(_npcHeadTransform.rotation, _finalRotation, Time.deltaTime * 150);
            _npcHeadTransform.rotation = currentRotation;

            // Calculate the angle to the target rotation
            var angleToTarget = Quaternion.Angle(_npcHeadTransform.rotation, _finalRotation);

            // Stop the animation and finalize if the rotation is close enough to the target
            if (angleToTarget < 1f)
            {
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
