using UnityEngine;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class LeaveGameMenu : AbstractMenu
    {
        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            CreateRootElements("MENU_LEAVE_GAME");
        }

        protected override void Undefined(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Back(string commandName)
        {
            _menuManager.BackMenu();
        }

        protected override void StartMenu(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void StartItem(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string commandName)
        {
            Application.Quit();
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
