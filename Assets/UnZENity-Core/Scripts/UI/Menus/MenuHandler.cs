using System.Collections.Generic;
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

        private void OnEnable()
        {
            InitializeMenus();
            
            OpenMenu("MENU_MAIN");
        }

        private void InitializeMenus()
        {
            if (_menuList.Count != 0)
            {
                return;
            }

            _menuList.Add("MENU_MAIN", Instantiate(_mainMenuPrefab, transform));
            _menuList.Add("MENU_SAVEGAME_LOAD", Instantiate(_loadMenuPrefab, transform));
            _menuList.Add("MENU_SAVEGAME_SAVE", Instantiate(_saveMenuPrefab, transform));
            _menuList.Add("MENU_LEAVE_GAME", Instantiate(_leaveMenuPrefab, transform));

            _menuList.Add("MENU_OPTIONS", Instantiate(_settingsMenuPrefab, transform));
            _menuList.Add("MENU_OPT_GAME", Instantiate(_settingsGameMenuPrefab, transform));
            _menuList.Add("MENU_OPT_GRAPHICS", Instantiate(_settingsGraphicsMenuPrefab, transform));
            _menuList.Add("MENU_OPT_VIDEO", Instantiate(_settingsVideoMenuPrefab, transform));
            _menuList.Add("MENU_OPT_AUDIO", Instantiate(_settingsAudioMenuPrefab, transform));
            _menuList.Add("MENU_OPT_CONTROLS", Instantiate(_settingsControlsMenuPrefab, transform));

            GameContext.InteractionAdapter.InitUIInteraction();
            
            CloseAllMenus();
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

        public void OpenMenu(string menuName)
        {
            if (!_currentMenu.IsNullOrEmpty())
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
                OpenMenu(result);
            }
            else
            {
                ToggleVisibility();
            }
        }
    }
}
