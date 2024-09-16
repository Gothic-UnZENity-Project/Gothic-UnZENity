using System;
using UnityEngine.Events;
using ZenKit;
#if GUZ_HVR_INSTALLED
#endif

namespace GUZ.Core.Context
{
    public static class GuzContext
    {
        // We need to ensure, that other modules will register themselves based on current Control setting.
        // Since we can't call them (e.g. Flat/VR) directly, we need to leverage this IoC pattern.
        public static readonly UnityEvent<Controls, GameVersion> RegisterAdapters = new();

        public static IInteractionAdapter InteractionAdapter;
        public static IDialogAdapter DialogAdapter;
        public static IGameVersionAdapter GameVersionAdapter;

        public enum Controls
        {
            VR,
            Flat
        }

        public static void SetContext(Controls controls, GameVersion version)
        {
            RegisterAdapters.Invoke(controls, version);
            
            if (InteractionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No control module registered for {controls}");
            }

            if (GameVersionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No version module registered for {version}");
            }
        }
    }
}
