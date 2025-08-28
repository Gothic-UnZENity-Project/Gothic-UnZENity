using GUZ.Core.Data.Container;
using GUZ.Core.Domain.Culling;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Services.Culling
{
    public class VobSoundCullingService : AbstractCullingService
    {
        private VobSoundCullingDomain _soundDomain = new();


        public VobSoundCullingService()
        {
            Domain = _soundDomain;
        }
        
        public void AddCullingEntry(VobContainer container)
        {
            _soundDomain.AddCullingEntry(container.Go, container.VobAs<ISound>());
        }
        
        public void AddCullingEntry(GameObject go, ISound vob)
        {
            _soundDomain.AddCullingEntry(go, vob);
        }
    }
}
