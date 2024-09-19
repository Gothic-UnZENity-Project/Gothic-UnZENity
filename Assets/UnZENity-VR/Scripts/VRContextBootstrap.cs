using GUZ.Core.Context;
using ZenKit;
#if !GUZ_HVR_INSTALLED
using System;
#endif

namespace GUZ.VR
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class VRContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterControlModule(GUZContext.Controls controls)
        {
            if (controls != GUZContext.Controls.VR)
            {
                return;
            }

// We register VR only if we have HVR installed.
#if GUZ_HVR_INSTALLED
            GUZContext.InteractionAdapter = new VRInteractionAdapter();
            GUZContext.DialogAdapter = new VRDialogAdapter();
#else
            throw new ArgumentException(
                "VR context is set, but compiler directive >GUZ_HVR_INSTALLED< isn't set. Did you set up Hurricane VR properly?");
#endif
        }

        protected override void RegisterGameVersionModule(GameVersion version)
        {
            // NOP
        }
    }
}
