﻿using GUZ.Core.Creator;
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
        private int? _cachedRoutineAction = null;
        private const int _daedalusLoopContinue = 0; // Id taken from a Daedalus constant.

        private void Start()
        {
            Properties.CurrentAction = new None(new AnimationAction(), gameObject);
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
            if (Properties.IsStateTimeActive && Properties.CurrentLoopState == NpcProperties.LoopState.Loop)
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
                if (Properties.NpcInstance != null)
                {
                    Vm.GlobalSelf = Properties.NpcInstance;
                }

                DaedalusSymbol symbol;
                switch (Properties.CurrentLoopState)
                {
                    case NpcProperties.LoopState.Start:
                        if (Properties.StateStart == 0)
                        {
                            return;
                        }

                        symbol = Vm.GetSymbolByIndex(Properties.StateStart);
                        switch (symbol.ReturnType)
                        {
                            case DaedalusDataType.Int:
                                Vm.Call<int>(Properties.StateStart);
                                break;
                            default:
                                Vm.Call(Properties.StateStart);
                                break;
                        }


                        Properties.CurrentLoopState = NpcProperties.LoopState.Loop;
                        break;
                    case NpcProperties.LoopState.Loop:
                        if (Properties.StateLoop == 0 && Properties.StateStart != 0)
                        {
                            Properties.CurrentLoopState = NpcProperties.LoopState.Start;
                            return;
                        }

                        symbol = Vm.GetSymbolByIndex(Properties.StateLoop);
                        switch (symbol.ReturnType)
                        {
                            case DaedalusDataType.Int:
                                var loopResponse = Vm.Call<int>(Properties.StateLoop);
                                // Some ZS_*_Loop return !=0 when they want to quit.
                                if (loopResponse != _daedalusLoopContinue)
                                {
                                    Properties.CurrentLoopState = NpcProperties.LoopState.End;
                                }
                                break;
                            default:
                                Vm.Call(Properties.StateLoop);
                                break;
                        }

                        break;

                    case NpcProperties.LoopState.End:
                        if (Properties.StateEnd != 0)
                        {
                            symbol = Vm.GetSymbolByIndex(Properties.StateEnd);
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

                        // We filled the AnimationQueue with the ZS_*_End() animations. Do not fill it again until a new behaviour is triggered.
                        Properties.CurrentLoopState = NpcProperties.LoopState.AfterEnd;
                        break;
                    case NpcProperties.LoopState.AfterEnd:
                        // Check if we have a cached routine to start
                        if (_cachedRoutineAction.HasValue)
                        {
                            StartRoutineImmediately(_cachedRoutineAction.Value);
                            _cachedRoutineAction = null;
                        }
                        else
                        {
                            // We're done. Restart normal routine.
                            Properties.CurrentLoopState = NpcProperties.LoopState.Start;

                            // If we're inside another ZS_*_ loop via Ai_StartState(), we will exit it now. If not, we will simply restart current ZS_* routine.
                            var currentRoutine = gameObject.GetComponent<Routine>().CurrentRoutine;
                            if (currentRoutine != null)
                            {
                                StartRoutine(currentRoutine.Action);
                            }
                        }
                        break;
                }
            }
            // Go on
            else
            {
                Debug.Log($"Start playing >{Properties.AnimationQueue.Peek().GetType()}< on >{Properties.Go.name}<");
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

            NpcHelper.ExecutePerception(VmGothicEnums.PerceptionType.AssessPlayer, Properties, Properties.NpcInstance, (NpcInstance)GameData.GothicVm.GlobalHero);
            // FIXME - We need to add other active perceptions here:
            //         PERC_ASSESSBODY, PERC_ASSESSITEM, PERC_ASSESSENEMY, PERC_ASSESSFIGHTER
            //         But at best when we test it immediately

            // Reset timer if we executed Perceptions.
            Properties.CurrentPerceptionTime = 0f;
        }

        public void StartRoutine(int action, string wayPointName)
        {
            // We need to set WayPoint within Daedalus instance as it calls _self.wp_ during routine loops.
            Properties.NpcInstance.Wp = wayPointName;
            StartRoutine(action);
        }

        public void StartRoutine(int action)
        {
            if (Properties.CurrentLoopState == NpcProperties.LoopState.Loop)
            {
                // Cache the new action and force the current loop to end
                _cachedRoutineAction = action;
            }
            else
            {
                // If we're not in a loop, start the new routine immediately
                StartRoutineImmediately(action);
            }
        }

        public void StartRoutineImmediately(int action)
        {
            // End original loop first
            // TODO - Calling ClearState(false) was buggy when e.g. Diego dialog "END" was clicked. Then the dialog lines were skipped.
            // if (Properties.CurrentLoopState == NpcProperties.LoopState.Loop)
            // {
            //     // We reuse this function as it is doing what we need.
            //     ClearState(true);
            // }

            var didRoutineChange = Properties.StateStart != action;

            if (didRoutineChange && Vm.GetSymbolByIndex(Properties.StateStart).Name == "ZS_TALK")
            {
                Debug.Log($"NPC:{Properties.NpcInstance.Id} new routine after ZS_TALK!! THIS SKIPS ZS_TALK_END");
                Debug.Log($"Current state is {Vm.GetSymbolByIndex(action).Name} {Properties.CurrentLoopState}");
            }

            Properties.StateStart = action;

            var routineSymbol = Vm.GetSymbolByIndex(action);

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

            Properties.CurrentLoopState = NpcProperties.LoopState.Start;

            // We need to properly start state time as e.g. ZS_Cook won't call AI_StartState() or Npc_SetStateTime()
            // But it's required as it checks immediately how long the Cauldron is already been whirled.
            Properties.IsStateTimeActive = true;

            // When we reached end of ZS_*_END, we also call this method. Check if we really altered the routine action or just restarted it.
            if (didRoutineChange)
            {
                Debug.Log($"Start new routine >{routineSymbol.Name}< on >{Properties.Go.name}<");
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
            Properties.CurrentAction = new None(new AnimationAction(), gameObject);
            Properties.StateTime = 0.0f;
            Properties.ItemAnimationState = -1;

            if (stopCurrentStateImmediately)
            {
                Properties.CurrentLoopState = NpcProperties.LoopState.None;
                AnimationCreator.StopAnimation(Properties.Go);
            }
            else
            {
                Properties.CurrentLoopState = NpcProperties.LoopState.End; // Next frame, the End logic will be executed.
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

        /// <summary>
        /// As all Components on a GameObject get called, we need to feed this information into current AnimationAction instance.
        /// </summary>
        public void AnimationEndCallback(string eventEndSignalParam)
        {
            var eventData = JsonUtility.FromJson<SerializableEventEndSignal>(eventEndSignalParam);

            Properties.CurrentAction.AnimationEndEventCallback(eventData);
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
            {
                gameObject.transform.position = WayNetHelper.GetWayNetPoint(currentRoutine.Waypoint).Position;
                GameData.WayPoints.TryGetValue(currentRoutine.Waypoint, out Properties.CurrentWayPoint);
            }

            // Animation state handling
            Properties.AnimationQueue.Clear();
            Properties.CurrentAction = new None(new AnimationAction(), gameObject);
            Properties.StateTime = 0.0f;

            // WayNet handling
            if (Properties.CurrentFreePoint != null)
            {
                // FIXME - If we despawn an NPC, the FP needs to be cleared as well.
                Properties.CurrentFreePoint.IsLocked = false;
            }
            Properties.CurrentFreePoint = null;
            // Properties.CurrentWayPoint = null;

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
            if (GameGlobals.Config.GameVersion == GameVersion.Gothic1)
            {
                Properties.NpcInstance.SetAiVar(Constants.DaedalusConst.AIVItemStatusKey, Constants.DaedalusConst.TAITNone);
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
