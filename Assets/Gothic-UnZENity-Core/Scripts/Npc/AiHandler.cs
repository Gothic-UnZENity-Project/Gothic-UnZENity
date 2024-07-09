using GUZ.Core.Creator;
using GUZ.Core.Data.ZkEvents;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Npc
{
    public class AiHandler : BasePlayerBehaviour, IAnimationCallbacks
    {
        private static DaedalusVm Vm => GameData.GothicVm;
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
            Properties.CurrentAction.Tick();

            // Add new milliseconds when stateTime shall be measured.
            if (Properties.IsStateTimeActive)
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
                Vm.GlobalSelf = Properties.NpcInstance;

                switch (Properties.CurrentLoopState)
                {
                    case NpcProperties.LoopState.Start:
                        if (Properties.StateLoop == 0)
                        {
                            return;
                        }

                        Vm.Call(Properties.StateStart);

                        Properties.CurrentLoopState = NpcProperties.LoopState.Loop;
                        break;
                    case NpcProperties.LoopState.Loop:
                        var symbol = Vm.GetSymbolByIndex(Properties.StateLoop);
                        if (symbol is { HasReturn: true })
                        {
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

        public void StartRoutine(int action, string wayPointName)
        {
            // End original loop first
            if (Properties.CurrentLoopState == NpcProperties.LoopState.Loop)
            {
                // We reuse this function as it is doing what we need.
                ClearState(false);
            }

            // We need to set WayPoint within Daedalus instance as it calls _self.wp_ during routine loops.
            Properties.NpcInstance.Wp = wayPointName;
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
            Properties.StateTime = 0;
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
                Properties.CurrentLoopState = NpcProperties.LoopState.End;

                if (Properties.StateEnd != 0)
                {
                    // We always need to set "self" before executing any Daedalus function.
                    Vm.GlobalSelf = Properties.NpcInstance;
                    Vm.Call(Properties.StateEnd);
                }
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

            // FIXME ! We need to re-add physics when e.g. looping walk animation!
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
            gameObject.transform.position = WayNetHelper.GetWayNetPoint(currentRoutine.Waypoint).Position;

            // Animation state handling
            Properties.AnimationQueue.Clear();
            Properties.CurrentAction = new None(new AnimationAction(), gameObject);
            Properties.StateTime = 0.0f;

            // WayNet handling
            if (Properties.CurrentFreePoint != null)
            {
                // If we despawn an NPC, the FP needs to be cleared as well.
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
            Properties.NpcInstance.SetAiVar(Constants.DaedalusAIVItemStatusKey, Constants.DaedalusTAITNone);

            // Start over
            StartRoutine(currentRoutine.Action, currentRoutine.Waypoint);
        }
    }
}
