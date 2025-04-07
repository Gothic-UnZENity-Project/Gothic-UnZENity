using System.Collections.Generic;
using MyBox;
using UnityEngine;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class MenuManager : MonoBehaviour
    {
        private Dictionary<string, GameObject> menuList = new();
        private string currentMenu;
        private Stack<string> menuQueue = new();

        [SerializeField] private GameObject mainMenuPrefab;
        [SerializeField] private GameObject saveMenuPrefab;
        [SerializeField] private GameObject loadMenuPrefab;
        [SerializeField] private GameObject settingsMenuPrefab;
        [SerializeField] private GameObject leaveMenuPrefab;

        private void Awake()
        {
            OpenMenu("MENU_MAIN");
        }

        public void OpenMenu(string menuName)
        {
            if (!currentMenu.IsNullOrEmpty())
            {
                menuQueue.Push(currentMenu);
            }

            currentMenu = menuName;

            CloseAllMenus();
            if (menuList.ContainsKey(menuName))
            {
                menuList[menuName].SetActive(true);

                return;
            }

            GameObject menu = null;
            switch (menuName)
            {
                case "MENU_MAIN":
                    menu = Instantiate(mainMenuPrefab, this.transform);
                    break;
                case "MENU_SAVEGAME_LOAD":
                    menu = Instantiate(loadMenuPrefab, this.transform);
                    break;
                case "MENU_SAVEGAME_SAVE":
                    menu = Instantiate(saveMenuPrefab, this.transform);
                    break;
                case "MENU_OPTIONS":
                    // TODO: Refactor the current settings page to be used by the new approach
                    break;
                case "MENU_LEAVE_GAME":
                    menu = Instantiate(leaveMenuPrefab, this.transform);
                    break;
            }

            menuList.Add(menuName, menu);
        }

        public void CloseAllMenus()
        {
            foreach (var menu in menuList.Values)
            {
                menu.SetActive(false);
            }
        }

        public void BackMenu()
        {
            OpenMenu(menuQueue.Pop());
        }
    }
}
