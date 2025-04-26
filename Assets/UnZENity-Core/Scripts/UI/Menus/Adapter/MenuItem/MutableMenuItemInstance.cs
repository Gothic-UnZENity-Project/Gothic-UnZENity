using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public class MutableMenuItemInstance : IMenuItemInstance
    {
        public string FontName { get; set; }
        public string BackPic { get; set; }
        public string AlphaMode { get; set; }
        public int Alpha { get; set; }
        public MenuItemType MenuItemType { get; set; }
        public string OnChgSetOption { get; set; }
        public string OnChgSetOptionSection { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int DimX { get; set; }
        public int DimY { get; set; }
        public float SizeStartScale { get; set; }
        public MenuItemFlag Flags { get; set; }
        public float OpenDelayTime { get; set; }
        public float OpenDuration { get; set; }
        public int FramePosX { get; set; }
        public int FramePosY { get; set; }
        public int FrameSizeX { get; set; }
        public int FrameSizeY { get; set; }
        public string HideIfOptionSectionSet { get; set; }
        public string HideIfOptionSet { get; set; }
        public int HideOnValue { get; set; }
        
        public string GetText(int i)
        {
            throw new System.NotImplementedException();
        }

        public MenuItemSelectAction GetOnSelAction(int i)
        {
            throw new System.NotImplementedException();
        }

        public string GetOnSelActionS(int i)
        {
            throw new System.NotImplementedException();
        }

        public int GetOnEventAction(MenuItemEventAction i)
        {
            throw new System.NotImplementedException();
        }

        public float GetUserFloat(int i)
        {
            throw new System.NotImplementedException();
        }

        public string GetUserString(int i)
        {
            throw new System.NotImplementedException();
        }
    }
}
