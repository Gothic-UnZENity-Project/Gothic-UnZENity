using System;

namespace GUZ.Core.UI.Menus
{
    public class SettingsMenu : AbstractMenu
    {
        protected override void Awake()
        {
            base.Awake();

            CreateRootElements("MENU_OPTIONS");
        }

        protected override void Undefined(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void Back(string itemName, string commandName)
        {
            MenuHandler.BackMenu();
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

        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return true;
        }
    }
}
