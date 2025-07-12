using GUZ.Core.Data.Container;
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
        protected bool IsDestReached;

        protected AbstractWalkAnimationAction2(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        protected virtual void OnDestinationReached()
        {
            StopWalk();
        }

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
                OnDestinationReached();
            // Do not rotate when a destination is reached this frame. Either rotate next frame (e.g. GoToWP.nextRoute) or stop it fully.
            else
                HandleRotation();
        }

        private string GetWalkModeAnimationString()
        {
            var walkMode = (VmGothicEnums.WalkMode)Vob.AiHuman.WalkMode;
            switch (walkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    return "S_WALKL";
                case VmGothicEnums.WalkMode.Run:
                    return "S_RUNL";
                default:
                    Logger.LogWarning($"Animation of type {walkMode} not yet implemented.", LogCat.Ai);
                    return "";
            }
        }

        protected virtual void StartWalk()
        {
            PhysicsHelper.EnablePhysicsForNpc(PrefabProps);

            var animName = GetWalkModeAnimationString();
            PrefabProps.AnimationSystem.PlayAnimation(animName);
        }

        protected virtual void StopWalk()
        {
            PhysicsHelper.EnablePhysicsForNpc(PrefabProps);

            var animName = GetWalkModeAnimationString();
            PrefabProps.AnimationSystem.StopAnimation(animName);
        }

        private bool IsDestinationReached()
        {
            var npcPos = NpcTransform.position;
            var walkPos = GetWalkDestination();
            var npcDistPos = new Vector3(npcPos.x, walkPos.y, npcPos.z);

            var distance = Vector3.Distance(npcDistPos, walkPos);

            // FIXME - Scorpio is above FP, but values don't represent it.
            if (distance < Constants.NpcDestinationReachedThreshold)
            {
                IsDestReached = true;
            }

            return IsDestReached;
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
    }
}
