using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class InteractiveProperties : MovableProperties
    {
        public new InteractiveObject Properties;

        public override void SetData(IVirtualObject data)
        {
            if (data is InteractiveObject interactiveData)
            {
                Properties = interactiveData;
                base.Properties = interactiveData;
            }

            base.SetData(data);
        }
    }
}
