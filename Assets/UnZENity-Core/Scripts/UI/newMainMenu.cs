using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        protected override void Undefined(string commandName)
        {
            Debug.Log($"Main Menu Undefined: {commandName}");
        }

        protected override void Back(string commandName)
        {
            Debug.Log($"Main Menu Back: {commandName}");
            _menuManager.BackMenu();
        }

        protected override void StartMenu(string commandName)
        {
            Debug.Log($"Main Menu Start: {commandName}");
            _menuManager.OpenMenu(commandName);
        }

        protected override void StartItem(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string commandName)
        {
            Debug.Log($"Main Menu Close: {commandName}");
            _menuManager.CloseAllMenus();
            if (commandName == "NEW_GAME")
            {
                GameManager.I.LoadWorld(Constants.SelectedWorld, -1, SceneManager.GetActiveScene().name);
            }
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
