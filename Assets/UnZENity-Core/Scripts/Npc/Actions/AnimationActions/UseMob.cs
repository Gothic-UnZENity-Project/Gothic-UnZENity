using System.Linq;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core.Vob;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class UseMob : AbstractWalkAnimationAction2
    {
        private const string _mobTransitionAnimationString = "T_{0}{1}{2}_2_{3}";
        private const string _mobLoopAnimationString = "S_{0}_S{1}";
        private GameObject _mobGo;
        private GameObject _slotGo;
        private Vector3 _destination;

        private string _schemeName => Action.String0;
        private int _desiredState => Action.Int0;
        private bool IsStopUsingMob => _desiredState <= -1;

        private bool _isMobFoundButNotYetInitialized;
        private string _currentMobAnimation;


        public UseMob(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            // NPC is already interacting with a Mob, we therefore assume it's a change of state (e.g. -1 to stop Mob usage)
            if (Props.BodyState == VmGothicEnums.BodyState.BsMobinteract)
            {
                _mobGo = PrefabProps.CurrentInteractable;
                _slotGo = PrefabProps.CurrentInteractableSlot;

                StartMobUseAnimation();
                return;
            }

            // Else: We have a new animation where we seek the Mob before walking towards and executing action.
            var go = GetNearestMob();
            _mobGo = go;

            if (go == null)
            {
                IsFinishedFlag = true;
                return;
            }
            
            if (go.GetComponent<VobLoader>().IsLoaded)
                StartNow();
            else
                StartDelayed();
        }

        private void StartNow()
        {
            // We call Start only if the Mobsi is already available.
            base.Start();

            _isMobFoundButNotYetInitialized = false;

            var slot = GetNearestMobSlot();

            if (slot == null)
            {
                IsFinishedFlag = true;
                return;
            }

            _slotGo = slot;
            _destination = _slotGo.transform.position;

            PrefabProps.CurrentInteractable = _mobGo;
            PrefabProps.CurrentInteractableSlot = _slotGo;

            SetBodyState();
        }

        private void SetBodyState()
        {
            var mobsiScheme = _mobGo.GetComponentInChildren<VobProperties>().VisualScheme;

            if (Constants.Daedalus.MobSit.Contains(mobsiScheme))
                Props.BodyState = VmGothicEnums.BodyState.BsSit;
            else if (Constants.Daedalus.MobLie.Contains(mobsiScheme))
                Props.BodyState = VmGothicEnums.BodyState.BsLie;
            else if (Constants.Daedalus.MobClimb.Contains(mobsiScheme))
                Props.BodyState = VmGothicEnums.BodyState.BsClimb;
            else if (Constants.Daedalus.MobNotInterruptable.Contains(mobsiScheme))
                Props.BodyState = VmGothicEnums.BodyState.BsMobinteract;
            else
                Props.BodyState = VmGothicEnums.BodyState.BsMobinteractInterrupt;
        }

        /// <summary>
        /// We need to wait until it's there...
        /// </summary>
        private void StartDelayed()
        {
            _isMobFoundButNotYetInitialized = true;
        }
        
        public override void Tick()
        {
            if (_isMobFoundButNotYetInitialized)
            {
                if (!_mobGo.GetComponent<VobLoader>().IsLoaded)
                {
                    return;
                }
                else
                {
                    StartNow();
                }
            }

            
            if (IsDestReached)
            {
                TickMobUsage();
            }
            else
            {
                base.Tick();
            }
        }

        private void TickMobUsage()
        {
            if (PrefabProps.AnimationSystem.IsPlaying(_currentMobAnimation))
                return;
            
            UpdateState();

            // If we arrived at the Mobsi, we will further execute the transitions step-by-step until demanded state is reached.
            if (Props.CurrentInteractableStateId != _desiredState)
            {
                PlayTransitionAnimation();
                return;
            }

            // Mobsi isn't in use any longer
            if (Props.CurrentInteractableStateId == -1)
            {
                PrefabProps.CurrentInteractable = null;
                PrefabProps.CurrentInteractableSlot = null;
                Props.BodyState = VmGothicEnums.BodyState.BsStand;

                PhysicsHelper.EnablePhysicsForNpc(PrefabProps);
            }
            // Loop Mobsi animation until the same UseMob with -1 is called.
            else
            {
                var mobVisualName = _mobGo.GetComponentInChildren<VobProperties>().VisualScheme;
                var animName = string.Format(_mobLoopAnimationString, mobVisualName, _desiredState);
                PrefabProps.AnimationSystem.PlayAnimation(animName);
            }

            IsFinishedFlag = true;
        }

        [CanBeNull]
        private GameObject GetNearestMob()
        {
            var pos = NpcGo.transform.position;
            return VobHelper.GetFreeInteractableWithin10M(pos, Action.String0).go;
        }

        [CanBeNull]
        private GameObject GetNearestMobSlot()
        {
            if (_mobGo == null)
                return null;

            var pos = NpcGo.transform.position;
            var slot = VobHelper.GetNearestSlot(_mobGo, pos);

            return slot;
        }

        protected override void OnDestinationReached()
        {
            base.OnDestinationReached();
            
            StartMobUseAnimation();
        }

        private void StartMobUseAnimation()
        {
            // Place item for Mobsi usage in hand - if needed. Will be "spawned" via animation >EventType.ItemInsert< later.
            var itemName = ((InteractiveObject)_mobGo.GetComponentInChildren<VobProperties>().Properties).Item;
            if (itemName.NotNullOrEmpty())
            {
                var item = VmInstanceManager.TryGetItemData(itemName);
                Props.CurrentItem = item!.Index;
            }
            
            PhysicsHelper.DisablePhysicsForNpc(PrefabProps);

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

        private void UpdateState()
        {
            // FIXME - We need to check. For Cauldron/Cook we have only t_s0_2_Stand, but not t_s1_2_s0 - But is it for all of them?
            if (IsStopUsingMob)
            {
                Props.CurrentInteractableStateId = -1;
                Props.CurrentItem = -1;
            }
            else
            {
                var newStateAddition = Props.CurrentInteractableStateId > _desiredState ? -1 : +1;
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

            var mobVisualName = _mobGo.GetComponentInChildren<VobProperties>().VisualScheme;
            var slotPositionName = GetSlotPositionTag(_slotGo.name);
            var animName = string.Format(_mobTransitionAnimationString, mobVisualName, slotPositionName, from, to);

            _currentMobAnimation = animName;
            PrefabProps.AnimationSystem.PlayAnimation(animName);
        }
    }
}
