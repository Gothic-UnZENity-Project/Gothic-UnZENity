using System;
using UnityEngine.Events;
#if GUZ_HVR_INSTALLED
#endif

namespace GUZ.Core.Context
{
    public static class GuzContext
    {
        // We need to ensure, that other modules will register themselves based on current Control setting.
        // Since we can't call them (e.g. Flat/VR) directly, we need to leverage this IoC pattern.
        public static readonly UnityEvent<Controls> RegisterAdapters = new();

        public static IInteractionAdapter InteractionAdapter;
        public static IDialogAdapter DialogAdapter;

        public enum Controls
        {
            VR,
            Flat
        }

        public static void SetContext(Controls controls)
        {
            RegisterAdapters.Invoke(controls);
            
            if (InteractionAdapter == null)
            {
                throw new ArgumentOutOfRangeException($"No control module registered for {controls}");
            }
        }

//         private static void SetFlatContext()
//         {
//             Debug.Log("Selecting Context: Flat");
//             InteractionAdapter = new FlatInteractionAdapter();
//         }
//
//         private static void SetVRContext()
//         {
// #if GUZ_HVR_INSTALLED
//             Debug.Log("Selecting Context: VR");
//             InteractionAdapter = new HVRInteractionAdapter();
//             DialogAdapter = new HVRDialogAdapter();
// #else
//             throw new Exception("Hurricane VR isn't activated inside Player Settings. Please do before you use it.");
// #endif
//         }
    }
}
