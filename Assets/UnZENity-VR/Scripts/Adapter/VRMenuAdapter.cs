using GUZ.Core.Adapter;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using GUZ.VR.Manager;
using ZenKit.Daedalus;

namespace GUZ.VR.Adapter
{
    public class VRMenuAdapter : IMenuAdapter
    {
        /// <summary>
        /// Update menu entries based on VR needs.
        /// --> Replace control menu with VR settings menu
        /// </summary>
        public void UpdateMainMenu(AbstractMenuInstance mainMenu)
        {
            var vrControlsMenu = CreateVrMenu(mainMenu);

            // Will be used to clone render settings from its items.
            var gameMenu = mainMenu.FindMenuRecursive("MENU_OPT_GAME")!;

            vrControlsMenu.Items.Add(CreateHeadline(gameMenu));
            vrControlsMenu.Items.Add(CreateSmoothSpectatorLabel(gameMenu));
            vrControlsMenu.Items.Add(CreateSmoothSpectatorChoicebox(gameMenu));

            vrControlsMenu.Items.Add(CreateBackButton(mainMenu));
        }

        private MutableMenuInstance CreateVrMenu(AbstractMenuInstance mainMenu)
        {
            // Find OPT_CONTROLS menu item to overwrite
            var controlsMenuParent = mainMenu.FindMenuRecursive("MENU_OPT_CONTROLS")!.Parent;
            var controlsMenuItem = controlsMenuParent.FindMenuItem("MENUITEM_OPT_CONTROLS", out var controlsItemIndex);
            
            // Create empty menu
            var vrControlsMenu = new MutableMenuInstance("MENU_UNZENITY_OPT_VR", controlsMenuParent);
            
            // Create menu item and replace it where >Keyboard< settings are normally
            var vrControlsMenuItem = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR", controlsMenuItem);
            controlsMenuParent.ReplaceItemAt(controlsItemIndex, vrControlsMenuItem);

            // Add some setting
            vrControlsMenuItem.SetText(0, VRMenuLocalization.GetText("menuitem.vr"));
            vrControlsMenuItem.SetOnSelAction(0, MenuItemSelectAction.StartMenu);
            vrControlsMenuItem.SetOnSelActionS(0, "MENU_UNZENITY_OPT_VR");
            vrControlsMenuItem.MenuInstance = vrControlsMenu;

            return vrControlsMenu;
        }

        private MutableMenuItemInstance CreateHeadline(AbstractMenuInstance gameMenu)
        {
            var gameHeadline = gameMenu.FindMenuItem("MENUITEM_GAME_HEADLINE", out _);
            var vrHeadline = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_HEADLINE", gameHeadline);
            vrHeadline.SetText(0, VRMenuLocalization.GetText("menuitem.vr.headline"));
            
            return vrHeadline;
        }

        private MutableMenuItemInstance CreateSmoothSpectatorLabel(AbstractMenuInstance gameMenu)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var smoothingLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_SMOOTHSPECTATOR", subtitlesLabel);
            smoothingLabel.SetText(0, VRMenuLocalization.GetText("menuitem.smooth.label"));
            smoothingLabel.SetText(1, VRMenuLocalization.GetText("menuitem.smooth.description"));
            
            return smoothingLabel;
        }
        
        private MutableMenuItemInstance CreateSmoothSpectatorChoicebox(AbstractMenuInstance gameMenu)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var smoothingSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_SMOOTHSPECTATOR_CHOICE", subtitlesChoice);
            smoothingSetting.SetText(0, VRMenuLocalization.GetText("menuitem.smooth.value"));
            smoothingSetting.OnChgSetOption = VRConstants.IniNames.SmoothSpectator;
            smoothingSetting.OnChgSetOptionSection = Constants.IniSection;
            
            return smoothingSetting;
        }

        private MutableMenuItemInstance CreateBackButton(AbstractMenuInstance mainMenu)
        {
            var someOptionsMenu = mainMenu.FindMenuRecursive("MENU_OPT_GRAPHICS")!;
            var backButtonReference = someOptionsMenu.FindMenuItem("MENUITEM_GRA_BACK", out _)!;
            var backButton = new MutableMenuItemInstance("MENU_UNZENITY_OPT_VR_BACK", backButtonReference);
            
            backButton.SetText(0, backButtonReference.GetText(0)); // Text: BACK
            backButton.SetOnSelAction(0, MenuItemSelectAction.Back);

            return backButton;
        }
    }
}
