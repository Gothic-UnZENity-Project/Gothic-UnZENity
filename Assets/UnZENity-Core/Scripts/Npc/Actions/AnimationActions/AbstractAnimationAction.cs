using System;
using System.Linq;
using GUZ.Core.Creator;
using GUZ.Core.Data.Container;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;
using EventType = ZenKit.EventType;
using Logger = GUZ.Core.Util.Logger;
using Object = UnityEngine.Object;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractAnimationAction
    {
        public readonly AnimationAction Action;
        protected readonly NpcContainer NpcContainer;
        protected readonly NpcInstance NpcInstance;
        protected readonly GameObject NpcGo;
        protected readonly NpcProperties Props;
        protected readonly ZenKit.Vobs.INpc Vob;
        protected readonly NpcPrefabProperties PrefabProps;

        protected float ActionTime;
        protected float ActionEndEventTime;

        protected bool IsFinishedFlag;

        public AbstractAnimationAction(AnimationAction action, NpcContainer npcData)
        {
            Action = action;
            NpcContainer = npcData;
            NpcInstance = npcData.Instance;
            NpcGo = npcData.Go;
            Props = npcData.Props;
            Vob = npcData.Vob;
            PrefabProps = npcData.PrefabProps;
        }

        public virtual void Start()
        {
            // By default, every Daedalus animation starts without using physics. But they can always overwrite it (e.g.) for walking.
            PhysicsHelper.DisablePhysicsForNpc(PrefabProps);
        }
        
        protected string GetWalkModeAnimationString()
        {
            // The name of the currently active weapon == prefix of animation.
            var weaponState = Vob.FightMode == (int)VmGothicEnums.WeaponState.NoWeapon
                ? ""
                : ((VmGothicEnums.WeaponState)Vob.FightMode).ToString().ToUpper();
            
            string walkMode;
            switch (Props.WalkMode)
            {
                case VmGothicEnums.WalkMode.Walk:
                    walkMode = "WALKL";
                    break;
                case VmGothicEnums.WalkMode.Run:
                    walkMode = "RUNL";
                    break;
                case VmGothicEnums.WalkMode.Sneak:
                    walkMode = "SNEAKL";
                    break;
                case VmGothicEnums.WalkMode.Water:
                    walkMode = "WATERL";
                    break;
                case VmGothicEnums.WalkMode.Swim:
                    walkMode = "SWIML";
                    break;
                case VmGothicEnums.WalkMode.Dive:
                    walkMode = "DIVEL";
                    break;
                default:
                    Logger.LogWarning($"Animation of type {Props.WalkMode} not yet implemented.", LogCat.Animation);
                    return "";
            }

            return $"S_{weaponState}{walkMode}";
        }

        /// <summary>
        /// We just set the audio by default.
        /// </summary>
        public virtual void AnimationSfxEventCallback(SerializableEventSoundEffect sfxData)
        {
            var clip = GameGlobals.Vobs.GetSoundClip(sfxData.Name);
            PrefabProps.NpcSound.clip = clip;
            PrefabProps.NpcSound.maxDistance = sfxData.Range.ToMeter();
            PrefabProps.NpcSound.Play();

            if (sfxData.EmptySlot)
            {
                Logger.LogWarning($"PxEventSfxData.emptySlot not yet implemented: {sfxData.Name}", LogCat.Ai);
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
                    Logger.Log(
                        "EventType.inventory_torch: I assume this means: if torch is in inventory, then put it out. " +
                        "But not really sure. Need a NPC with real usage of it to predict right.", LogCat.Ai);
                    break;
                default:
                    Logger.LogWarning($"EventType.type {data.Type} not yet supported.", LogCat.Ai);
                    break;
            }
        }

        public virtual void AnimationMorphEventCallback(SerializableEventMorphAnimation data)
        {
            var type = PrefabProps.HeadMorph.GetAnimationTypeByName(data.Animation);

            PrefabProps.HeadMorph.StartAnimation(Props.BodyData.Head, type);
        }

        protected virtual void InsertItem(string slot1, string slot2)
        {
            if (slot2.Any())
            {
                throw new Exception("Slot 2 is set but not yet handled by InsertItem as AnimationEvent.");
            }

            var slotGo = NpcGo.FindChildRecursively(slot1);

            GameGlobals.Vobs.CreateItemMesh(Props.CurrentItem, slotGo);

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
        /// Called every update cycle.
        /// Can be used to handle frequent things internally.
        /// </summary>
        public virtual void Tick()
        {
            ActionTime += Time.deltaTime;

            if (ActionEndEventTime != 0.0f && ActionTime >= ActionEndEventTime)
            {
                AnimationEnd();
            }

        }

        /// <summary>
        /// Most of our animations are fine if we just set this flag and return it via IsFinished()
        /// If an animation has also a next animation set, it will be called within NpcAnimationHandler automatically (e.g. idle animation).
        /// </summary>
        protected virtual void AnimationEnd()
        {
            IsFinishedFlag = true;
        }

        /// <summary>
        /// Most of our animations are fine if we just set this flag and return it via IsFinished()
        /// If an animation has also a next animation set, we will call it automatically. Alternatively we play an idle animation.
        /// If the overall behaviour isn't intended, the overwriting class can always reset/alter the animation being played at the same frame.
        /// </summary>
        [Obsolete("As we BlendIn/BlendOut, this method is never reached. Use AnimationEnd() instead.")]
        public virtual void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            if (eventData.NextAnimation.Any())
            {
                PhysicsHelper.DisablePhysicsForNpc(PrefabProps);
                PrefabProps.AnimationSystem.PlayAnimation(eventData.NextAnimation);
            }

            IsFinishedFlag = true;
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
