using System;
using System.Collections;
using UnityEngine;

namespace GUZ.Core.World
{
    public enum GameTimeInterval
    {
        EveryGameSecond,
        EveryGameMinute,
        EveryGameHour
    }

    public class GameTime
    {
        public static readonly DateTime MinTime = new(1, 1, 1, 0, 0, 0);
        public static readonly DateTime MaxTime = new(9999, 12, 31, 23, 59, 59);

        private int _secondsInMinute;
        private int _minutesInHour;

        // Calculation: One full ingame day (==86400 ingame seconds) has 6000 sec real time
        // 6000 real time seconds -> 86400 ingame seconds
        //    x real time seconds ->     1 ingame second
        //    x == 0.06944
        // Reference (ger): https://forum.worldofplayers.de/forum/threads/939357-Wie-lange-dauert-ein-Tag-in-Gothic
        private static readonly float _oneIngameSecond = 0.06944f;
        private DateTime _time = new(1, 1, 1, 15, 0, 0);
        private Coroutine _timeTickCoroutineHandler;

        private readonly ICoroutineManager _coroutineManager;
        private readonly int _featureStartHour;
        private readonly int _featureStartMinute;
        private readonly float _featureTimeMultiplier;

        public GameTime(GameConfiguration config, ICoroutineManager coroutineManager)
        {
            _coroutineManager = coroutineManager;
            _featureStartHour = config.StartTimeHour;
            _featureStartMinute = config.StartTimeMinute;
            _featureTimeMultiplier = config.TimeSpeedMultiplier;
        }

        public void Init()
        {
            // Set debug value for current Time.
            _time = new DateTime(_time.Year, _time.Month, _time.Day, _featureStartHour, _featureStartMinute,
                _time.Second);
            _minutesInHour = _featureStartMinute;

            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(WorldLoaded);
            GlobalEventDispatcher.GeneralSceneUnloaded.AddListener(WorldUnloaded);
        }

        private void WorldLoaded(GameObject playerGo)
        {
            _timeTickCoroutineHandler = _coroutineManager.StartCoroutine(TimeTick());
        }

        private void WorldUnloaded()
        {
            // Pause Coroutine until next world is loaded.
            _coroutineManager.StopCoroutine(_timeTickCoroutineHandler);
        }

        public DateTime GetCurrentDateTime()
        {
            return new DateTime(_time.Ticks);
        }

        private IEnumerator TimeTick()
        {
            while (true)
            {
                _time = _time.AddSeconds(1);

                if (_time > MaxTime)
                {
                    _time = MinTime;
                }

                GlobalEventDispatcher.GameTimeSecondChangeCallback.Invoke(_time);
                RaiseMinuteAndHourEvent();
                yield return new WaitForSeconds(_oneIngameSecond / _featureTimeMultiplier);
            }
        }

        private void RaiseMinuteAndHourEvent()
        {
            _secondsInMinute++;
            if (_secondsInMinute % 60 == 0)
            {
                _secondsInMinute = 0;
                GlobalEventDispatcher.GameTimeMinuteChangeCallback.Invoke(_time);
                RaiseHourEvent();
            }
        }

        private void RaiseHourEvent()
        {
            _minutesInHour++;
            if (_minutesInHour % 60 == 0)
            {
                _minutesInHour = 0;
                GlobalEventDispatcher.GameTimeHourChangeCallback.Invoke(_time);
            }
        }

        public bool IsDay()
        {
            // 6:30 - 18:30  -  values taken from gothic and regoth - https://github.com/REGoth-project/REGoth/blob/master/src/engine/GameClock.cpp#L126
            var startOfDay = new TimeSpan(6, 30, 0);
            var endOfDay = new TimeSpan(18, 30, 0);

            var currentTime = _time.TimeOfDay;

            return currentTime >= startOfDay && currentTime <= endOfDay;
        }

        public int GetDay()
        {
            return _time.Day;
        }

        public TimeSpan GetCurrentTime()
        {
            return _time.TimeOfDay;
        }

        public void SetTime(int hour, int minute)
        {
            _time = new DateTime(_time.Year, _time.Month, _time.Day, hour, minute, 0);
        }

        public float GetSkyTime()
        {
            var currentTime = _time.TimeOfDay;

            double totalSecondsInADay = 24 * 60 * 60;

            double secondsPassedSinceNoon;
            if (currentTime < TimeSpan.FromHours(12))
            {
                secondsPassedSinceNoon = currentTime.TotalSeconds + 12 * 60 * 60;
            }
            else
            {
                secondsPassedSinceNoon = currentTime.TotalSeconds - 12 * 60 * 60;
            }

            // Calculate sky time as a float between 0 and 1
            var skyTime = (float)(secondsPassedSinceNoon / totalSecondsInADay);

            return skyTime;
        }
    }
}
