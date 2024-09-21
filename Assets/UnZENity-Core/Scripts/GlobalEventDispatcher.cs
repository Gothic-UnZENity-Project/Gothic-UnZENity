using System;
using UnityEngine;
using UnityEngine.Events;
using ZenKit;

namespace GUZ.Core
{
    /// <summary>
    /// Loading/Unloading order of scenes:
    /// https://github.com/Gothic-UnZENity-Project/Gothic-UnZENity/blob/main/Docs/development/diagrams/SceneLoading.drawio.png
    /// </summary>
    public static class GlobalEventDispatcher
    {
        // We need to ensure, that other modules will register themselves basefgd on current Control+GameMode setting.
        // Since we can't call them (e.g. Flat/VR) directly, we need to leverage this IoC pattern.
        public static readonly UnityEvent<GameContext.Controls> RegisterControlAdapters = new();
        public static readonly UnityEvent<GameVersion> RegisterGameVersionAdapters = new();

        public static readonly UnityEvent ZenKitBootstrapped = new();
        
        public static readonly UnityEvent WorldFullyLoaded = new ();

        public static readonly UnityEvent MainMenuSceneLoaded = new();
        public static readonly UnityEvent MainMenuSceneUnloaded = new();

        public static readonly UnityEvent LoadingSceneLoaded = new();
        public static readonly UnityEvent LoadingSceneUnloaded = new();

        // Hint: Scene general is always loaded >after< world is fully filled with vobs etc.
        /// <summary>
        /// GameObject playerGo - as we spawn it the same frame, we call this event. But Unity can Find() it one frame later earliest.
        /// We therefore provide it to the event.
        /// </summary>
        public static readonly UnityEvent<GameObject> GeneralSceneLoaded = new();

        public static readonly UnityEvent GeneralSceneUnloaded = new();

        public static readonly UnityEvent WorldSceneLoaded = new();
        public static readonly UnityEvent WorldSceneUnloaded = new();

        public static readonly UnityEvent<DateTime> GameTimeSecondChangeCallback = new();
        public static readonly UnityEvent<DateTime> GameTimeMinuteChangeCallback = new();
        public static readonly UnityEvent<DateTime> GameTimeHourChangeCallback = new();

        public static readonly UnityEvent<GameObject> MusicZoneEntered = new();
        public static readonly UnityEvent<GameObject> MusicZoneExited = new();
        public static readonly UnityEvent<string, string> LevelChangeTriggered = new();
        
        public static readonly UnityEvent<string, object> PlayerPrefUpdated = new();

    }
}
