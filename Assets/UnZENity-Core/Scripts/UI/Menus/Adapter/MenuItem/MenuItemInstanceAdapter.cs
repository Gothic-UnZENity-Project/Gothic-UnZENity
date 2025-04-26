using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public class MenuItemInstanceAdapter : IMenuItemInstance
    {
        private readonly MenuItemInstance _menuItemInstance;
    
        public MenuItemInstanceAdapter(MenuItemInstance menuItemInstance)
        {
            _menuItemInstance = menuItemInstance;
        }

        public string FontName
        {
            get => _menuItemInstance.FontName;
            set => _menuItemInstance.FontName = value;
        }

        public string BackPic
        {
            get => _menuItemInstance.BackPic;
            set => _menuItemInstance.BackPic = value;
        }

        public string AlphaMode
        {
            get => _menuItemInstance.AlphaMode;
            set => _menuItemInstance.AlphaMode = value;
        }

        public int Alpha
        {
            get => _menuItemInstance.Alpha;
            set => _menuItemInstance.Alpha = value;
        }

        public MenuItemType MenuItemType
        {
            get => _menuItemInstance.MenuItemType;
            set => _menuItemInstance.MenuItemType = value;
        }

        public string OnChgSetOption
        {
            get => _menuItemInstance.OnChgSetOption;
            set => _menuItemInstance.OnChgSetOption = value;
        }

        public string OnChgSetOptionSection
        {
            get => _menuItemInstance.OnChgSetOptionSection;
            set => _menuItemInstance.OnChgSetOptionSection = value;
        }

        public int PosX
        {
            get => _menuItemInstance.PosX;
            set => _menuItemInstance.PosX = value;
        }

        public int PosY
        {
            get => _menuItemInstance.PosY;
            set => _menuItemInstance.PosY = value;
        }

        public int DimX
        {
            get => _menuItemInstance.DimX;
            set => _menuItemInstance.DimX = value;
        }

        public int DimY
        {
            get => _menuItemInstance.DimY;
            set => _menuItemInstance.DimY = value;
        }

        public float SizeStartScale
        {
            get => _menuItemInstance.SizeStartScale;
            set => _menuItemInstance.SizeStartScale = value;
        }

        public MenuItemFlag Flags
        {
            get => _menuItemInstance.Flags;
            set => _menuItemInstance.Flags = value;
        }

        public float OpenDelayTime
        {
            get => _menuItemInstance.OpenDelayTime;
            set => _menuItemInstance.OpenDelayTime = value;
        }

        public float OpenDuration
        {
            get => _menuItemInstance.OpenDuration;
            set => _menuItemInstance.OpenDuration = value;
        }

        public int FramePosX
        {
            get => _menuItemInstance.FramePosX;
            set => _menuItemInstance.FramePosX = value;
        }

        public int FramePosY
        {
            get => _menuItemInstance.FramePosY;
            set => _menuItemInstance.FramePosY = value;
        }

        public int FrameSizeX
        {
            get => _menuItemInstance.FrameSizeX;
            set => _menuItemInstance.FrameSizeX = value;
        }

        public int FrameSizeY
        {
            get => _menuItemInstance.FrameSizeY;
            set => _menuItemInstance.FrameSizeY = value;
        }

        public string HideIfOptionSectionSet
        {
            get => _menuItemInstance.HideIfOptionSectionSet;
            set => _menuItemInstance.HideIfOptionSectionSet = value;
        }

        public string HideIfOptionSet
        {
            get => _menuItemInstance.HideIfOptionSet;
            set => _menuItemInstance.HideIfOptionSet = value;
        }

        public int HideOnValue
        {
            get => _menuItemInstance.HideOnValue;
            set => _menuItemInstance.HideOnValue = value;
        }

        public string GetText(int i)
        {
            return _menuItemInstance.GetText(i);
        }

        public MenuItemSelectAction GetOnSelAction(int i)
        {
            return _menuItemInstance.GetOnSelAction(i);
        }

        public string GetOnSelActionS(int i)
        {
            return _menuItemInstance.GetOnSelActionS(i);
        }

        public int GetOnEventAction(MenuItemEventAction i)
        {
            return _menuItemInstance.GetOnEventAction(i);
        }

        public float GetUserFloat(int i)
        {
            return _menuItemInstance.GetUserFloat(i);
        }

        public string GetUserString(int i)
        {
            return _menuItemInstance.GetUserString(i);
        }
    }
}
