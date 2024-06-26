using System;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractRotateAnimationAction : AbstractAnimationAction
    {
        private const float _rotationSpeed = 5f;

        private Quaternion _finalDirection;
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
            _finalDirection = GetRotationDirection();

            // Already aligned.
            if (Math.Abs(NpcGo.transform.eulerAngles.y - _finalDirection.y) < 1f)
            {
                IsFinishedFlag = true;
                return;
            }

            // https://discussions.unity.com/t/determining-whether-to-rotate-left-or-right/44021
            var cross = Vector3.Cross(NpcGo.transform.forward, _finalDirection.eulerAngles);
            _isRotateLeft = cross.y >= 0;

            AnimationCreator.PlayAnimation(Props.MdsNames, GetRotateModeAnimationString(), NpcGo, true);
        }

        private string GetRotateModeAnimationString()
        {
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    return _isRotateLeft ? "T_WALKWTURNL" : "T_WALKWTURNR";
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
                Quaternion.Slerp(npcTransform.rotation, _finalDirection, Time.deltaTime * _rotationSpeed);

            // Check if rotation is done.
            if (Quaternion.Angle(npcTransform.rotation, currentRotation) < 1f)
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
