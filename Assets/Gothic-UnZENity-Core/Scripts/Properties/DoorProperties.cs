using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class DoorProperties : InteractiveProperties
    {
        public new Door Properties;

        public override void SetData(IVirtualObject data)
        {
            if (data is Door doorData)
            {
                Properties = doorData;
                base.Properties = doorData;
            }

            base.SetData(data);
        }
    }
}
