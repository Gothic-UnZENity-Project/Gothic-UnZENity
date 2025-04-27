using System;
using System.Collections.Generic;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public class MutableMenuItemInstance : IMenuItemInstance
    {
        /// <summary>
        /// Copy constructor. Mostly copying dimensions and look.
        /// </summary>
        public MutableMenuItemInstance(IMenuItemInstance reference)
        {
            FontName = reference.FontName;
            BackPic = reference.BackPic;
            AlphaMode = reference.AlphaMode;
            Alpha = reference.Alpha;
            MenuItemType = reference.MenuItemType;
            OnChgSetOption = reference.OnChgSetOption;
            OnChgSetOptionSection = reference.OnChgSetOptionSection;
            PosX = reference.PosX;
            PosY = reference.PosY;
            DimX = reference.DimX;
            DimY = reference.DimY;
            SizeStartScale = reference.SizeStartScale;
            Flags = reference.Flags;
            OpenDelayTime = reference.OpenDelayTime;
            OpenDuration = reference.OpenDuration;
            FramePosX = reference.FramePosX;
            FramePosY = reference.FramePosY;
            FrameSizeX = reference.FrameSizeX;
            FrameSizeY = reference.FrameSizeY;
            HideIfOptionSectionSet = reference.HideIfOptionSectionSet;
            HideIfOptionSet = reference.HideIfOptionSet;
            HideOnValue = reference.HideOnValue;
        }

        public string Name { get; set; }
        public IMenuInstance AbstractMenuInstance { get; set; }
        
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

        private string[] _texts = new string[Constants.DaedalusMenu.MaxUserStrings];
        public string GetText(int i)
        {
            return _texts[i];
        }

        public void SetText(int i, string text)
        {
            _texts[i] = text;
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
