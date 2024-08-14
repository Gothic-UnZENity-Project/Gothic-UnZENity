using System;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractRotateAnimationAction : AbstractAnimationAction
    {
        private Quaternion _finalRotation;
        private bool _isRotateLeft;

        protected AbstractRotateAnimationAction(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        /// <summary>
        /// We need to define the final direction within overriding class.
        /// </summary>
        protected abstract Quaternion GetRotationDirection();

        public override void Start()
        {
            _finalRotation = GetRotationDirection();

            // Already aligned.
            if (Math.Abs(NpcGo.transform.eulerAngles.y - _finalRotation.y) < 1f)
            {
                IsFinishedFlag = true;
                return;
            }

            // https://discussions.unity.com/t/determining-whether-to-rotate-left-or-right/44021
            var cross = Vector3.Cross(NpcGo.transform.forward, _finalRotation.eulerAngles);
            _isRotateLeft = cross.y >= 0;

            AnimationCreator.PlayAnimation(Props.MdsNames, GetRotateModeAnimationString(), NpcGo, true);
        }

        private string GetRotateModeAnimationString()
        {
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    return _isRotateLeft ? "T_WALKTURNL" : "T_WALKTURNR";
                default:
                    Debug.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.");
                    return "";
            }
        }

        public override void Tick()
        {
            base.Tick();

            HandleRotation(NpcGo.transform);
        }

        /// <summary>
        /// Unfortunately it seems that G1 rotation animations have no root motions for the rotation (unlike walking).
        /// We therefore need to set it manually here.
        /// </summary>
        private void HandleRotation(Transform npcTransform)
        {
            var currentRotation =
                Quaternion.RotateTowards(npcTransform.rotation, _finalRotation, Time.deltaTime * 100);

            // Check if rotation is done.
            if (Quaternion.Angle(npcTransform.rotation, _finalRotation) < 1f)
            {
                AnimationCreator.StopAnimation(NpcGo);
                IsFinishedFlag = true;
            }
            else
            {
                npcTransform.rotation = currentRotation;
            }
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);
            IsFinishedFlag = false;
        }
    }
}
