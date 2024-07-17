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
        private Dictionary<int, List<Routine>> _npcStartTimeDict = new();

        private readonly int _featureStartHour;
        private readonly int _featureStartMinute;

        public RoutineManager(GameConfiguration config)
        {
            _featureStartHour = config.StartTimeHour;
            _featureStartMinute = config.StartTimeMinute;
        }

        public void Init()
        {
            //Init starting position
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
            // We need to fill in routines backwards as e.g. Mud and Scorpio have duplicate routines. Last one needs to win.
            routines.Reverse();
            foreach (var routine in routines)
            {
                _npcStartTimeDict.TryAdd(routine.NormalizedStart, new List<Routine>());
                _npcStartTimeDict[routine.NormalizedStart].Add(npcID);
            }
        }

        public void Unsubscribe(Routine routineInstance, List<RoutineData> routines)
        {
            foreach (var routine in routines)
            {
                if (!_npcStartTimeDict.TryGetValue(routine.NormalizedStart, out var routinesForStartPoint))
                {
                    return;
                }

                routinesForStartPoint.Remove(routineInstance);

                // Remove element if empty
                if (_npcStartTimeDict[routine.NormalizedStart].Count == 0)
                {
                    _npcStartTimeDict.Remove(routine.NormalizedStart);
                }
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
            if (!_npcStartTimeDict.TryGetValue(normalizedNow, out var routineItems))
            {
                return;
            }

            foreach (var routineItem in routineItems)
            {
                routineItem.ChangeRoutine(now);
            }
        }
    }
}
