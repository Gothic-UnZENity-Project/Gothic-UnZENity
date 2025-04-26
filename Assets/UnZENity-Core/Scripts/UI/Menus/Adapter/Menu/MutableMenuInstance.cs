using GUZ.Core.UI.Menus.Adapter.MenuItem;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public class MutableMenuInstance : IMenuInstance
    {
        public IMenuItemInstance GetMenuItemInstance(string menuItemName)
        {
            throw new System.NotImplementedException();
        }

        public string GetItem(int i)
        {
            throw new System.NotImplementedException();
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
