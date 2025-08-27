using System;
using GUZ.Core.Vm;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.Core.Properties.Vobs
{
    public class VobItemProperties2 : VobProperties2
    {
        public readonly ItemInstance Instance;

        public VobItemProperties2(IVirtualObject vob) : base(vob)
        {
            var vobItem = (IItem)vob;
            
            string itemName;
            if (!string.IsNullOrEmpty(vobItem.Instance))
                itemName = vobItem.Instance;
            else if (!string.IsNullOrEmpty(vobItem.Name))
                itemName = vobItem.Name;
            else
                throw new Exception("Vob Item -> no usable name found.");

            Instance = VmInstanceManager.TryGetItemData(itemName);
        }

        public override string GetFocusName()
        {
            var vobItem = VobAs<IItem>();
            var itemVmData = VmInstanceManager.TryGetItemData(vobItem.Name);
            
            if (vobItem?.Amount > 1)
                return $"{itemVmData?.Name} ({vobItem.Amount})";
            else
                return itemVmData?.Name;
        }
    }
}
