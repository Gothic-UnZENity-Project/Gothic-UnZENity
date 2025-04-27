using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MenuInstanceAdapter : IMenuInstance
    {
        public string Name { get; set; }
        public IMenuInstance Parent { get; set; }
        public IMenuInstance ParentAbstract { get; set; }
        public List<IMenuItemInstance> Items { get; set; }

        private readonly MenuInstance _menuInstance;

        public MenuInstanceAdapter(string name, [CanBeNull] IMenuInstance parentAbstractMenu)
        {
            Name = name;
            ParentAbstract = parentAbstractMenu;
            
            _menuInstance = GameData.MenuVm.InitInstance<MenuInstance>(name);
            
            // We immediately initialize all menu entries as we will later change Index of them (e.g. add a new menu in between).
            Items = new();
            for (var i = 0;; i++)
            {
                var itemName = _menuInstance.GetItem(i);

                // We passed the last element.
                if (itemName.IsNullOrEmpty())
                    break;

                var instance = GameData.MenuVm.InitInstance<MenuItemInstance>(itemName);
                Items.Add(new MenuItemInstanceAdapter(instance, itemName, this));
            }
        }

        public void InsertItemAt(int index, IMenuItemInstance menuItemInstance)
        {
            Items.Insert(index, menuItemInstance);
        }
        
        public IMenuItemInstance GetMenuItemInstance(string menuItemName)
        {
            return Items.First(i => i.Name == menuItemName);
        }

        public void FindMenuItem(string menuItemName, out IMenuItemInstance menuItemInstance, out int index)
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

        public void ReplaceItemAt(int index, IMenuItemInstance item)
        {
            throw new NotImplementedException();
        }

        public string GetItem(int i)
        {
            return _menuInstance.GetItem(i);
        }

        public string BackPic
        {
            get => _menuInstance.BackPic;
            set => _menuInstance.BackPic = value;
        }

        public string BackWorld
        {
            get => _menuInstance.BackWorld;
            set => _menuInstance.BackWorld = value;
        }

        public int PosX
        {
            get => _menuInstance.PosX;
            set => _menuInstance.PosX = value;
        }

        public int PosY
        {
            get => _menuInstance.PosY;
            set => _menuInstance.PosY = value;
        }

        public int DimX
        {
            get => _menuInstance.DimX;
            set => _menuInstance.DimX = value;
        }

        public int DimY
        {
            get => _menuInstance.DimY;
            set => _menuInstance.DimY = value;
        }

        public int Alpha
        {
            get => _menuInstance.Alpha;
            set => _menuInstance.Alpha = value;
        }

        public string MusicTheme
        {
            get => _menuInstance.MusicTheme;
            set => _menuInstance.MusicTheme = value;
        }

        public int EventTimerMsec
        {
            get => _menuInstance.EventTimerMsec;
            set => _menuInstance.EventTimerMsec = value;
        }

        public MenuFlag Flags
        {
            get => _menuInstance.Flags;
            set => _menuInstance.Flags = value;
        }

        public int DefaultOutGame
        {
            get => _menuInstance.DefaultOutGame;
            set => _menuInstance.DefaultOutGame = value;
        }

        public int DefaultInGame
        {
            get => _menuInstance.DefaultInGame;
            set => _menuInstance.DefaultInGame = value;
        }
    }
}
