using System;
using System.Linq;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;
using EventType = ZenKit.EventType;
using Object = UnityEngine.Object;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractAnimationAction
    {
        protected readonly AnimationAction Action;
        protected readonly GameObject NpcGo;
        protected readonly NpcProperties Props;

        protected bool IsFinishedFlag;

        public AbstractAnimationAction(AnimationAction action, GameObject npcGo)
        {
            Action = action;
            NpcGo = npcGo;
            Props = npcGo.GetComponent<NpcProperties>();
        }

        public virtual void Start()
        {
            // By default every Daedalus aninmation starts without using physics. But they can always overwrite it (e.g.) for walking.
            PhysicsHelper.DisablePhysicsForNpc(Props);
        }

        public string GetWalkModeAnimationString()
        {
            string walkmode;
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkmode = "WALK";
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkmode = "RUN";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkmode = "SNEAK";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkmode = "WATER";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkmode = "SWIM";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkmode = "DIVE";
                    break;
                default:
                    Debug.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.");
                    return "";
            }

            return $"S_{walkmode}";
        }

        /// <summary>
        /// We just set the audio by default.
        /// </summary>
        public virtual void AnimationSfxEventCallback(SerializableEventSoundEffect sfxData)
        {
            var clip = VobHelper.GetSoundClip(sfxData.Name);
            Props.NpcSound.clip = clip;
            Props.NpcSound.maxDistance = sfxData.Range.ToMeter();
            Props.NpcSound.Play();

            if (sfxData.EmptySlot)
            {
                Debug.LogWarning($"PxEventSfxData.emptySlot not yet implemented: {sfxData.Name}");
            }
        }

        public virtual void AnimationEventCallback(SerializableEventTag data)
        {
            switch (data.Type)
            {
                case EventType.ItemInsert:
                    InsertItem(data.Slot1, data.Slot2);
                    break;
                case EventType.ItemDestroy:
                case EventType.ItemRemove:
                    RemoveItem();
                    break;
                case EventType.TorchInventory:
                    Debug.Log(
                        "EventType.inventory_torch: I assume this means: if torch is in inventory, then put it out. But not really sure. Need a NPC with real usage of it to predict right.");
                    break;
                default:
                    Debug.LogWarning($"EventType.type {data.Type} not yet supported.");
                    break;
            }
        }

        public virtual void AnimationMorphEventCallback(SerializableEventMorphAnimation data)
        {
            var type = Props.HeadMorph.GetAnimationTypeByName(data.Animation);

            Props.HeadMorph.StartAnimation(Props.BodyData.Head, type);
        }

        protected virtual void InsertItem(string slot1, string slot2)
        {
            if (slot2.Any())
            {
                throw new Exception("Slot 2 is set but not yet handled by InsertItem as AnimationEvent.");
            }

            var slotGo = NpcGo.FindChildRecursively(slot1);

            VobCreator.CreateItemMesh(Props.CurrentItem, slotGo);

            Props.UsedItemSlot = slot1;
        }

        private void RemoveItem()
        {
            // Some animations need to force remove items, some not.
            if (Props.UsedItemSlot == "")
            {
                return;
            }

            var slotGo = NpcGo.FindChildRecursively(Props.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }

        /// <summary>
        /// Most of our animations are fine if we just set this flag and return it via IsFinished()
        /// If an animation has also a next animation set, we will call it automatically.
        /// If this is not intended, the overwriting class can always reset the animation being played at the same frame.
        /// </summary>
        public virtual void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            // e.g. T_STAND_2_WASH -> S_WASH -> S_WASH ... -> T_WASH_2_STAND
            // Inside daedalus there is no information about S_WASH, but we need this animation automatically being played.
            if (eventData.NextAnimation.Any())
            {
                PhysicsHelper.DisablePhysicsForNpc(Props);
                AnimationCreator.PlayAnimation(Props.MdsNames, eventData.NextAnimation, Props.Go);
            }
            // Play Idle animation
            // But only if NPC isn't using an item right now. Otherwise breathing will spawn hand to hips which looks wrong when (e.g.) drinking beer.
            else if (Props.CurrentItem < 0)
            {
                var weaponState = Props.WeaponState == VmGothicEnums.WeaponState.NoWeapon ? "" : Props.WeaponState.ToString();
                var animName = Props.WalkMode switch
                {
                    VmGothicEnums.WalkMode.Walk => $"S_{weaponState}WALKL",
                    VmGothicEnums.WalkMode.Sneak => $"S_{weaponState}SNEAK",
                    VmGothicEnums.WalkMode.Swim => $"S_{weaponState}SWIM",
                    VmGothicEnums.WalkMode.Dive => $"S_{weaponState}DIVE",
                    _ => $"S_{weaponState}RUN"
                };
                var idleAnimPlaying = AnimationCreator.PlayAnimation(Props.MdsNames, animName, Props.Go, true);
                if (!idleAnimPlaying)
                {
                    Debug.LogError($"Animation {animName} not found for {NpcGo.name} on {this}.");
                }
            }

            IsFinishedFlag = true;
        }

        /// <summary>
        /// Called every update cycle.
        /// Can be used to handle frequent things internally.
        /// </summary>
        public virtual void Tick()
        {
        }

        /// <summary>
        /// Most of our animations are fine if we just set this flag and return it via IsFinished()
        /// </summary>
        public virtual bool IsFinished()
        {
            return IsFinishedFlag;
        }

        public virtual void StopImmediately()
        {
            IsFinishedFlag = true;
        }
    }
}
