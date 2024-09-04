using System;
using System.Linq;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class UseMob : AbstractWalkAnimationAction
    {
        private const string _mobTransitionAnimationString = "T_{0}{1}{2}_2_{3}";
        private const string _mobLoopAnimationString = "S_{0}_S{1}";
        private GameObject _mobGo;
        private GameObject _slotGo;
        private Vector3 _destination;

        private bool IsStopUsingMob => Action.Int0 <= -1;

        public UseMob(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            base.Start();

            // NPC is already interacting with a Mob, we therefore assume it's a change of state (e.g. -1 to stop Mob usage)
            if (Props.BodyState == VmGothicEnums.BodyState.BsMobinteract)
            {
                _mobGo = Props.CurrentInteractable;
                _slotGo = Props.CurrentInteractableSlot;

                StartMobUseAnimation();
                return;
            }

            // Else: We have a new animation where we seek the Mob before walking towards and executing action.
            var mob = GetNearestMob();
            var slot = GetNearestMobSlot(mob);

            if (slot == null)
            {
                IsFinishedFlag = true;
                return;
            }

            _mobGo = mob;
            _slotGo = slot;
            _destination = _slotGo.transform.position;

            Props.CurrentInteractable = _mobGo;
            Props.CurrentInteractableSlot = _slotGo;
            Props.BodyState = VmGothicEnums.BodyState.BsMobinteract;
        }

        [CanBeNull]
        private GameObject GetNearestMob()
        {
            var pos = NpcGo.transform.position;
            return VobHelper.GetFreeInteractableWithin10M(pos, Action.String0)?.gameObject;
        }

        [CanBeNull]
        private GameObject GetNearestMobSlot(GameObject mob)
        {
            if (mob == null)
            {
                return null;
            }

            var pos = NpcGo.transform.position;
            var slot = VobHelper.GetNearestSlot(mob.gameObject, pos);

            return slot;
        }

        protected override void OnDestinationReached()
        {
            StartMobUseAnimation();
        }

        private void StartMobUseAnimation()
        {
            State = WalkState.Done;
            PhysicsHelper.DisablePhysicsForNpc(Props);

            // AnimationCreator.StopAnimation(NpcGo);
            NpcGo.transform.SetPositionAndRotation(_slotGo.transform.position, _slotGo.transform.rotation);

            PlayTransitionAnimation();
        }

        private string GetSlotPositionTag(string name)
        {
            if (name.EndsWithIgnoreCase("_FRONT"))
            {
                return "_FRONT_";
            }

            if (name.EndsWithIgnoreCase("_BACK"))
            {
                return "_BACK_";
            }

            return "_";
        }

        protected override Vector3 GetWalkDestination()
        {
            return _destination;
        }

        /// <summary>
        /// Only after the Mob is reached and final transition animation is done, we will finalize this Action.
        /// </summary>
        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            base.AnimationEndEventCallback(eventData);
            IsFinishedFlag = false;

            if (State != WalkState.Done)
            {
                return;
            }

            UpdateState();

            // If we arrived at the Mobsi, we will further execute the transitions step-by-step until demanded state is reached.
            if (Props.CurrentInteractableStateId != Action.Int0)
            {
                PlayTransitionAnimation();
                return;
            }

            // Mobsi isn't in use any longer
            if (Props.CurrentInteractableStateId == -1)
            {
                Props.CurrentInteractable = null;
                Props.CurrentInteractableSlot = null;
                Props.BodyState = VmGothicEnums.BodyState.BsStand;

                PhysicsHelper.EnablePhysicsForNpc(Props);
            }
            // Loop Mobsi animation until the same UseMob with -1 is called.
            else
            {
                var mobVisualName = _mobGo.GetComponent<VobProperties>().VisualScheme;
                var animName = string.Format(_mobLoopAnimationString, mobVisualName, Action.Int0);
                AnimationCreator.PlayAnimation(Props.MdsNames, animName, NpcGo, true);
            }

            IsFinishedFlag = true;
        }

        private void UpdateState()
        {
            // FIXME - We need to check. For Cauldron/Cook we have only t_s0_2_Stand, but not t_s1_2_s0 - But is it for all of them?
            if (IsStopUsingMob)
            {
                Props.CurrentInteractableStateId = -1;
            }
            else
            {
                var newStateAddition = Props.CurrentInteractableStateId > Action.Int0 ? -1 : +1;
                Props.CurrentInteractableStateId += newStateAddition;
            }
        }

        private void PlayTransitionAnimation()
        {
            string from;
            string to;

            // FIXME - We need to check. For Cauldron/Cook we have only t_s0_2_Stand, but not t_s1_2_s0 - But is it for all of them?
            if (IsStopUsingMob)
            {
                from = "S0";
                to = "Stand";
            }
            else
            {
                from = Props.CurrentInteractableStateId.ToString();
                to = $"S{Props.CurrentInteractableStateId + 1}";

                from = from switch
                {
                    "-1" => "Stand",
                    _ => $"S{from}"
                };
            }

            var mobVisualName = _mobGo.GetComponent<VobProperties>().VisualScheme;
            var slotPositionName = GetSlotPositionTag(_slotGo.name);
            var animName = string.Format(_mobTransitionAnimationString, mobVisualName, slotPositionName, from, to);

            AnimationCreator.PlayAnimation(Props.MdsNames, animName, NpcGo);
        }

        protected override void InsertItem(string slot1, string slot2)
        {
            if (slot2.Any())
            {
                throw new Exception("Slot 2 is set but not yet handled by InsertItem as AnimationEvent.");
            }

            var slotGo = NpcGo.FindChildRecursively(slot1);
            var item = ((InteractiveObject)_mobGo.GetComponent<VobProperties>().Properties).Item;
            VobCreator.CreateItemMesh(item, slotGo);

            Props.UsedItemSlot = slot1;
        }
    }
}
