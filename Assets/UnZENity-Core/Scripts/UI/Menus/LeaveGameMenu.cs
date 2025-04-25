using UnityEngine;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class LeaveGameMenu : AbstractMenu
    {
        protected override void Awake()
        {
            base.Awake();
            Setup();
        }

        private void Setup()
        {
            CreateRootElements("MENU_LEAVE_GAME");
        }

        protected override void Undefined(string itemName, string commandName)
        {
            return;
        }

        protected override void Back(string itemName, string commandName)
        {
            MenuHandler.BackMenu();
        }

        protected override void StartMenu(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void StartItem(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string itemName, string commandName)
        {
            Application.Quit();
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
            return true;
        }
    }
}
