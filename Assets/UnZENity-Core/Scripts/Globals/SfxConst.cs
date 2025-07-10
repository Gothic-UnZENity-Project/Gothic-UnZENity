using GUZ.Core.Vm;
using ZenKit.Daedalus;

namespace GUZ.Core.Globals
{
    public static class SfxConst
    {
        public static SoundEffectInstance InvOpen => VmInstanceManager.TryGetSfxData("INV_OPEN");
        public static SoundEffectInstance InvClose => VmInstanceManager.TryGetSfxData("INV_CLOSE");

    }
}
