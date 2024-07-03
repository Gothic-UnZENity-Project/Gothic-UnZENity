using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobDoorProperties : VobInteractiveProperties
    {
        public Door DoorProperties => (Door)Properties;

        // FIXME - We need to load the proper string value from Daedalus -> $"MOBNAME_{FocusName}"
        public override string FocusName => DoorProperties.FocusName;
    }
}
