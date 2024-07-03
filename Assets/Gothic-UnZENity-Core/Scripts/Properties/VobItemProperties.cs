using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobItemProperties : VobProperties
    {
        public ItemInstance Item;
        public Item ItemProperties => (Item)Properties;

        public override string FocusName => Item.Name + "(" + ItemProperties.Amount + ")";

        public void SetData(Item data, ItemInstance item)
        {
            base.SetData(data);

            Item = item;
        }
    }
}
