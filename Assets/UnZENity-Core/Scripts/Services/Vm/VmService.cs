using GUZ.Core.Domain.Vm;
using GUZ.Core.Extensions;

namespace GUZ.Core.Services.Vm
{
    public class VmService
    {
        private VmExternalDomain _domain = new VmExternalDomain().Inject();


        public void RegisterExternals()
        {
            _domain.RegisterExternals();
        }
    }
}
