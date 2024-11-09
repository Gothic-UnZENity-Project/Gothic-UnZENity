using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.Core.UI
{
    /// <summary>
    /// The quest log contains of three major areas:
    /// 1. Left area with clickable elements for e.g. ActiveMissions.
    /// 2. List area where the clickable topics for e.g. ActiveMissions are visible.
    /// 3. Once a list item is clicked on the right, the whole UI changes and shows the topic texts alone.
    ///
    /// |-----------------|----------|            |----------------------------|
    /// | Active Missions | Mission1 |            |  Detail description        |
    /// |-----------------| Mission2 | --click--> |  with all log              |
    /// |                 | ...      |            |  ------------------------  |
    /// |                 |          |            |  entries below each other  |
    /// |-----------------|----------|            |----------------------------|
    /// </summary>
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _canvas;
        // Menu entries (e.g. text:Current missions) are created dynamically. We therefore use this GO as reference (kind of Prefab).
        [SerializeField] private GameObject _background;

        // Create 22 buttons as list elements for all lists
        // TODO - Gothic handled it via FontSize. But for now, we can go with a fixed amount.
        private const int _visibleListItemAmount = 22;
        private const string _instanceNameActiveMissions = "MENU_ITEM_LIST_MISSIONS_ACT";
        private const string _instanceNameFailedMissions = "MENU_ITEM_LIST_MISSIONS_FAILED";
        private const string _instanceNameSuccessMissions = "MENU_ITEM_LIST_MISSIONS_OLD";
        private const string _instanceNameLog = "MENU_ITEM_LIST_LOG";
        // Easier to loop through or check .Contains() later.
        private string[] _listItemInstanceNames =
        {
            _instanceNameActiveMissions,
            _instanceNameFailedMissions,
            _instanceNameSuccessMissions,
            _instanceNameLog
        };

        // Sub-elements which need to be disabled initially.
        private static readonly string[] _initiallyDisabledMenuItems =
        {
            _instanceNameActiveMissions, _instanceNameFailedMissions, _instanceNameSuccessMissions,
            _instanceNameLog, "MENU_ITEM_CONTENT_VIEWER"
        };

        private Dictionary<string, (MenuItemInstance item, GameObject go)> _menuCache = new();
        private Dictionary<string, ListItemContainer> _listMenuCache = new();
        private ListItemContainer _activeListMenu;


        private class ListItemContainer
        {
            public string InstanceName;
            public MenuItemInstance Instance;
            public List<SaveLogTopic> LogTopics;
            public GameObject RootGo;
            public GameObject[] ItemGOs;
            public int CurrentListScrollValue; // When we use arrows to move up and down, we store where we are.
            public GameObject ArrowUpGo;
            public GameObject ArrowDownGo;
        }

        /// <summary>
        /// When opening the Log for the first time:
        /// 1. Load Daedalus information for menus (UI dimensions, labels, ...)
        /// 3. Use this data to create GameObjects dynamically based on Template elements (button, text, ...)
        /// 3. For lists (e.g. active missions), create the list items and arrows (do not them fill yet)
        /// </summary>
        private void Start()
        {
            Setup();
        }

        /// <summary>
        /// Each time we open the log:
        /// 1. Reset visibility for sub-menus (e.g. if FailedMissions was open last time, we disable it now)
        /// 2. Set current date time (left menu element)
        /// 3. Fill all the mission/note list elements with texts (e.g. all SuccessMissions will get their topic titles)
        /// </summary>
        public void ToggleVisibility()
        {
            // Toggle visibility
            gameObject.SetActive(!gameObject.activeSelf);

            if (gameObject.activeSelf)
            {
                ResetView();
                FillLists();
            }
        }

        private void Setup()
        {
            var menuInstance = GameData.MenuVm.InitInstance<MenuInstance>("MENU_LOG");

            var backPic = GameGlobals.Textures.GetMaterial(menuInstance.BackPic);
            _background.GetComponent<MeshRenderer>().material = backPic;

            // Set canvas size based on texture size of background
            var canvasRect = _canvas.GetComponent<RectTransform>();
            canvasRect.SetWidth(backPic.mainTexture.width);
            canvasRect.SetHeight(backPic.mainTexture.height);

            // Calculate pixelRatio for virtual positions of child elements.
            var virtualPixelX = menuInstance.DimX + 1;
            var virtualPixelY = menuInstance.DimY + 1;
            var realPixelX = backPic.mainTexture.width;
            var realPixelY = backPic.mainTexture.height;

            var pixelRatioX = (float)virtualPixelX / realPixelX; // for normal G1, should be 16 (=8192 / 512)
            var pixelRatioY = (float)virtualPixelY / realPixelY;

            for (var i = 0; ; i++)
            {
                var menuItemName = menuInstance.GetItem(i);

                // We passed the last item.
                if (menuItemName.IsNullOrEmpty())
                {
                    break;
                }

                CreateMenuItem(menuInstance, pixelRatioX, pixelRatioY, menuItemName);
            }

            CreateLists();
        }

        private void CreateMenuItem(MenuInstance main, float pixelRatioX, float pixelRatioY, string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            GameObject itemGo;

            if (item.MenuItemType == MenuItemType.ListBox)
            {
                itemGo = new GameObject(menuItemName);
                itemGo.SetParent(_canvas);
            }
            else if (item.Flags.HasFlag(MenuItemFlag.Selectable))
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: menuItemName, parent: _canvas)!;
                var button = itemGo.GetComponentInChildren<Button>();

                button.onClick.AddListener(() =>
                {
                    OnMenuItemClicked(item.GetOnSelAction(0), item.GetOnSelActionS(0));
                });
            }
            else
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: menuItemName, parent: _canvas)!;
            }

            _menuCache[menuItemName] = (item, itemGo);

            var rect = itemGo.GetComponent<RectTransform>();
            rect.SetLeft(item.PosX / pixelRatioX);
            rect.SetTop(item.PosY / pixelRatioY);

            if (item.DimX > 0)
            {
                rect.SetRight((main.DimX - item.PosX - item.DimX) / pixelRatioX);
            }

            if (item.DimY > 0)
            {
                rect.SetBottom((main.DimY - item.PosY - item.DimY) / pixelRatioY);
            }

            var textComp = itemGo.GetComponentInChildren<TMP_Text>();

            if (item.MenuItemType == MenuItemType.Text && item.Flags.HasFlag(MenuItemFlag.Centered))
            {
                textComp.alignment = TextAlignmentOptions.TopGeoAligned;
            }

            if (_initiallyDisabledMenuItems.Contains(menuItemName))
            {
                itemGo.SetActive(false);
            }
            else if (item.MenuItemType == MenuItemType.Text)
            {
                textComp.text = item.GetText(0);
            }
        }

        private void CreateLists()
        {
            foreach (var entryName in _listItemInstanceNames)
            {
                var listEntry = _menuCache[entryName];
                var container = new ListItemContainer()
                {
                    InstanceName = entryName,
                    Instance = listEntry.item,
                    RootGo = listEntry.go,
                    ItemGOs = new GameObject[_visibleListItemAmount]
                };

                // Create Item GameObjects (22x)
                for (var i = 0; i < _visibleListItemAmount; i++)
                {
                    var itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: $"{i}", parent: listEntry.go)!;
                    container.ItemGOs[i] = itemGo;
                }

                // Create arrow buttons
                {
                    // UP
                    {
                        var arrowUpGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: "ARROW_UP", parent: listEntry.go)!;
                        container.ArrowUpGo = arrowUpGo;

                        arrowUpGo.name = "ARROW_UP";
                        arrowUpGo.GetComponent<MeshRenderer>().material = GameGlobals.Textures.ArrowUpMaterial;

                        // FIXME - Set Position!
                        var arrowUpRect = arrowUpGo.GetComponentInChildren<RectTransform>();

                        var arrowUpButton = arrowUpGo.GetComponentInChildren<Button>();
                        arrowUpButton.onClick.AddListener(() =>
                        {
                            OnArrowUpClick();
                        });
                    }

                    // DOWN
                    {
                        var arrowDownGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: "ARROW_DOWN", parent: listEntry.go)!;
                        container.ArrowDownGo = arrowDownGo;

                        arrowDownGo.name = "ARROW_DOWN";
                        arrowDownGo.GetComponent<MeshRenderer>().material = GameGlobals.Textures.ArrowDownMaterial;

                        // FIXME - Set Position!
                        var arrowUpRect = arrowDownGo.GetComponentInChildren<RectTransform>();

                        var arrowDownButton = arrowDownGo.GetComponentInChildren<Button>();
                        arrowDownButton.onClick.AddListener(() =>
                        {
                            OnArrowDownClick();
                        });
                    }
                }

                _listMenuCache[entryName] = container;
            }
        }

        private void ResetView()
        {
            _listMenuCache.ForEach(i => i.Value.CurrentListScrollValue = 0);

            // Reset visibility for List menus
            foreach (var list in _listMenuCache.Values)
            {
                list.RootGo.SetActive(false);
                list.ArrowUpGo.SetActive(false);
                list.ArrowDownGo.SetActive(false);
            }
        }

        private void FillLists()
        {
            _listMenuCache[_instanceNameActiveMissions].LogTopics = GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Running);
            _listMenuCache[_instanceNameSuccessMissions].LogTopics = GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Success);
            _listMenuCache[_instanceNameFailedMissions].LogTopics = GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Failure);
            _listMenuCache[_instanceNameLog].LogTopics = GameGlobals.Story.GetLogTopics(SaveTopicSection.Notes, SaveTopicStatus.Free);

            foreach (var list in _listMenuCache.Values)
            {
                FillList(list);
            }
        }

        private void FillList(ListItemContainer list)
        {
            for (var i = 0; i < _visibleListItemAmount; i++)
            {
                var logItemOffset = i + list.CurrentListScrollValue;
                list.ItemGOs[i].GetComponentInChildren<TMP_Text>().text = list.LogTopics[logItemOffset].Description;

                // FIXME - Set OnClick(index)
            }

            list.ArrowUpGo.SetActive(list.CurrentListScrollValue > 0);
            list.ArrowDownGo.SetActive(list.LogTopics.Count > list.CurrentListScrollValue - _visibleListItemAmount);
        }

        public void OnMenuItemClicked(MenuItemSelectAction action, string commandName)
        {
            switch (action)
            {
                case MenuItemSelectAction.ExecuteCommand:
                    ExecuteCommand(commandName);
                    break;
                default:
                    Debug.LogError($"Unknown command {commandName}({action})");
                    break;
            }
        }

        private void OnArrowUpClick()
        {
            --_activeListMenu.CurrentListScrollValue;

            FillList(_activeListMenu);
        }

        private void OnArrowDownClick()
        {
            ++_activeListMenu.CurrentListScrollValue;

            FillList(_activeListMenu);
        }

        private void ExecuteCommand(string commandName)
        {
            var split = commandName.Split(' ');

            // Some commands are named like "EFFECTS MENU_ITEM_XYZ". We remove the unnecessary prefix (e.g. EFFECT).
            var menuItemName = split.Last();

            var itemCache = _menuCache[menuItemName];
            itemCache.go.SetActive(true);

            // If the Command==ListMenu, then we set it as currently active.
            _listMenuCache.TryGetValue(menuItemName, out _activeListMenu);
        }
    }
}
