using System.Collections.Generic;
using System.Linq;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public abstract class AbstractMenuInstance
    {
        public string Name;
        public AbstractMenuInstance Parent;
        public List<AbstractMenuItemInstance> Items;

        protected AbstractMenuInstance(string name, [CanBeNull] AbstractMenuInstance parentMenu)
        {
            Name = name;
            Parent = parentMenu;
        }

        public List<string> GetMenuInstanceNamesRecursive()
        {
            var menuInstanceNames = new List<string>();

            menuInstanceNames.Add(Name);

            if (Items == null)
            {
                return menuInstanceNames;
            }

            foreach (var item in Items)
            {
                if (item.MenuInstance != null)
                {
                    menuInstanceNames.AddRange(item.MenuInstance.GetMenuInstanceNamesRecursive());
                }
            }

            return menuInstanceNames;
        }
        
        [CanBeNull]
        public AbstractMenuInstance FindMenu(string subMenuName)
        {
            if (Name == subMenuName)
                return this;
    
            foreach (var menuItem in Items)
            {
                var foundMenu = menuItem.MenuInstance?.FindMenu(subMenuName);
                if (foundMenu != null)
                    return foundMenu;
            }

            return null;
        }

        public void FindMenuItem(string menuItemName, out AbstractMenuItemInstance menuItemInstance, out int index)
        {
            menuItemInstance = null;
            index = -1;
            
            for (var i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (item.Name == menuItemName)
                {
                    menuItemInstance = item;
                    index = i;
                    return;
                }
            }
        }

        public void ReplaceItemAt(int index, AbstractMenuItemInstance item)
        {
            Items[index] = item;
        }
        
        public abstract string GetItem(int i);

        public virtual string BackPic { get; set; }
        public virtual string BackWorld { get; set; }
        public virtual int PosX { get; set; }
        public virtual int PosY { get; set; }
        public virtual int DimX { get; set; }
        public virtual int DimY { get; set; }
        public virtual int Alpha { get; set; }
        public virtual string MusicTheme { get; set; }
        public virtual int EventTimerMsec { get; set; }
        public virtual MenuFlag Flags { get; set; }
        public virtual int DefaultOutGame { get; set; }
        public virtual int DefaultInGame { get; set; }
    }
}
