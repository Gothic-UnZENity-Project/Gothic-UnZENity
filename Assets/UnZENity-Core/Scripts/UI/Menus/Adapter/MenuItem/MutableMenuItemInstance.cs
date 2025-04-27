using System;
using System.Collections.Generic;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus.Adapter.MenuItem
{
    public class MutableMenuItemInstance : AbstractMenuItemInstance
    {
        /// <summary>
        /// Copy constructor. Mostly copying dimensions and look.
        /// </summary>
        public MutableMenuItemInstance(string menuItemName, AbstractMenuItemInstance reference): base(menuItemName)
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

        private string[] _texts = new string[Constants.DaedalusMenu.MaxUserStrings];
        public override string GetText(int i)
        {
            return _texts[i];
        }

        public override void SetText(int i, string text)
        {
            _texts[i] = text;
        }

        public override MenuItemSelectAction GetOnSelAction(int i)
        {
            throw new NotImplementedException();
        }

        public override string GetOnSelActionS(int i)
        {
            throw new NotImplementedException();
        }

        public override int GetOnEventAction(MenuItemEventAction i)
        {
            throw new NotImplementedException();
        }

        public override float GetUserFloat(int i)
        {
            throw new NotImplementedException();
        }

        public override string GetUserString(int i)
        {
            throw new NotImplementedException();
        }
    }
}
