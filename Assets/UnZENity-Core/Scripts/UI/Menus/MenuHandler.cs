using System.Collections.Generic;
using GUZ.Core.UI.Menus.Adapter.Menu;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;

namespace GUZ.Core.UI.Menus
{
    public class MenuHandler : MonoBehaviour
    {
        private Dictionary<string, GameObject> _menuList = new();
        private string _currentMenu;
        private Stack<string> _menuQueue = new();

        [SerializeField] private GameObject _mainMenuPrefab;
        [SerializeField] private GameObject _saveMenuPrefab;
        [SerializeField] private GameObject _loadMenuPrefab;
        [SerializeField] private GameObject _leaveMenuPrefab;

        [SerializeField] private GameObject _settingsMenuPrefab;
        [SerializeField] private GameObject _settingsGameMenuPrefab;
        [SerializeField] private GameObject _settingsGraphicsMenuPrefab;
        [SerializeField] private GameObject _settingsVideoMenuPrefab;
        [SerializeField] private GameObject _settingsAudioMenuPrefab;
        [SerializeField] private GameObject _settingsControlsMenuPrefab;

        // Cached MainMenu tree hierarchy of submenus (load, save, settings) and their children (sub-settings, settings fields)
        public IMenuInstance MainMenuHierarchy { get; private set; }

        private void Awake()
        {
            InitializeMenus();
        }

        private void OnEnable()
        {
            OpenMenu("MENU_MAIN");
        }

        private void InitializeMenus()
        {
            // Initialize whole ZenKit Menu.dat hierarchy.
            MainMenuHierarchy = new MenuInstanceAdapter("MENU_MAIN");

            GameContext.InteractionAdapter.UpdateMainMenu(MainMenuHierarchy);
            
            InstantiateMenu("MENU_MAIN", _mainMenuPrefab);
            InstantiateMenu("MENU_SAVEGAME_LOAD", _loadMenuPrefab);
            InstantiateMenu("MENU_SAVEGAME_SAVE", _saveMenuPrefab);
            InstantiateMenu("MENU_LEAVE_GAME", _leaveMenuPrefab);

            InstantiateMenu("MENU_OPTIONS", _settingsMenuPrefab);
            InstantiateMenu("MENU_OPT_GAME", _settingsGameMenuPrefab);
            InstantiateMenu("MENU_OPT_GRAPHICS", _settingsGraphicsMenuPrefab);
            InstantiateMenu("MENU_OPT_VIDEO", _settingsVideoMenuPrefab);
            InstantiateMenu("MENU_OPT_AUDIO", _settingsAudioMenuPrefab);
            InstantiateMenu("MENU_OPT_CONTROLS", _settingsControlsMenuPrefab);

            GameContext.InteractionAdapter.InitUIInteraction();
            
            CloseAllMenus();
        }

        private void InstantiateMenu(string menuName, GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            go.GetComponent<AbstractMenu>().InitializeMenu(MainMenuHierarchy.FindSubMenu(menuName));
            
            _menuList.Add(menuName, go);
        }

        public void ToggleVisibility()
        {
            // reset the queue
            if (gameObject.activeSelf)
            {
                CloseAllMenus();
                _menuQueue.Clear();
                _currentMenu = null;
            }

            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void OpenMenu(string menuName, bool viaBackButton = false)
        {
            if (!viaBackButton && !_currentMenu.IsNullOrEmpty())
            {
                _menuQueue.Push(_currentMenu);
            }

            _currentMenu = menuName;

            CloseAllMenus();
            if (!_menuList.ContainsKey(menuName))
            {
                return;
            }

            _menuList[menuName].SetActive(true);
        }

        private void CloseAllMenus()
        {
            foreach (var menu in _menuList.Values)
            {
                menu.SetActive(false);
            }
        }

        public void BackMenu()
        {
            var nextMenu = _menuQueue.TryPop(out string result);
            if (nextMenu)
            {
                OpenMenu(result, true);
            }
            else
            {
                ToggleVisibility();
            }
        }
    }
}
