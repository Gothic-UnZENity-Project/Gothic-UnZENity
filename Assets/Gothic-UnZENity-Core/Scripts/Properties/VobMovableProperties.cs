using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobMovableProperties : VobProperties
    {
        public MovableObject MovableProperties => (MovableObject)Properties;

        // FIXME - We need to load the proper string value from Daedalus -> $"MOBNAME_{FocusName}"
        public override string FocusName => MovableProperties.FocusName;
    }
}
