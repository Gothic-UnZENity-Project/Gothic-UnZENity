using System.Collections.Generic;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Vm;
using GUZ.Core.Vob.WayNet;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit.Daedalus;

namespace GUZ.Core.Properties
{
    public class NpcProperties : AbstractProperties
    {
        public NpcInstance NpcInstance;

        [FormerlySerializedAs("npcSound")] public AudioSource NpcSound;
        [FormerlySerializedAs("bip01")] public Transform Bip01;

        [FormerlySerializedAs("colliderRootMotion")]
        public Transform ColliderRootMotion;

        [FormerlySerializedAs("head")] public Transform Head;
        [FormerlySerializedAs("headMorph")] public HeadMorph HeadMorph;

        public WayPoint CurrentWayPoint;
        public FreePoint CurrentFreePoint;

        public List<InfoInstance> Dialogs = new();

        // Visual
        [FormerlySerializedAs("mdmName")] public string MdmName;
        [FormerlySerializedAs("baseMdsName")] public string BaseMdsName;

        [FormerlySerializedAs("overlayMdsName")]
        public string OverlayMdsName;

        public string[] MdsNames => new[] { BaseMdsName, OverlayMdsName };
        public string BaseMdhName => BaseMdsName;
        public string OverlayMdhName => OverlayMdsName;
        public string[] MdhNames => new[] { BaseMdhName, OverlayMdhName };

        public List<ItemInstance> EquippedItems = new();
        public VmGothicExternals.ExtSetVisualBodyData BodyData;

        // Perceptions
        public Dictionary<VmGothicEnums.PerceptionType, int> Perceptions = new();

        [FormerlySerializedAs("perceptionTime")]
        public float PerceptionTime;

        // NPC items/talents/...
        public Dictionary<VmGothicEnums.Talent, int> Talents = new();
        public Dictionary<uint, int> Items = new(); // itemId => amount


        public readonly Queue<AbstractAnimationAction> AnimationQueue = new();
        [FormerlySerializedAs("walkMode")] public VmGothicEnums.WalkMode WalkMode;

        // HINT: These information aren't set within Daedalus. We need to define them manually.
        // HINT: i.e. every animation might have a BS. E.g. when AI_TakeItem() is called, we set BS.BS_TAKEITEM
        [FormerlySerializedAs("bodyState")] public VmGothicEnums.BodyState BodyState;

        [FormerlySerializedAs("currentInteractable")]
        public GameObject CurrentInteractable; // e.g. PSI_CAULDRON

        [FormerlySerializedAs("currentInteractableSlot")]
        public GameObject CurrentInteractableSlot; // e.g. ZS_0

        [FormerlySerializedAs("currentInteractableStateId")]
        public int CurrentInteractableStateId = -1;

        [FormerlySerializedAs("prevStateStart")]
        public uint PrevStateStart;

        // State itself always means start state function. Gothic assumes this when checking for aistate.
        public int State => StateStart;

        [FormerlySerializedAs("stateStart")] public int StateStart;
        [FormerlySerializedAs("stateLoop")] public int StateLoop;
        [FormerlySerializedAs("stateEnd")] public int StateEnd;

        // State time is activated within AI_StartState()
        // e.g. used to handle random wait loops for idle eating animations (eat a cheese only every n-m seconds)
        [FormerlySerializedAs("isStateTimeActive")]
        public bool IsStateTimeActive;

        [FormerlySerializedAs("stateTime")] public float StateTime;

        [FormerlySerializedAs("currentLoopState")]
        public LoopState CurrentLoopState = LoopState.None;

        public AbstractAnimationAction CurrentAction;

        [FormerlySerializedAs("hasItemEquipped")]
        public bool HasItemEquipped;

        [FormerlySerializedAs("currentItem")] public int CurrentItem;
        [FormerlySerializedAs("usedItemSlot")] public string UsedItemSlot;

        // We need to start with an "invalid" value as >0< is an allowed state value like in >t_Potion_Stand_2_S0<
        [FormerlySerializedAs("itemAnimationState")]
        public int ItemAnimationState = -1;

        public enum LoopState
        {
            None,
            Start,
            Loop,
            End
        }

#pragma warning disable CS0414 // Just a debug flag for easier debugging if we missed to copy something in the future. 
        [FormerlySerializedAs("isClonedFromAnother")]
        public bool IsClonedFromAnother;
#pragma warning restore CS0414
        public void Copy(NpcProperties other)
        {
            IsClonedFromAnother = true;
            NpcInstance = other.NpcInstance;

            MdmName = other.MdmName;
            BaseMdsName = other.BaseMdsName;
            OverlayMdsName = other.OverlayMdsName;
            BodyData = other.BodyData;
            Perceptions = other.Perceptions;
            PerceptionTime = other.PerceptionTime;
        }
    }
}
