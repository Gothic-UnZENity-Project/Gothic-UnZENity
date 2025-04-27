using GUZ.Core.UI.Menus.Adapter.Menu;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public interface IMenuItemInstance
    {
        string Name { get; set; }
        IMenuInstance MenuInstance { get; set; }
        
        string FontName { get; set; }
        string BackPic { get; set; }
        string AlphaMode { get; set; }
        int Alpha { get; set; }
        MenuItemType MenuItemType { get; set; }
        string OnChgSetOption { get; set; }
        string OnChgSetOptionSection { get; set; }
        int PosX { get; set; }
        int PosY { get; set; }
        int DimX { get; set; }
        int DimY { get; set; }
        float SizeStartScale { get; set; }
        MenuItemFlag Flags { get; set; }
        float OpenDelayTime { get; set; }
        float OpenDuration { get; set; }
        int FramePosX { get; set; }
        int FramePosY { get; set; }
        int FrameSizeX { get; set; }
        int FrameSizeY { get; set; }
        string HideIfOptionSectionSet { get; set; }
        string HideIfOptionSet { get; set; }
        int HideOnValue { get; set; }
        string GetText(int i);
        MenuItemSelectAction GetOnSelAction(int i);
        string GetOnSelActionS(int i);
        int GetOnEventAction(MenuItemEventAction i);
        float GetUserFloat(int i);
        string GetUserString(int i);
    }
}
