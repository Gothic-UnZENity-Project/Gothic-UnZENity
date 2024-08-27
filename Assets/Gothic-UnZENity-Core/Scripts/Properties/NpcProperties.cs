using System;
using System.Collections.Generic;
using GUZ.Core.Npc;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Vm;
using GUZ.Core.Vob.WayNet;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Properties
{
    /// <summary>
    /// This component is attached to the root of NPC/Monster prefab.
    /// It's data is filled, whenever we call GothicVM.InitNpc(). This call triggers DaedalusVM to execute INSTANCE logic and we will fetch it with this object.
    /// </summary>
    public class NpcProperties : AbstractProperties
    {
        public NpcInstance NpcInstance;

        public AudioSource NpcSound;
        public Transform Bip01;

        public Transform ColliderRootMotion;

        public Transform Head;
        public HeadMorph HeadMorph;

        public WayPoint CurrentWayPoint;
        public FreePoint CurrentFreePoint;

        public List<InfoInstance> Dialogs = new();

        // Visual
        public string MdmName;
        public string BaseMdsName;

        public string OverlayMdsName;

        public string[] MdsNames => new[] { BaseMdsName, OverlayMdsName };
        public string BaseMdhName => BaseMdsName;
        public string OverlayMdhName => OverlayMdsName;
        public string[] MdhNames => new[] { BaseMdhName, OverlayMdhName };

        public List<ItemInstance> EquippedItems = new();
        public VmGothicExternals.ExtSetVisualBodyData BodyData;

        // Perceptions
        public Dictionary<VmGothicEnums.PerceptionType, int> Perceptions = new();

        public float PerceptionTime;

        // NPC items/talents/...
        public Dictionary<VmGothicEnums.Talent, int> Talents = new();
        public Dictionary<uint, int> Items = new(); // itemId => amount


        public readonly Queue<AbstractAnimationAction> AnimationQueue = new();
        public VmGothicEnums.WalkMode WalkMode;
        public VmGothicEnums.WeaponState WeaponState;

        // HINT: These information aren't set within Daedalus. We need to define them manually.
        // HINT: i.e. every animation might have a BS. E.g. when AI_TakeItem() is called, we set BS.BS_TAKEITEM
        public VmGothicEnums.BodyState BodyState;

        public GameObject CurrentInteractable; // e.g. PSI_CAULDRON

        public GameObject CurrentInteractableSlot; // e.g. ZS_0

        public int CurrentInteractableStateId = -1;

        public uint PrevStateStart;

        // State itself always means start state function. Gothic assumes this when checking for aistate.
        public int State => StateStart;

        public int StateStart;
        public int StateLoop;
        public int StateEnd;

        // State time is activated within AI_StartState()
        // e.g. used to handle random wait loops for idle eating animations (eat a cheese only every n-m seconds)
        public bool IsStateTimeActive;

        public float StateTime;

        public LoopState CurrentLoopState = LoopState.None;

        public AbstractAnimationAction CurrentAction;

        public bool HasItemEquipped;

        public int CurrentItem = -1;
        public string UsedItemSlot;

        // We need to start with an "invalid" value as >0< is an allowed state value like in >t_Potion_Stand_2_S0<
        public int ItemAnimationState = -1;

        public enum LoopState
        {
            None,
            Start,
            Loop,
            End
        }

#pragma warning disable CS0414 // Just a debug flag for easier debugging if we missed to copy something in the future. 
        public bool IsClonedFromAnother;
#pragma warning restore CS0414
        public void Copy(NpcProperties other)
        {
            IsClonedFromAnother = true;

            MdmName = other.MdmName;
            BaseMdsName = other.BaseMdsName;
            OverlayMdsName = other.OverlayMdsName;
            BodyData = other.BodyData;
            Perceptions = other.Perceptions;
            PerceptionTime = other.PerceptionTime;
        }

        public override string GetFocusName()
        {
            return NpcInstance.GetName(0);
        }
    }
}
