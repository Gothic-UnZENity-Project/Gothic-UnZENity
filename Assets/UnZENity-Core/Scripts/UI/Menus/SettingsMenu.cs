using System;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
using GUZ.Core.UI.Menus.Adapter.MenuItem;
using UnityEngine.Rendering;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus
{
    public class SettingsMenu : AbstractMenu
    {
        protected override void Awake()
        {
            base.Awake();

            var menuInstance = new MenuInstanceAdapter("MENU_OPTIONS");

            menuInstance.GetMenuItemInstance("MENUITEM_OPT_HEADING").PosY -= GetSymbolInt("MENU_OPT_DY");
            
            // Data is heavily extracted from MENUITEM_OPT_GAME
            // We simply show the menu above the first element: GameSettings
            menuInstance.InsertItemAt(0, new MutableMenuItemInstance
            {
                Name = "VR Menu",
                Flags = MenuItemFlag.Centered,
                PosY = GetSymbolInt("MENU_OPT_START_Y") - GetSymbolInt("MENU_OPT_DY"),
                DimX = 8192,
                DimY = 750,
                Texts = new []{ "<<VR MENU>> (Translate)" }
            });
            
            CreateRootElements(menuInstance);
        }

        private string GetSymbolString(string symbolName)
        {
            return GameData.MenuVm.GetSymbolByName(symbolName).GetString(0);
        }

        private int GetSymbolInt(string symbolName)
        {
            return GameData.MenuVm.GetSymbolByName(symbolName).GetInt(0);
        }

        protected override void Undefined(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void StartMenu(string itemName, string commandName)
        {
            MenuHandler.OpenMenu(commandName);
        }

        protected override void StartItem(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void Close(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void ConsoleCommand(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void PlaySound(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void ExecuteCommand(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }
    }
}
