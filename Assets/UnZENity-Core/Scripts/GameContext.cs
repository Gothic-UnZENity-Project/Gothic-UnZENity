using System;
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

        public static ContextInteractionService ContextInteractionService;
        public static IContextDialogService ContextDialogService;
        public static IContextGameVersionService ContextGameVersionService;

        public static bool IsLab;

        public enum Controls
        {
            VR,
            Flat
        }
    }
}
