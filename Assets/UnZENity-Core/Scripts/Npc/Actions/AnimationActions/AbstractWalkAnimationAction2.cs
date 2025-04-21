using GUZ.Core._Npc2;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractWalkAnimationAction2 : AbstractAnimationAction
    {
        protected Transform NpcTransform => NpcGo.transform;

        protected AbstractWalkAnimationAction2(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
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

            // NPCs spawn on top of a WP. We need to inform the implementing class to act (e.g. alter destination WP)
            if (IsDestinationReached())
            {
                OnDestinationReached();
            }

            StartWalk();
        }

        public override void Tick()
        {
            base.Tick();

            if (IsFinishedFlag)
            {
                return;
            }

            if (IsDestinationReached())
            {
                OnDestinationReached();
            }

            HandleRotation();

        }

        private string GetWalkModeAnimationString()
        {
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    return "S_WALKL";
                case VmGothicEnums.WalkMode.Run:
                    return "S_RUNL";
                default:
                    Logger.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.", LogCat.Ai);
                    return "";
            }
        }

        protected virtual void StartWalk()
        {
            PhysicsHelper.EnablePhysicsForNpc(PrefabProps);

            var animName = GetWalkModeAnimationString();
            PrefabProps.AnimationSystem.PlayAnimation(animName);
        }

        private bool IsDestinationReached()
        {
            var npcPos = NpcTransform.position;
            var walkPos = GetWalkDestination();
            var npcDistPos = new Vector3(npcPos.x, walkPos.y, npcPos.z);

            var distance = Vector3.Distance(npcDistPos, walkPos);

            // FIXME - Scorpio is above FP, but values don't represent it.
            return distance < Constants.NpcDestinationReachedThreshold;
        }

        private void HandleRotation()
        {
            var destination = GetWalkDestination();
            var npcPos = NpcTransform.position;
            var sameHeightDirection = new Vector3(destination.x, npcPos.y, destination.z);
            var direction = (sameHeightDirection - npcPos);
            var destinationRotation = Quaternion.LookRotation(direction);
            NpcTransform.rotation = Quaternion.RotateTowards(NpcTransform.rotation, destinationRotation, Time.deltaTime * Constants.NpcRotationSpeed); 
        }

        /// <summary>
        /// We need to alter rootNode's position once walk animation is done.
        /// </summary>
        protected override void AnimationEnd()
        {
            base.AnimationEnd();

            // We need to ensure, that physics are always active when an NPC walks!
            PhysicsHelper.EnablePhysicsForNpc(PrefabProps);

            NpcTransform.localPosition = PrefabProps.Bip01.position;
            PrefabProps.Bip01.localPosition = Vector3.zero;
            PrefabProps.ColliderRootMotion.localPosition = Vector3.zero;

            // TODO - Needed?
            // root.SetLocalPositionAndRotation(
            //     root.localPosition + bip01Transform.localPosition,
            //     root.localRotation * bip01Transform.localRotation);
        }
    }
}
