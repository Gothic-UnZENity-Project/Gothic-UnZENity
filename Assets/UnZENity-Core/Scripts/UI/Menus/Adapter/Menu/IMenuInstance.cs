using System.Collections.Generic;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public interface IMenuInstance
    {
        public string Name {get; set;}
        public IMenuInstance Parent {get; set;}
        List<IMenuItemInstance> Items { get; set; }

        [CanBeNull]
        IMenuInstance FindSubMenu(string subMenuName)
        {
            if (this.Name == subMenuName)
                return this;
    
            foreach (var menuItem in Items)
            {
                var foundMenu = menuItem.AbstractMenuInstance?.FindSubMenu(subMenuName);
                if (foundMenu != null)
                    return foundMenu;
            }

            return null;
        }
        
        void FindMenuItem(string menuItemName, out IMenuItemInstance menuItemInstance, out int index);
        void ReplaceItemAt(int index, IMenuItemInstance item);
        
        string GetItem(int i);
        
        string BackPic { get; set; }
        string BackWorld { get; set; }
        int PosX { get; set; }
        int PosY { get; set; }
        int DimX { get; set; }
        int DimY { get; set; }
        int Alpha { get; set; }
        string MusicTheme { get; set; }
        int EventTimerMsec { get; set; }
        MenuFlag Flags { get; set; }
        int DefaultOutGame { get; set; }
        int DefaultInGame { get; set; }
    }
}
