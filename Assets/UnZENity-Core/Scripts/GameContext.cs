using System;
using GUZ.Core._Adapter;
using GUZ.Core.Services.Context;
using ZenKit;
#if GUZ_HVR_INSTALLED
#endif

namespace GUZ.Core
{
    [Obsolete("Move to separate Context*Service classes injected via DI.")]
    public static class GameContext
    {
        public static bool IsZenKitInitialized;
        public static bool IsControlsInitialized;
        public static bool IsGameVersionInitialized;

        public static string GameLanguage;

        public static ContextInteractionService ContextInteractionService;
        public static IContextMenuService ContextMenuService;
        public static IContextDialogService ContextDialogService;
        public static IGameVersionAdapter GameVersionAdapter;

        public static bool IsLab;

        public enum Controls
        {
            VR,
            Flat
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
