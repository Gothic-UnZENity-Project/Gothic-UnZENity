using System;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MutableMenuInstance : AbstractMenuInstance
    {
        public MutableMenuInstance(string name, [CanBeNull] AbstractMenuInstance parentMenu)
            : base(name, parentMenu)
        {
            
        }

        public AbstractMenuItemInstance GetMenuItemInstance(string menuItemName)
        {
            throw new NotImplementedException();
        }

        public AbstractMenuInstance FindSubMenu(string subMenuName)
        {
            throw new NotImplementedException();
        }

        public override string GetItem(int i)
        {
            throw new NotImplementedException();
        }
    }
}
