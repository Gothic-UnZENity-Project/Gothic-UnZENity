using System.Collections.Generic;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MutableAbstractMenuInstance : IMenuInstance
    {
        public string Name { get; set; }
        public IMenuInstance Parent { get; set; }
        public List<IMenuItemInstance> Items { get; set; }

        public IMenuItemInstance GetMenuItemInstance(string menuItemName)
        {
            throw new System.NotImplementedException();
        }

        public IMenuInstance FindSubMenu(string subMenuName)
        {
            throw new System.NotImplementedException();
        }

        public void FindMenuItem(string menuItemName, out IMenuItemInstance menuItemInstance, out int index)
        {
            throw new System.NotImplementedException();
        }

        public void ReplaceItemAt(int index, IMenuItemInstance item)
        {
            Items[index] = item;
        }

        public string GetItem(int i)
        {
            return "";
        }

        public string BackPic { get; set; }
        public string BackWorld { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int DimX { get; set; }
        public int DimY { get; set; }
        public int Alpha { get; set; }
        public string MusicTheme { get; set; }
        public int EventTimerMsec { get; set; }
        public MenuFlag Flags { get; set; }
        public int DefaultOutGame { get; set; }
        public int DefaultInGame { get; set; }
    }
}
