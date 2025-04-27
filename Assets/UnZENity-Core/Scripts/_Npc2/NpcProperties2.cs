using System.Collections.Generic;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Vm;
using GUZ.Core.Vob.WayNet;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    public class NpcProperties2
    {
        // Misc
        public List<InfoInstance> Dialogs = new();
        public VmGothicEnums.WalkMode WalkMode;

        // Routines
        public List<RoutineData> Routines = new();
        public RoutineData RoutinePrevious;
        public RoutineData RoutineCurrent;

        // WayNet
        public WayPoint CurrentWayPoint;
        public FreePoint CurrentFreePoint;

        // NPC items/talents/...
        public Dictionary<VmGothicEnums.Talent, int> Talents = new();
        public VmGothicEnums.WeaponState WeaponState;

        public List<ItemInstance> EquippedItems = new();
        public Dictionary<uint, int> Items = new(); // itemId => amount
        public string UsedItemSlot;
        public bool HasItemEquipped;
        public int CurrentItem = -1;
        // We need to start with an "invalid" value as >0< is an allowed state value like in >t_Potion_Stand_2_S0<
        public int ItemAnimationState = -1;

        public int CurrentInteractableStateId = -1;

        // Visual
        public string MdmName;
        public string MdsNameBase;
        public string MdsNameOverlay;
        public string[] MdsNames => new[] { MdsNameBase, MdsNameOverlay };

        // An MDS file has always an MDH file named identically
        public string MdhNameBase => MdsNameBase;
        public string MdhNameOverlay => MdsNameOverlay;

        public VmGothicExternals.ExtSetVisualBodyData BodyData;

        // Perceptions
        public Dictionary<VmGothicEnums.PerceptionType, int> Perceptions = new();
        public float PerceptionTime = 5f; // Default in seconds
        public float CurrentPerceptionTime;


        // AI topics

        public NpcInstance EnemyNpc;
        public NpcInstance TargetNpc;

        public enum LoopState
        {
            None,
            Start,
            Loop,
            End,
            AfterEnd
        }
        public readonly Queue<AbstractAnimationAction> AnimationQueue = new();

        // State itself always means start state function. Gothic assumes this when checking for aistate.
        public int State => StateStart;
        public uint PrevStateStart;

        // HINT: This information isn't set within Daedalus. We need to define it manually.
        // HINT: i.e. every animation might have a BS. E.g. when AI_TakeItem() is called, we set BS.BS_TAKEITEM
        public VmGothicEnums.BodyState BodyState;

        public int StateStart;
        public int StateLoop;
        public int StateEnd;

        // State time is activated within AI_StartState()
        // e.g. used to handle random wait loops for idle eating animations (eat a cheese only every n-m seconds)
        public bool IsStateTimeActive;
        public float StateTime;
        public LoopState CurrentLoopState = LoopState.None;
        public AbstractAnimationAction CurrentAction;
        
        // Attitudes
        // HINT: These values are only used when checking the attitude towards the player
        // HINT: for attitudes between NPC we directly use the guild attitude
        public VmGothicEnums.Attitude Attitude = VmGothicEnums.Attitude.Neutral;
        public VmGothicEnums.Attitude TempAttitude = VmGothicEnums.Attitude.Neutral;
    }
}
