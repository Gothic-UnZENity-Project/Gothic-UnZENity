using System;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MutableMenuInstance : AbstractMenuInstance
    {
        public MutableMenuInstance(string name, [CanBeNull] AbstractMenuInstance parentMenu)
            : base(name, parentMenu)
        {
            BackPic = Constants.DaedalusMenu.BackPic;
            Items = new();
            DimX = 8191; // Taken from PROTOTYPE C_MENU_DEF
            DimY = 8191; // Taken from PROTOTYPE C_MENU_DEF
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
