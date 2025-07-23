using GUZ.Core.Vm;
using ZenKit.Daedalus;

namespace GUZ.Core.Globals
{
    public static class SfxConst
    {
        public const string NoSoundName = "nosound.wav";

        
        public static SoundEffectInstance InvOpen => VmInstanceManager.TryGetSfxData("INV_OPEN").GetFirstSound();
        public static SoundEffectInstance InvClose => VmInstanceManager.TryGetSfxData("INV_CLOSE").GetFirstSound();
    }
}
