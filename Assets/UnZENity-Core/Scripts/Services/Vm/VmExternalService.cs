using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Domain.Vm;
using GUZ.Core.Extensions;
using Reflex.Attributes;

namespace GUZ.Core.Services.Vm
{
    public class VmExternalService
    {
        private VmExternalDomain _domain = new VmExternalDomain().Inject();
        
        public void RegisterExternals()
        {
            _domain.RegisterExternals();
        }
    }
}
