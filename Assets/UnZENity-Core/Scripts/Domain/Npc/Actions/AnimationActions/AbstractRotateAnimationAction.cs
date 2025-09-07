using GUZ.Core.Models.Container;
using GUZ.Core.Const;
using GUZ.Core.Logging;
using GUZ.Core.Models.Vm;
using GUZ.Core.Services;
using GUZ.Core.Services.Npc;
using GUZ.Core.Services.Vm;
using GUZ.Core.Util;
using Reflex.Attributes;
using UnityEngine;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public abstract class AbstractRotateAnimationAction : AbstractAnimationAction
    {
        [Inject] private readonly AnimationService _animationService;
        [Inject] private readonly GameStateService _gameStateService;

        // Can be used to rotate without animation.
        protected bool PlayAnimation = true;

        private Quaternion _finalRotation;
        private bool _isRotateLeft;

        private Transform NpcHeadTransform => PrefabProps.Head;

        protected AbstractRotateAnimationAction(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        /// <summary>
        /// We need to define the final direction within overriding class.
        /// </summary>
        protected abstract Quaternion GetRotationDirection();

        private Quaternion GetDesiredHeadRotation()
        {
            // Get the current forward direction of the NPC's body
            var currentBodyForwardDirection = NpcGo.transform.TransformDirection(Vector3.forward);

            // Calculate the desired rotation for the head to look in the current body's forward direction
            var desiredHeadRotation = Quaternion.LookRotation(currentBodyForwardDirection);

            // Adjust the desired head rotation to prevent the head from resting on the shoulder
            desiredHeadRotation *= Quaternion.Euler(0f, -30f, 90f); // Reset pitch and roll

            return desiredHeadRotation;
        }


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

            if (Quaternion.Angle(NpcGo.transform.rotation, _finalRotation) > 1f && PlayAnimation)
            {
                PrefabProps.AnimationSystem.PlayAnimation(GetRotateModeAnimationString());
            }
        }

        private string GetRotateModeAnimationString()
        {
            var walkMode = (VmGothicEnums.WalkMode)Vob.AiHuman.WalkMode;
            string walkModeString;
            switch (walkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkModeString = "RUN"; // FIXME: We need to implement aniAlias feature, then change it back to t_WalkTurnL
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkModeString = "Run";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkModeString = "Sneak";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkModeString = "Water";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkModeString = "Swim";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkModeString = "Dive";
                    break;
                default:
                    Logger.LogWarning($"Animation of type {walkMode} not yet implemented.", LogCat.Ai);
                    return "";
            }

            return $"T_{walkModeString}TURN{(_isRotateLeft ? 'L' : 'R')}";
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
            var turnSpeed = _gameStateService.GuildValues.GetTurnSpeed((int)VmService.Guild.GIL_HUMAN);
            var currentRotation =
                Quaternion.RotateTowards(npcTransform.rotation, _finalRotation, Time.deltaTime * turnSpeed);

            // Check if rotation is done.
            if (Quaternion.Angle(npcTransform.rotation, _finalRotation) < 1f && IsFinishedFlag != true)
            {
                PrefabProps.AnimationSystem.StopAnimation(GetRotateModeAnimationString());
                PrefabProps.AnimationSystem.PlayAnimation(_animationService.GetAnimationName(VmGothicEnums.AnimationType.Move, Vob));

                IsFinishedFlag = true;
            }
            else
            {
                npcTransform.rotation = currentRotation;

                // Many monsters (e.g. Bloodflies) have no head.
                if (NpcHeadTransform)
                    NpcHeadTransform.rotation = GetDesiredHeadRotation();
            }
        }

        protected override void AnimationEnd()
        {
            base.AnimationEnd();
            IsFinishedFlag = false;
        }
    }
}
