using GUZ.Core;
using GUZ.Core._Adapter;
using GUZ.VR.Services.Context;
using ZenKit;
#if GUZ_HVR_INSTALLED
using GUZ.VR.Adapter;
#endif

namespace GUZ.VR
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class VRContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterGameVersionModule(GameVersion version)
        {
            // NOP
        }
    }
}
