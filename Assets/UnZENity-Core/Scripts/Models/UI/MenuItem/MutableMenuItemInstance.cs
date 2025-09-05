using System;
using GUZ.Core.Const;
using ZenKit.Daedalus;

namespace GUZ.Core.Model.UI.MenuItem
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
            
            // for Slider: .tga of slider button
            SetUserString(0, reference.GetUserString(0));
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

        private MenuItemSelectAction[] _onSelActions = new MenuItemSelectAction[Constants.DaedalusMenu.MaxSelActions];
        public override MenuItemSelectAction GetOnSelAction(int i)
        {
            return _onSelActions[i];
        }

        public override void SetOnSelAction(int i, MenuItemSelectAction action)
        {
            _onSelActions[i] = action;
        }

        private string[] _onSelActionStrings = new string[Constants.DaedalusMenu.MaxSelActions];
        public override string GetOnSelActionS(int i)
        {
            return _onSelActionStrings[i];
        }

        public override void SetOnSelActionS(int i, string actionS)
        {
            _onSelActionStrings[i] = actionS;
        }

        public override int GetOnEventAction(MenuItemEventAction i)
        {
            throw new NotImplementedException();
        }

        private float[] _userFloats = new float[Constants.DaedalusMenu.MaxUserVars];
        public override float GetUserFloat(int i)
        {
            return _userFloats[i];
        }

        public override void SetUserFloat(int i, float value)
        {
            _userFloats[i] = value;
        }

        private string[] _userStrings = new string[Constants.DaedalusMenu.MaxUserVars];
        public override string GetUserString(int i)
        {
            return _userStrings[i];
        }

        public override void SetUserString(int i, string text)
        {
            _userStrings[i] = text;
        }
    }
}
