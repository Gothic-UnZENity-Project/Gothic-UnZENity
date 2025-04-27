using System;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public class MutableMenuItemInstance : IMenuItemInstance
    {
        public MutableMenuItemInstance()
        {
            // Set default values based on G1 C_MENU_ITEM_DEF
            FontName = GameData.MenuVm.GetSymbolByName("MENU_FONT_DEFAULT").GetString(0);
            MenuItemType = MenuItemType.Text;
            Flags = MenuItemFlag.ChromaKeyed | MenuItemFlag.Transparent | MenuItemFlag.Selectable;
            PosX = 0;
            PosY = 0;
            DimX = -1;
            DimY = -1;
        }

        public string Name { get; set; }
        public IMenuInstance MenuInstance { get; set; }
        
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

        public string[] Texts;
        public string GetText(int i)
        {
            return Texts.Length > i ? Texts[i] : string.Empty;
        }

        public MenuItemSelectAction GetOnSelAction(int i)
        {
            throw new NotImplementedException();
        }

        public string GetOnSelActionS(int i)
        {
            throw new NotImplementedException();
        }

        public int GetOnEventAction(MenuItemEventAction i)
        {
            throw new NotImplementedException();
        }

        public float GetUserFloat(int i)
        {
            throw new NotImplementedException();
        }

        public string GetUserString(int i)
        {
            throw new NotImplementedException();
        }
    }
}
