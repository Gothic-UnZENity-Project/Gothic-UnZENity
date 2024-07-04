using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobContainerProperties : VobInteractiveProperties
    {
        public Container ContainerProperties => (Container)Properties;

        // FIXME - We need to load the proper string value from Daedalus -> $"MOBNAME_{FocusName}"
        public override string FocusName => ContainerProperties?.FocusName;
    }
}
