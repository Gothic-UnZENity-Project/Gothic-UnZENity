using System;
using System.Linq;
using GUZ.Core.Globals;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;

namespace GUZ.Core.Adapters.UI.Menus
{
    public class MainMenu : AbstractMenu
    {
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
            MenuHandler.ToggleVisibility();
            if (commandName == "NEW_GAME")
            {
                GameManager.I.LoadWorld(GameGlobals.Config.GothicGame.World, 0, SceneManager.GetActiveScene().name);
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

        // FIXME - Saving and other elements aren't working yet. We therefore disable it for now.
        private string[] _ignoredMainMenuEntries =
        {
            "MENUITEM_MAIN_SAVEGAME_LOAD", "MENUITEM_MAIN_SAVEGAME_SAVE", "MENUITEM_MAIN_INTRO", "MENUITEM_MAIN_CREDITS"
        };
        
        protected override bool IsMenuItemActive(string menuItemName)
        {
            if (_ignoredMainMenuEntries.Contains(menuItemName))
                return false;
            
            return ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyInGame) == 0 &&
                    !GameData.InGameAndAlive) ||
                   ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyOutGame) == 0 &&
                    GameData.InGameAndAlive);
        }
    }
}
