using System.Linq;
using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobProperties : AbstractProperties
    {
        public IVirtualObject Properties;

        public override string GetFocusName()
        {
            return Properties?.Name;
        }
    }
}
