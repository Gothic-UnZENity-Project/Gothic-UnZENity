using GUZ.Core;
using GUZ.Core.Adapter;
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
        protected override void RegisterControlModule(GameContext.Controls controls)
        {
            if (controls != GameContext.Controls.VR)
            {
                return;
            }

// We register VR only if we have HVR installed.
#if GUZ_HVR_INSTALLED
            GameContext.InteractionAdapter = new VRInteractionAdapter();
            GameContext.DialogAdapter = new VRDialogAdapter();
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
