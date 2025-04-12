using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class newMainMenu : AbstractMenu
    {
        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            CreateRootElements("MENU_MAIN");
        }

        protected override void Undefined(string itemName, string commandName)
        {
            return;
        }

        protected override void Back(string itemName, string commandName)
        {
            _menuManager.BackMenu();
        }

        protected override void StartMenu(string itemName, string commandName)
        {
            _menuManager.OpenMenu(commandName);
        }

        protected override void StartItem(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string itemName, string commandName)
        {
            _menuManager.ToggleVisibility();
            if (commandName == "NEW_GAME")
            {
                GameManager.I.LoadWorld(Constants.SelectedWorld, -1, SceneManager.GetActiveScene().name);
            }
        }

        protected override void ConsoleCommand(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void PlaySound(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteCommand(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyInGame) == 0 &&
                    !GameData.InGameAndAlive) ||
                   ((MenuItemCache[menuItemName].item.Flags & MenuItemFlag.OnlyOutGame) == 0 &&
                    GameData.InGameAndAlive);
        }
    }
}
