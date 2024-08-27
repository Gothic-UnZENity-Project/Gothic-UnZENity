using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.Core.Properties
{
    public class VobItemProperties : VobProperties
    {
        public ItemInstance Item;
        public Item ItemProperties => (Item)Properties;


        public void SetData(Item data, ItemInstance item)
        {
            base.SetData(data);

            Item = item;
        }

        public override string GetFocusName()
        {
            if (ItemProperties?.Amount > 1)
            {
                return $"{Item.Name} ({ItemProperties.Amount})";
            }
            else
            {
                return Item.Name;
            }

        }
    }
}
