using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobInteractiveProperties : VobMovableProperties
    {
        public InteractiveObject InteractiveProperties => (InteractiveObject)Properties;

        // FIXME - We need to load the proper string value from Daedalus -> $"MOBNAME_{FocusName}"
        public override string FocusName => InteractiveProperties?.FocusName;
    }
}
