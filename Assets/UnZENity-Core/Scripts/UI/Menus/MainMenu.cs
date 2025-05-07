using System;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.Adapter.Menu;
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
                GameManager.I.LoadWorld(Constants.SelectedWorld, -1, SceneManager.GetActiveScene().name);
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
            return ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyInGame) == 0 &&
                    !GameData.InGameAndAlive) ||
                   ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyOutGame) == 0 &&
                    GameData.InGameAndAlive);
        }
    }
}
