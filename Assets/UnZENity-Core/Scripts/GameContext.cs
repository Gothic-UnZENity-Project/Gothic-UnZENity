using System;
using GUZ.Core.Adapter;
using ZenKit;
#if GUZ_HVR_INSTALLED
#endif

namespace GUZ.Core
{
    public static class GameContext
    {
        public static bool IsControlsInitialized;
        public static bool IsGameVersionInitialized;

        public static IInteractionAdapter InteractionAdapter;
        public static IDialogAdapter DialogAdapter;
        public static ISubtitlesAdapter SubtitlesAdapter;
        public static IGameVersionAdapter GameVersionAdapter;

        public static bool IsLab;

        public enum Controls
        {
            VR,
            Flat
        }

        public static void SetControlContext(Controls controls)
        {
            GlobalEventDispatcher.RegisterControlAdapters.Invoke(controls);
            
            if (InteractionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No control module registered for {controls}");
            }

            IsControlsInitialized = true;
        }
        
        public static void SetGameVersionContext(GameVersion version)
        {
            GlobalEventDispatcher.RegisterGameVersionAdapters.Invoke(version);
            
            if (GameVersionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No version module registered for {version}");
            }

            IsGameVersionInitialized = true;
        }
    }
}
