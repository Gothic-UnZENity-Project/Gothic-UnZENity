using System;
using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    [Obsolete("Successor is AbstractWalkAnimationAction2, but it needs to be tested with (1)GoToFp, (2)GoToNpc, (3)UseMob, (4)GoToNextFp first.")]
    public abstract class AbstractWalkAnimationAction : AbstractAnimationAction
    {
        protected enum WalkState
        {
            Initial,
            Rotate,
            Walk,
            WalkAndRotate, // If we're already walking and a new WP is the destination, we walk and rotate together.
            Done
        }

        protected WalkState State = WalkState.Initial;

        protected AbstractWalkAnimationAction(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        protected abstract void OnDestinationReached();

        /// <summary>
        /// We need to define the final destination spot within overriding class.
        /// </summary>
        protected abstract Vector3 GetWalkDestination();

        public override void Start()
        {
            base.Start();

            PhysicsHelper.EnablePhysicsForNpc(PrefabProps);
        }

        public override void Tick()
        {
            base.Tick();

            if (IsFinishedFlag)
            {
                return;
            }

            switch (State)
            {
                case WalkState.Initial:
                    State = WalkState.Rotate;
                    HandleRotation(NpcGo.transform, GetWalkDestination(), false);
                    return;
                case WalkState.Rotate:
                    HandleRotation(NpcGo.transform, GetWalkDestination(), false);
                    return;
                case WalkState.Walk:
                    HandleWalk(PrefabProps.ColliderRootMotion.transform);
                    return;
                case WalkState.WalkAndRotate:
                    HandleRotation(NpcGo.transform, GetWalkDestination(), true);
                    return;
                case WalkState.Done:
                    return; // NOP
                default:
                    Logger.Log($"MovementState {State} not yet implemented.", LogCat.Ai);
                    return;
            }
        }

        private string GetWalkModeAnimationString()
        {
            var weaponState = Props.WeaponState == VmGothicEnums.WeaponState.NoWeapon
                ? ""
                : Props.WeaponState.ToString();
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    return $"S_{weaponState}WALKL";
                case VmGothicEnums.WalkMode.Run:
                    return $"S_{weaponState}RUNL";
                default:
                    Logger.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.", LogCat.Ai);
                    return "";
            }
        }

        private void StartWalk()
        {
            var animName = GetWalkModeAnimationString();
            PrefabProps.AnimationSystem.PlayAnimation(animName);

            State = WalkState.Walk;
        }

        private void HandleWalk(Transform transform)
        {
            var npcPos = transform.position;
            var walkPos = GetWalkDestination();
            var npcDistPos = new Vector3(npcPos.x, walkPos.y, npcPos.z);

            var distance = Vector3.Distance(npcDistPos, walkPos);

            // FIXME - Scorpio is above FP, but values don't represent it.
            if (distance < Constants.NpcDestinationReachedThreshold)
            {
                OnDestinationReached();
            }
        }

        private void HandleRotation(Transform transform, Vector3 destination, bool includesWalking)
        {
            var pos = transform.position;
            var sameHeightDirection = new Vector3(destination.x, pos.y, destination.z);
            var direction = (sameHeightDirection - pos).normalized;
            var dot = Vector3.Dot(direction, transform.forward);
            var lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * 100);

            // Stop the rotation and start walking.
            if (Math.Abs(dot - 1f) < 0.0001f)
            {
                State = WalkState.Walk;

                // If we didn't walk so far, we do it now.
                if (!includesWalking)
                {
                    StartWalk();
                }
            }
        }

        /// <summary>
        /// We need to alter rootNode's position once walk animation is done.
        /// </summary>
        protected override void AnimationEnd()
        {
            base.AnimationEnd();

            // We need to ensure, that physics still apply when an animation is looped.
            if (State != WalkState.Done)
            {
                PhysicsHelper.EnablePhysicsForNpc(PrefabProps);
            }

            NpcGo.transform.localPosition = PrefabProps.Bip01.position;
            PrefabProps.Bip01.localPosition = Vector3.zero;
            PrefabProps.ColliderRootMotion.localPosition = Vector3.zero;

            // TODO - Needed?
            // root.SetLocalPositionAndRotation(
            //     root.localPosition + bip01Transform.localPosition,
            //     root.localRotation * bip01Transform.localRotation);
        }
    }
}
