using System.Collections.Generic;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using JetBrains.Annotations;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.Menu
{
    public interface IMenuInstance
    {
        public string Name {get; set;}
        List<IMenuItemInstance> Items { get; set; }
        IMenuItemInstance GetMenuItemInstance(string menuItemName);

        [CanBeNull] public IMenuInstance FindSubMenu(string subMenuName);
        
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
