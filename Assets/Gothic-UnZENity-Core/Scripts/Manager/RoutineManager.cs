using System;
using System.Collections.Generic;
using GUZ.Core.Npc.Routines;
using UnityEngine;

namespace GUZ.Core.Manager
{
    /// <summary>
    /// Manages the Routines in a central spot. Routines Subscribe here. Calls the Routines when they are due.
    /// </summary>
    public class RoutineManager
    {
        private Dictionary<int, List<Routine>> npcStartTimeDict = new();

        private readonly bool _featureEnable;
        private readonly int _featureStartHour;
        private readonly int _featureStartMinute;

        public RoutineManager(GameConfiguration config)
        {
            _featureEnable = config.enableNpcRoutines;
            _featureStartHour = config.startTimeHour;
            _featureStartMinute = config.startTimeMinute;
        }
        
        public void Init()
        {
            //Init starting position
            if (!_featureEnable)
            {
                return;
            }
            
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(WorldLoadedEvent);
            GlobalEventDispatcher.GameTimeMinuteChangeCallback.AddListener(Invoke);
        }

        private void WorldLoadedEvent(GameObject playerGo)
        {
            var time = new DateTime(1, 1, 1, _featureStartHour, _featureStartMinute, 0);
            
            Invoke(time);
        }

        public void Subscribe(Routine npcID, List<RoutineData> routines)
        {
            if (!_featureEnable)
            {
                return;
            }

            // We need to fill in routines backwards as e.g. Mud and Scorpio have duplicate routines. Last one needs to win.
            routines.Reverse();
            foreach (var routine in routines)
            {
                npcStartTimeDict.TryAdd(routine.normalizedStart, new());
                npcStartTimeDict[routine.normalizedStart].Add(npcID);
            }
        }

        public void Unsubscribe(Routine routineInstance, List<RoutineData> routines)
        {
            foreach (RoutineData routine in routines)
            {
                if (!npcStartTimeDict.TryGetValue(routine.normalizedStart, out List<Routine> routinesForStartPoint))
                    return;

                routinesForStartPoint.Remove(routineInstance);

                // Remove element if empty
                if (npcStartTimeDict[routine.normalizedStart].Count == 0)
                    npcStartTimeDict.Remove(routine.normalizedStart);
            }
        }

        /// <summary>
        /// Calls the routineInstances that are due.
        /// Triggers Routine Change
        /// </summary>
        private void Invoke(DateTime now)
        {
            var normalizedNow = now.Hour % 24 * 60 + now.Minute;
            
            Debug.Log($"RoutineManager.timeChanged={now}");
            if (!npcStartTimeDict.TryGetValue(normalizedNow, out var routineItems))
                return;
            
            foreach (var routineItem in routineItems)
            {
                routineItem.ChangeRoutine(now);
            }
        }
    }
}
