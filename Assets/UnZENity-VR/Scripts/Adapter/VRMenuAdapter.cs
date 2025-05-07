#if GUZ_HVR_INSTALLED
using GUZ.Core.Adapter;
using GUZ.Core.UI.Menus.Adapter.Menu;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using GUZ.VR.Manager;
using HurricaneVR.Framework.Core.Player;
using ZenKit.Daedalus;
using Constants = GUZ.Core.Globals.Constants;

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
            var vrAccessibilityMenu = CreateAccessibilityMenu(mainMenu);

            // Will be used to clone render settings from its items.
            var gameMenu = mainMenu.FindMenuRecursive("MENU_OPT_GAME")!;

            var menuStartY = Constants.DaedalusMenu.MenuStartY;
            var menuDY = Constants.DaedalusMenu.MenuDY;
            
            vrAccessibilityMenu.Items.Add(CreateAccessibilityHeadline(gameMenu));

            vrAccessibilityMenu.Items.Add(CreateSitStandLabel(gameMenu, menuStartY + menuDY * 0));
            vrAccessibilityMenu.Items.Add(CreateSitStandChoicebox(gameMenu, menuStartY + menuDY * 0));
            vrAccessibilityMenu.Items.Add(CreateMoveDirectionLabel(gameMenu, menuStartY + menuDY * 1));
            vrAccessibilityMenu.Items.Add(CreateMoveDirectionChoicebox(gameMenu, menuStartY + menuDY * 1));
            vrAccessibilityMenu.Items.Add(CreateRotationTypeLabel(gameMenu, menuStartY + menuDY * 2));
            vrAccessibilityMenu.Items.Add(CreateRotationTypeChoicebox(gameMenu, menuStartY + menuDY * 2));
            vrAccessibilityMenu.Items.Add(CreateSmoothRotationSpeedLabel(gameMenu, menuStartY + menuDY * 3));
            vrAccessibilityMenu.Items.Add(CreateSmoothRotationSpeedSlider(gameMenu, menuStartY + menuDY * 3));
            vrAccessibilityMenu.Items.Add(CreateSnapRotationLabel(gameMenu, menuStartY + menuDY * 4));
            vrAccessibilityMenu.Items.Add(CreateSnapRotationChoicebox(gameMenu, menuStartY + menuDY * 4));
            vrAccessibilityMenu.Items.Add(CreateSmoothSpectatorLabel(gameMenu, menuStartY + menuDY * 5));
            vrAccessibilityMenu.Items.Add(CreateSmoothSpectatorChoicebox(gameMenu, menuStartY + menuDY * 5));

            vrAccessibilityMenu.Items.Add(CreateBackButton(mainMenu));


            // TODO
            // var vrImmersionMenu = "";
            // FIXME - Add - Setting: ItemCollisionWhileDragged
        }

        private MutableMenuInstance CreateAccessibilityMenu(AbstractMenuInstance mainMenu)
        {
            // Find OPT_CONTROLS menu item to overwrite
            var controlsMenuParent = mainMenu.FindMenuRecursive("MENU_OPT_CONTROLS")!.Parent;
            var controlsMenuItem = controlsMenuParent.FindMenuItem("MENUITEM_OPT_CONTROLS", out var controlsItemIndex);
            
            // Create empty menu
            var vrControlsMenu = new MutableMenuInstance("MENU_UNZENITY_OPT_VR_ACCESSIBILITY", controlsMenuParent);
            
            // Create menu item and replace it where >Keyboard< settings are normally
            var vrControlsMenuItem = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY", controlsMenuItem);
            controlsMenuParent.ReplaceItemAt(controlsItemIndex, vrControlsMenuItem);

            // Add some setting
            vrControlsMenuItem.SetText(0, VRMenuLocalization.GetText("menuitem.vr_accessibility"));
            vrControlsMenuItem.SetOnSelAction(0, MenuItemSelectAction.StartMenu);
            vrControlsMenuItem.SetOnSelActionS(0, "MENU_UNZENITY_OPT_VR_ACCESSIBILITY");
            vrControlsMenuItem.MenuInstance = vrControlsMenu;

            return vrControlsMenu;
        }

        private MutableMenuItemInstance CreateAccessibilityHeadline(AbstractMenuInstance gameMenu)
        {
            var gameHeadline = gameMenu.FindMenuItem("MENUITEM_GAME_HEADLINE", out _);
            var vrHeadline = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_HEADLINE", gameHeadline);
            vrHeadline.SetText(0, VRMenuLocalization.GetText("menuitem.vr_accessibility.headline"));
            
            return vrHeadline;
        }
        
        private AbstractMenuItemInstance CreateSitStandChoicebox(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var sitStandLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SIT_STAND", subtitlesLabel);
            
            sitStandLabel.PosY = posY;
            
            sitStandLabel.SetText(0, VRMenuLocalization.GetText("menuitem.sitStand.label"));
            sitStandLabel.SetText(1, VRMenuLocalization.GetText("menuitem.sitStand.description"));
            
            return sitStandLabel;
        }

        private AbstractMenuItemInstance CreateSitStandLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var sitStandSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SIT_STAND_CHOICE", subtitlesChoice);

            sitStandSetting.PosY = posY;
            sitStandSetting.SetUserFloat(3, (int)HVRSitStand.PlayerHeight); // Default value if no INI value exists.

            sitStandSetting.SetText(0, VRMenuLocalization.GetText("menuitem.sitStand.value"));
            sitStandSetting.OnChgSetOption = VRConstants.IniNames.SitStand;
            sitStandSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return sitStandSetting;
        }
        
        private AbstractMenuItemInstance CreateMoveDirectionLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var moveDirectionLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_MOVE_DIRECTION", subtitlesLabel);
            
            moveDirectionLabel.PosY = posY;
            
            moveDirectionLabel.SetText(0, VRMenuLocalization.GetText("menuitem.moveDirection.label"));
            moveDirectionLabel.SetText(1, VRMenuLocalization.GetText("menuitem.moveDirection.description"));
            
            return moveDirectionLabel;
        }
        
        private AbstractMenuItemInstance CreateMoveDirectionChoicebox(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var moveDirectionSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_MOVE_DIRECTION_CHOICE", subtitlesChoice);

            moveDirectionSetting.PosY = posY;

            moveDirectionSetting.SetText(0, VRMenuLocalization.GetText("menuitem.moveDirection.value"));
            moveDirectionSetting.OnChgSetOption = VRConstants.IniNames.MoveDirection;
            moveDirectionSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return moveDirectionSetting;
        }
        
        private AbstractMenuItemInstance CreateRotationTypeLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var rotationTypeLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_ROTATION_TYPE", subtitlesLabel);
            
            rotationTypeLabel.PosY = posY;
            
            rotationTypeLabel.SetText(0, VRMenuLocalization.GetText("menuitem.rotationType.label"));
            rotationTypeLabel.SetText(1, VRMenuLocalization.GetText("menuitem.rotationType.description"));
            
            return rotationTypeLabel;
        }

        private AbstractMenuItemInstance CreateRotationTypeChoicebox(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var rotationTypeSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_ROTATION_TYPE_CHOICE", subtitlesChoice);

            rotationTypeSetting.PosY = posY;
            rotationTypeSetting.SetUserFloat(3, (int)RotationType.Snap); // Default value if no INI value exists.

            rotationTypeSetting.SetText(0, VRMenuLocalization.GetText("menuitem.rotationType.value"));
            rotationTypeSetting.OnChgSetOption = VRConstants.IniNames.RotationType;
            rotationTypeSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return rotationTypeSetting;
        }

        private AbstractMenuItemInstance CreateSmoothRotationSpeedLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var smoothRotationLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SMOOTH_ROTATION", subtitlesLabel);
            
            smoothRotationLabel.PosY = posY;
            
            smoothRotationLabel.SetText(0, VRMenuLocalization.GetText("menuitem.smoothRotation.label"));
            smoothRotationLabel.SetText(1, VRMenuLocalization.GetText("menuitem.smoothRotation.description"));
            
            return smoothRotationLabel;
        }
        
        private AbstractMenuItemInstance CreateSmoothRotationSpeedSlider(AbstractMenuInstance gameMenu, int posY)
        {
            var mouseSlider = gameMenu.FindMenuItem("MENUITEM_MSENSITIVITY_SLIDER", out _);
            var smoothRotationSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SMOOTH_ROTATION_SLIDER", mouseSlider);

            smoothRotationSetting.PosY = posY;
            smoothRotationSetting.SetUserFloat(3, VRConstants.SmoothRotationDefaultValue); // Default value if no INI value exists.

            smoothRotationSetting.SetUserFloat(0, VRConstants.SmoothRotationSettingAmount);
            smoothRotationSetting.OnChgSetOption = VRConstants.IniNames.SmoothRotationSpeed;
            smoothRotationSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return smoothRotationSetting;
        }
        
        private AbstractMenuItemInstance CreateSnapRotationLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var smoothRotationLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SNAP_ROTATION", subtitlesLabel);
            
            smoothRotationLabel.PosY = posY;
            
            smoothRotationLabel.SetText(0, VRMenuLocalization.GetText("menuitem.snapRotation.label"));
            smoothRotationLabel.SetText(1, VRMenuLocalization.GetText("menuitem.snapRotation.description"));
            
            return smoothRotationLabel;
        }
        
        private AbstractMenuItemInstance CreateSnapRotationChoicebox(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var snapRotationSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SNAP_ROTATION_CHOICE", subtitlesChoice);

            snapRotationSetting.PosY = posY;
            snapRotationSetting.SetUserFloat(3, VRConstants.SnapRotationDefaultValue); // Default value if no INI value exists.

            snapRotationSetting.SetText(0, VRMenuLocalization.GetText("menuitem.snapRotation.value"));
            snapRotationSetting.OnChgSetOption = VRConstants.IniNames.SnapRotationAmount;
            snapRotationSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return snapRotationSetting;
        }

        private MutableMenuItemInstance CreateSmoothSpectatorLabel(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesLabel = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES", out _);
            var smoothingLabel= new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SMOOTHSPECTATOR", subtitlesLabel);
            
            smoothingLabel.PosY = posY;
            
            smoothingLabel.SetText(0, VRMenuLocalization.GetText("menuitem.smooth.label"));
            smoothingLabel.SetText(1, VRMenuLocalization.GetText("menuitem.smooth.description"));
            
            return smoothingLabel;
        }
        
        private MutableMenuItemInstance CreateSmoothSpectatorChoicebox(AbstractMenuInstance gameMenu, int posY)
        {
            var subtitlesChoice = gameMenu.FindMenuItem("MENUITEM_GAME_SUB_TITLES_CHOICE", out _);
            var smoothingSetting = new MutableMenuItemInstance("MENUITEM_UNZENITY_OPT_VR_ACCESSIBILITY_SMOOTHSPECTATOR_CHOICE", subtitlesChoice);

            smoothingSetting.PosY = posY;

            smoothingSetting.SetText(0, VRMenuLocalization.GetText("menuitem.smooth.value"));
            smoothingSetting.OnChgSetOption = VRConstants.IniNames.SmoothSpectator;
            smoothingSetting.OnChgSetOptionSection = VRConstants.IniSectionAccessibility;
            
            return smoothingSetting;
        }

        private MutableMenuItemInstance CreateBackButton(AbstractMenuInstance mainMenu)
        {
            var someOptionsMenu = mainMenu.FindMenuRecursive("MENU_OPT_GRAPHICS")!;
            var backButtonReference = someOptionsMenu.FindMenuItem("MENUITEM_GRA_BACK", out _)!;
            var backButton = new MutableMenuItemInstance("MENU_UNZENITY_OPT_VR_ACCESSIBILITY_BACK", backButtonReference);
            
            backButton.SetText(0, backButtonReference.GetText(0)); // Text: BACK
            backButton.SetOnSelAction(0, MenuItemSelectAction.Back);

            return backButton;
        }
    }
}
#endif
