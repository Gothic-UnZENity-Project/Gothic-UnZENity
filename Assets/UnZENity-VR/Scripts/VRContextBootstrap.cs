using System;
using GUZ.Core.Context;

namespace GUZ.VR
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class VRContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterModule(GuzContext.Controls controls)
        {
            if (controls != GuzContext.Controls.VR)
            {
                return;
            }

// We register VR only if we have HVR installed.
#if GUZ_HVR_INSTALLED
            GuzContext.InteractionAdapter = new VRInteractionAdapter();
            GuzContext.DialogAdapter = new VRDialogAdapter();
#else
            throw new ArgumentException(
                "VR context is set, but compiler directive >GUZ_HVR_INSTALLED< isn't set. Did you set up Hurricane VR properly?");
#endif
        }
    }
}
