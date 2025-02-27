using System;
using System.Linq;
using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;
using EventType = ZenKit.EventType;
using Object = UnityEngine.Object;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public abstract class AbstractAnimationAction
    {
        protected readonly AnimationAction Action;
        protected readonly NpcContainer2 NpcContainer;
        protected readonly NpcInstance NpcInstance;
        protected readonly GameObject NpcGo;
        protected readonly NpcProperties2 Props;
        protected readonly NpcPrefabProperties2 PrefabProps;

        protected bool IsFinishedFlag;

        public AbstractAnimationAction(AnimationAction action, NpcContainer2 npcData)
        {
            Action = action;
            NpcContainer = npcData;
            NpcInstance = npcData.Instance;
            NpcGo = npcData.Go;
            Props = npcData.Props;
            PrefabProps = npcData.PrefabProps;
        }

        public virtual void Start()
        {
            // By default, every Daedalus animation starts without using physics. But they can always overwrite it (e.g.) for walking.
            PhysicsHelper.DisablePhysicsForNpc(PrefabProps);
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
            PrefabProps.NpcSound.clip = clip;
            PrefabProps.NpcSound.maxDistance = sfxData.Range.ToMeter();
            PrefabProps.NpcSound.Play();

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

        public void AnimationBlendOutEventCallback(SerializableEventBlendOutSignal eventData)
        {
            AnimationCreator.StartBlendOutAnimation(eventData.CurrentAnimName, eventData.BlendOutTime, NpcGo);
        }

        /// <summary>
        /// Most of our animations are fine if we just set this flag and return it via IsFinished()
        /// If an animation has also a next animation set, we will call it automatically. Alternatively we play an idle animation.
        /// If the overall behaviour isn't intended, the overwriting class can always reset/alter the animation being played at the same frame.
        /// </summary>
        public virtual void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            if (eventData.NextAnimation.Any())
            {
                PhysicsHelper.DisablePhysicsForNpc(PrefabProps);
                PrefabProps.AnimationHandler.PlayAnimation(eventData.NextAnimation);
            }
            // HINT: Should be handled automatically now!
            // // Play Idle animation
            // // But only if NPC isn't using an item right now. Otherwise, breathing will spawn hand to hips which looks wrong when (e.g.) drinking beer.
            // else if (Props.CurrentItem < 0)
            // {
            //     var weaponState = Props.WeaponState == VmGothicEnums.WeaponState.NoWeapon ? "" : Props.WeaponState.ToString();
            //     var animName = Props.WalkMode switch
            //     {
            //         VmGothicEnums.WalkMode.Walk => $"S_{weaponState}WALK",
            //         VmGothicEnums.WalkMode.Sneak => $"S_{weaponState}SNEAK",
            //         VmGothicEnums.WalkMode.Swim => $"S_{weaponState}SWIM",
            //         VmGothicEnums.WalkMode.Dive => $"S_{weaponState}DIVE",
            //         _ => $"S_{weaponState}RUN"
            //     };
            //     var idleAnimPlaying = AnimationCreator.PlayAnimation(Props.MdsNames, animName, NpcGo, true);
            //     if (!idleAnimPlaying)
            //     {
            //         Debug.LogError($"Animation {animName} not found for {NpcGo.name} on {this}.");
            //     }
            // }

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
