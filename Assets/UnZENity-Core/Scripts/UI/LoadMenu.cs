using UnityEngine;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class LoadMenu : AbstractMenu
    {
        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            CreateRootElements("MENU_SAVEGAME_LOAD");
        }

        protected override void Undefined(string commandName)
        {
            Debug.Log($"Main Menu Undefined: {commandName}");
        }

        protected override void Back(string commandName)
        {
            _menuManager.BackMenu();
        }

        protected override void StartMenu(string commandName)
        {
            _menuManager.OpenMenu(commandName);
        }

        protected override void StartItem(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string commandName)
        {
            _menuManager.CloseAllMenus();
        }

        protected override void ConsoleCommand(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void PlaySound(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteCommand(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return true;
        }
    }
}
