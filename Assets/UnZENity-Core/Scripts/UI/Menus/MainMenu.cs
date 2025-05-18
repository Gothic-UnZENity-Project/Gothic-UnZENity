using System;
using GUZ.Core.Globals;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus
{
    public class MainMenu : AbstractMenu
    {
        protected override void Undefined(string itemName, string commandName)
        {
            return;
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
            MenuHandler.ToggleVisibility();
            if (commandName == "NEW_GAME")
            {
                GameManager.I.LoadWorld(Constants.SelectedWorld, 0, SceneManager.GetActiveScene().name);
            }
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

        protected override bool IsMenuItemActive(string menuItemName)
        {
            // FIXME - Saving is not yet working in UnZENity. We therefore disable it for now.
            if (menuItemName == "MENUITEM_MAIN_SAVEGAME_SAVE")
                return false;
            
            return ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyInGame) == 0 &&
                    !GameData.InGameAndAlive) ||
                   ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyOutGame) == 0 &&
                    GameData.InGameAndAlive);
        }
    }
}
