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
            if (Quaternion.Angle(NpcGo.transform.rotation, _finalRotation) < 1f)
            {
                IsFinishedFlag = true;
                return;
            }

            // https://discussions.unity.com/t/determining-whether-to-rotate-left-or-right/44021
            var cross = Vector3.Cross(NpcGo.transform.forward, _finalRotation.eulerAngles);
            _isRotateLeft = cross.y >= 0;

            if (Quaternion.Angle(NpcGo.transform.rotation, _finalRotation) > 1f)
            {
                AnimationCreator.BlendAnimation(Props.MdsNames, GetRotateModeAnimationString(), NpcGo, true);
            }
        }

        private string GetRotateModeAnimationString()
        {
            string walkmode;
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkmode = "RUN"; // TODO: aniAlias not read properly from mds
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkmode = "Run";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkmode = "Sneak";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkmode = "Water";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkmode = "Swim";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkmode = "Dive";
                    break;
                default:
                    Debug.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.");
                    return "";
            }

            return $"T_{walkmode}TURN{(_isRotateLeft ? 'L': 'R')}";
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
            if (Quaternion.Angle(npcTransform.rotation, _finalRotation) < 1f && IsFinishedFlag != true)
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
