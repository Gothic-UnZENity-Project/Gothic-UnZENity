using System;
using UnityEngine.Events;
using ZenKit;
#if GUZ_HVR_INSTALLED
#endif

namespace GUZ.Core.Context
{
    public static class GUZContext
    {
        // We need to ensure, that other modules will register themselves based on current Control+GameMode setting.
        // Since we can't call them (e.g. Flat/VR) directly, we need to leverage this IoC pattern.
        public static readonly UnityEvent<Controls> RegisterControlAdapters = new();
        public static readonly UnityEvent<GameVersion> RegisterGameVersionAdapters = new();

        public static IInteractionAdapter InteractionAdapter;
        public static IDialogAdapter DialogAdapter;
        public static IGameVersionAdapter GameVersionAdapter;

        public enum Controls
        {
            VR,
            Flat
        }

        public static void SetControlContext(Controls controls)
        {
            RegisterControlAdapters.Invoke(controls);
            
            if (InteractionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No control module registered for {controls}");
            }
        }
        
        public static void SetGameVersionContext(GameVersion version)
        {
            RegisterGameVersionAdapters.Invoke(version);
            
            if (GameVersionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No version module registered for {version}");
            }
        }
    }
}
