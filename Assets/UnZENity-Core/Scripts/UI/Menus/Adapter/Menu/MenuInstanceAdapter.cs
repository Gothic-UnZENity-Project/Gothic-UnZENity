using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MenuInstanceAdapter : IMenuInstance
    {
        private readonly MenuInstance _menuInstance;
    
        public MenuInstanceAdapter(string name)
        {
            _menuInstance = GameData.MenuVm.InitInstance<MenuInstance>(name);
        }
        
        public MenuInstanceAdapter(MenuInstance menuInstance)
        {
            _menuInstance = menuInstance;
        }

        public IMenuItemInstance GetMenuItemInstance(string menuItemName)
        {
            var itemInstance = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);
            return new MenuItemInstanceAdapter(itemInstance);
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
