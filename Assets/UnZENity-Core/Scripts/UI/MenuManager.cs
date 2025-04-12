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

        private void OnEnable()
        {
            InitializeMenus();
            
            OpenMenu("MENU_MAIN");
        }

        private void InitializeMenus()
        {
            if (menuList.Count != 0)
            {
                return;
            }

            GameObject menu = null;
            menu = Instantiate(mainMenuPrefab, transform);
            menuList.Add("MENU_MAIN", menu);
            menu = Instantiate(loadMenuPrefab, transform);
            menuList.Add("MENU_SAVEGAME_LOAD", menu);
            menu = Instantiate(saveMenuPrefab, transform);
            menuList.Add("MENU_SAVEGAME_SAVE", menu);
            // TODO: Refactor the current settings page to be used by the new approach
            menu = Instantiate(leaveMenuPrefab, transform);
            menuList.Add("MENU_LEAVE_GAME", menu);
            
            GameContext.InteractionAdapter.InitUIInteraction();
            
            CloseAllMenus();
        }

        public void ToggleVisibility()
        {
            // reset the queue
            if (gameObject.activeSelf)
            {
                CloseAllMenus();
                menuQueue.Clear();
                currentMenu = null;
            }

            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void OpenMenu(string menuName)
        {
            if (!currentMenu.IsNullOrEmpty())
            {
                menuQueue.Push(currentMenu);
            }

            currentMenu = menuName;

            CloseAllMenus();
            if (!menuList.ContainsKey(menuName))
            {
                return;
            }

            menuList[menuName].SetActive(true);
        }

        private void CloseAllMenus()
        {
            foreach (var menu in menuList.Values)
            {
                menu.SetActive(false);
            }
        }

        public void BackMenu()
        {
            var nextMenu = menuQueue.TryPop(out string result);
            if (nextMenu)
            {
                OpenMenu(result);
            }
            else
            {
                ToggleVisibility();
            }
        }
    }
}
