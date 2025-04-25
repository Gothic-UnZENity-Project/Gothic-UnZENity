using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.UI;
using GUZ.Core.UnZENity_Core.Scripts.UI;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GUZ.Core.Menu
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
            throw new System.NotImplementedException();
        }

        protected override void Back(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }
    }
}
