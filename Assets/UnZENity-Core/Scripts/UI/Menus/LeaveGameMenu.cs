using System;
using UnityEngine;

namespace GUZ.Core.UI.Menus
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

        protected override void StartMenu(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void StartItem(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void Close(string itemName, string commandName)
        {
            Application.Quit();
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
