using GUZ.Core.Services.Caches;
using Reflex.Attributes;
using ZenKit.Daedalus;

namespace GUZ.Core.Services.Audio
{
    public class SfxService
    {
        public const string NoSoundName = "nosound.wav";

        [Inject] private readonly VmCacheService _vmCacheService;

        
        public SoundEffectInstance InvOpen => _vmCacheService.TryGetSfxData("INV_OPEN").GetFirstSound();
        public SoundEffectInstance InvClose => _vmCacheService.TryGetSfxData("INV_CLOSE").GetFirstSound();
    }
}
