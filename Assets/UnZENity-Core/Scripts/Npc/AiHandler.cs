using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc
{
    public class AiHandler : BasePlayerBehaviour, IAnimationCallbacks
    {
        private static DaedalusVm Vm => GameData.GothicVm;
        private const int _daedalusLoopContinue = 0; // Id taken from a Daedalus constant.

        private void Start()
        {
            Properties.CurrentAction = new None(new AnimationAction(), NpcData);
        }

        /// <summary>
        /// Basically:
        /// 1. Send Update (Tick) into current Animation to handle
        /// 2. If finished, then check, if we need to handle the new state. _Start() --> _Loop()
        ///
        /// Hint: The isStateTimeActive is only for AI_StartState() from Daedalus which calls sub-routine within routine.
        /// </summary>
        private void Update()
        {
            ExecuteActivePerceptions();

            Properties.CurrentAction.Tick();

            // Add new milliseconds when stateTime shall be measured.
            if (Properties.IsStateTimeActive && Properties.CurrentLoopState == NpcProperties2.LoopState.Loop)
            {
                Properties.StateTime += Time.deltaTime;
            }

            // If we're not yet done, we won't handle further tasks (like dequeuing another Action)
            if (!Properties.CurrentAction.IsFinished())
            {
                return;
            }

            // Queue is empty. Check if we want to start Looping
            if (Properties.AnimationQueue.Count == 0)
            {
                // We always need to set "self" before executing any Daedalus function.
                if (NpcInstance != null)
                {
                    Vm.GlobalSelf = NpcInstance;
                }

                DaedalusSymbol symbol;
                switch (Properties.CurrentLoopState)
                {
                    // None means, the NPC is newly created and didn't execute any Routine as of now.
                    case NpcProperties2.LoopState.None:
                        RestartCurrentRoutine();
                        break;
                    case NpcProperties2.LoopState.Start:
                        if (Properties.StateStart == 0)
                        {
                            return;
                        }

                        symbol = Vm.GetSymbolByIndex(Properties.StateStart)!;
                        switch (symbol.ReturnType)
                        {
                            case DaedalusDataType.Int:
                                Vm.Call<int>(Properties.StateStart);
                                break;
                            default:
                                Vm.Call(Properties.StateStart);
                                break;
                        }


                        Properties.CurrentLoopState = NpcProperties2.LoopState.Loop;
                        break;
                    case NpcProperties2.LoopState.Loop:
                        if (Properties.StateLoop == 0 && Properties.StateStart != 0)
                        {
                            Properties.CurrentLoopState = NpcProperties2.LoopState.Start;
                            return;
                        }

                        symbol = Vm.GetSymbolByIndex(Properties.StateLoop)!;
                        switch (symbol.ReturnType)
                        {
                            case DaedalusDataType.Int:
                                var loopResponse = Vm.Call<int>(Properties.StateLoop);
                                // Some ZS_*_Loop return !=0 when they want to quit.
                                if (loopResponse != _daedalusLoopContinue)
                                {
                                    Properties.CurrentLoopState = NpcProperties2.LoopState.End;
                                }
                                break;
                            default:
                                Vm.Call(Properties.StateLoop);
                                break;
                        }

                        break;

                    case NpcProperties2.LoopState.End:
                        if (Properties.StateEnd != 0)
                        {
                            symbol = Vm.GetSymbolByIndex(Properties.StateEnd)!;
                            switch (symbol.ReturnType)
                            {
                                case DaedalusDataType.Int:
                                    Vm.Call<int>(Properties.StateEnd);
                                    break;
                                default:
                                    Vm.Call(Properties.StateEnd);
                                    break;
                            }
                        }

                        // We filled the AnimationQueue with the ZS_*_End() animations once. END isn't looping.
                        Properties.CurrentLoopState = NpcProperties2.LoopState.AfterEnd;
                        break;
                    case NpcProperties2.LoopState.AfterEnd:
                        // We're done. Restart normal routine.
                        Properties.CurrentLoopState = NpcProperties2.LoopState.Start;

                        // If we're inside another ZS_*_ loop via Ai_StartState(), we will exit it now. If not, we will simply restart current ZS_* routine.
                        RestartCurrentRoutine();

                        break;
                }
            }
            // Go on
            else
            {
                Debug.Log($"Start playing >{Properties.AnimationQueue.Peek().GetType()}< on >{Go.transform.parent.name}<");
                PlayNextAnimation(Properties.AnimationQueue.Dequeue());
            }
        }

        /// <summary>
        /// Execute perceptions if it's about time.
        /// </summary>
        private void ExecuteActivePerceptions()
        {
            Properties.CurrentPerceptionTime += Time.deltaTime;
            if (Properties.CurrentPerceptionTime < Properties.PerceptionTime)
            {
                return;
            }

            GameGlobals.NpcAi.ExecutePerception(VmGothicEnums.PerceptionType.AssessPlayer, Properties, NpcInstance, (NpcInstance)GameData.GothicVm.GlobalHero);
            // FIXME - We need to add other active perceptions here:
            //         PERC_ASSESSBODY, PERC_ASSESSITEM, PERC_ASSESSENEMY, PERC_ASSESSFIGHTER
            //         But at best when we test it immediately

            // Reset timer if we executed Perceptions.
            Properties.CurrentPerceptionTime = 0f;
        }

        /// <summary>
        /// Restart means:
        /// 1. Either restart currently looping one or
        /// 2. Start the new one after Ai_ExchangeRoutine() got called and ZS_*END of previous one is done
        /// </summary>
        public void RestartCurrentRoutine()
        {
            var currentRoutine = Properties.RoutineCurrent;
            if (currentRoutine != null)
            {
                StartRoutine(currentRoutine.Action, currentRoutine.Waypoint);
            }
        }

        public void StartRoutine(int action, string wayPointName)
        {
            // We need to set WayPoint within Daedalus instance as it calls _self.wp_ during routine loops.
            NpcInstance.Wp = wayPointName;
            StartRoutine(action);
        }

        public void StartRoutine(int action)
        {
            // End original loop first
            // TODO - Calling ClearState(false) was buggy when e.g. Diego dialog "END" was clicked. Then the dialog lines were skipped.
            // if (Properties.CurrentLoopState == NpcProperties.LoopState.Loop)
            // {
            //     // We reuse this function as it is doing what we need.
            //     ClearState(false);
            // }

            var didRoutineChange = Properties.StateStart != action;

            Properties.StateStart = action;

            var routineSymbol = Vm.GetSymbolByIndex(action)!;

            var symbolLoop = Vm.GetSymbolByName($"{routineSymbol.Name}_Loop");
            if (symbolLoop != null)
            {
                Properties.StateLoop = symbolLoop.Index;
            }

            var symbolEnd = Vm.GetSymbolByName($"{routineSymbol.Name}_End");
            if (symbolEnd != null)
            {
                Properties.StateEnd = symbolEnd.Index;
            }

            Properties.CurrentLoopState = NpcProperties2.LoopState.Start;

            // We need to properly start state time as e.g. ZS_Cook won't call AI_StartState() or Npc_SetStateTime()
            // But it's required as it checks immediately how long the Cauldron is already been whirled.
            Properties.IsStateTimeActive = true;

            // When we reached end of ZS_*_END, we also call this method. Check if we really altered the routine action or just restarted it.
            if (didRoutineChange)
            {
                Debug.Log($"Start new routine >{routineSymbol.Name}< on >{Go.transform.parent.name}<");
                Properties.StateTime = 0;
            }
        }

        /// <summary>
        /// Clear ZS functions. If stopCurrentState=true, then stop current animation and don't execute with ZS_*_End()
        /// </summary>
        public void ClearState(bool stopCurrentStateImmediately)
        {
            // Whenever we change routine, we reset some data to "start" from scratch as if the NPC got spawned.
            Properties.AnimationQueue.Clear();
            Properties.CurrentAction = new None(new AnimationAction(), NpcData);
            Properties.StateTime = 0.0f;
            Properties.ItemAnimationState = -1;

            if (stopCurrentStateImmediately)
            {
                Properties.CurrentLoopState = NpcProperties2.LoopState.None;
                AnimationCreator.StopAnimation(Go);
            }
            else
            {
                Properties.CurrentLoopState = NpcProperties2.LoopState.End; // Next frame, the End logic will be executed.
            }
        }

        private void PlayNextAnimation(AbstractAnimationAction action)
        {
            Properties.CurrentAction = action;
            action.Start();
        }

        public void AnimationCallback(string eventTagDataParam)
        {
            var eventData = JsonUtility.FromJson<SerializableEventTag>(eventTagDataParam);
            Properties.CurrentAction.AnimationEventCallback(eventData);
        }

        public void AnimationSfxCallback(string eventSfxDataParam)
        {
            var eventData = JsonUtility.FromJson<SerializableEventSoundEffect>(eventSfxDataParam);
            Properties.CurrentAction.AnimationSfxEventCallback(eventData);
        }

        public void AnimationMorphCallback(string eventMorphDataParam)
        {
            var eventData = JsonUtility.FromJson<SerializableEventMorphAnimation>(eventMorphDataParam);
            Properties.CurrentAction.AnimationMorphEventCallback(eventData);
        }

        public void AnimationBlendOutCallback(string eventBlendOutParam)
        {
            var eventData = JsonUtility.FromJson<SerializableEventBlendOutSignal>(eventBlendOutParam);

            Properties.CurrentAction.AnimationBlendOutEventCallback(eventData);
        }

        /// <summary>
        /// Fully reset NPC state.
        /// Called after an NPC is re-enabled in the scene.
        /// </summary>
        public void ReEnableNpc()
        {
            // Spawn to initial spawn location
            var currentRoutine = gameObject.GetComponent<Routine>().CurrentRoutine;
            if (currentRoutine != null)
                gameObject.transform.position = WayNetHelper.GetWayNetPoint(currentRoutine.Waypoint).Position;

            // Animation state handling
            Properties.AnimationQueue.Clear();
            Properties.CurrentAction = new None(new AnimationAction(), NpcData);
            Properties.StateTime = 0.0f;

            // WayNet handling
            if (Properties.CurrentFreePoint != null)
            {
                // FIXME - If we despawn an NPC, the FP needs to be cleared as well.
                Properties.CurrentFreePoint.IsLocked = false;
            }
            Properties.CurrentFreePoint = null;
            Properties.CurrentWayPoint = null;

            // CurrentItem handling
            Properties.ItemAnimationState = -1;
            if (Properties.CurrentItem != -1)
            {
                // If NPC had an item in its hands, we need to remove the mesh.
                var leftHand = gameObject.FindChildRecursively("ZS_LEFTHAND");
                var rightHand = gameObject.FindChildRecursively("ZS_RIGHTHAND");

                if (leftHand != null)
                {
                    for (var i = 0; i < leftHand.transform.childCount; i++)
                    {
                        Destroy(leftHand.transform.GetChild(i).gameObject);
                    }
                }

                if (rightHand != null)
                {
                    for (var i = 0; i < rightHand.transform.childCount; i++)
                    {
                        Destroy(rightHand.transform.GetChild(i).gameObject);
                    }
                }
            }
            Properties.CurrentItem = -1;

            // Reset "currently" used item

            // FIXME - We need to properly set this value for Gothic2 as well.
            if (GameGlobals.Config.Dev.GameVersion == GameVersion.Gothic1)
            {
                NpcInstance.SetAiVar(Constants.DaedalusConst.AIVItemStatusKey, Constants.DaedalusConst.TAITNone);
            }

            // Start over
            if (currentRoutine != null)
                StartRoutine(currentRoutine.Action, currentRoutine.Waypoint);
            else
            {
                //if we don't have a routine means it's about a monster
                StartRoutine(Properties.StateStart);
            }
        }
    }
}
