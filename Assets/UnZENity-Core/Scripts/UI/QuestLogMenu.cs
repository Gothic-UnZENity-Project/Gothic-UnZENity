using System;
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
    ///
    ///
    /// Hint: We need to stick with center for UI elements as setting anchor positions (min=v2(0.5,0.5), max=v2(0.5,0.5))
    ///       Because setting the anchors at runtime (e.g. left-aligned) causes Unity to crash at a certain amount of changes.
    ///       Unfortunately this causes a lot of calculations within this class. But now you know at least. :-)
    /// </summary>
    public class QuestLogMenu : MonoBehaviour
    {
        // Menu entries (e.g. text:Current missions) are created dynamically. We therefore use this GO as reference (kind of Prefab).
        [SerializeField] private GameObject _canvas;
        [SerializeField] private GameObject _background;

        // Create 22 buttons as list elements for all lists
        // TODO - Gothic handled it via FontSize. But for now, we can go with a fixed amount.
        private const int _visibleListItemAmount = 22;
        private const int _listItemHeight = 17;
        private const int _margin = 12;

        private const string _instanceNameActiveMissions = "MENU_ITEM_LIST_MISSIONS_ACT";
        private const string _instanceNameFailedMissions = "MENU_ITEM_LIST_MISSIONS_FAILED";
        private const string _instanceNameSuccessMissions = "MENU_ITEM_LIST_MISSIONS_OLD";
        private const string _instanceNameLog = "MENU_ITEM_LIST_LOG";
        private const string _instanceContentViewer = "MENU_ITEM_CONTENT_VIEWER";

        // It's easier to loop through or check .Contains() later.
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
            _instanceNameLog, _instanceContentViewer
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
        private void Awake()
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
            _background.GetComponentInChildren<MeshRenderer>().sharedMaterial = backPic;

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

            CreateLists(pixelRatioX, pixelRatioY);
        }

        private void CreateMenuItem(MenuInstance main, float pixelRatioX, float pixelRatioY, string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            GameObject itemGo;

            if (item.MenuItemType == MenuItemType.ListBox)
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: menuItemName, parent: _canvas)!;
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
            // MENU_ITEM_CONTENT_VIEWER
            else if (menuItemName.EqualsIgnoreCase(_instanceContentViewer))
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiEmpty, name: menuItemName, parent: _canvas)!;

                var backPicGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiTexture, name: item.BackPic, parent: itemGo)!;
                var backPic = GameGlobals.Textures.GetMaterial(item.BackPic);
                var backPictRenderer = backPicGo.GetComponentInChildren<MeshRenderer>();
                backPictRenderer.sharedMaterial = backPic;
                backPictRenderer.transform.localScale = _background.GetComponentInChildren<MeshRenderer>().transform.localScale;
                backPictRenderer.transform.localPosition = _background.GetComponentInChildren<MeshRenderer>().transform.localPosition;

                var textGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: "Content", parent: itemGo)!;
                var textGoRect = textGo.GetComponentInChildren<RectTransform>();
                textGoRect.SetWidth(item.DimX / pixelRatioX);
                textGoRect.SetHeight(item.DimY / pixelRatioY);
            }
            else
            {
                itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiText, name: menuItemName, parent: _canvas)!;
            }

            _menuCache[menuItemName] = (item, itemGo);

            var rect = itemGo.GetComponent<RectTransform>();
            var halfMainWidth = (float)main.DimX / 2;
            var halfMainHeight = (float)main.DimY / 2;

            float itemWidth;
            if (item.DimX > 0)
            {
                // As we have anchor positions at the center (0.5), we need to move into a certain direction from the center
                // Hint: We need to stick with center, as setting anchor positions at runtime (e.g. left-aligned) causes Unity to crash at a certain amount of changes.
                itemWidth = item.DimX;
            }
            else
            {
                // We assume the element can be drawn until end of whole UI.
                itemWidth = ((float)main.DimX - item.PosX);
            }
            rect.SetPositionX((item.PosX - halfMainWidth + itemWidth / 2) / pixelRatioX);
            rect.SetWidth(itemWidth / pixelRatioX);

            float itemHeight;
            if (item.DimY > 0)
            {
                itemHeight = item.DimY;
            }
            else
            {
                // We assume the element can be drawn until end of whole UI.
                itemHeight = (float)main.DimY - item.PosY;
            }
            rect.SetPositionY((halfMainHeight - item.PosY - itemHeight / 2) / pixelRatioY);
            rect.SetHeight(itemHeight / pixelRatioY);

            if (_initiallyDisabledMenuItems.Contains(menuItemName))
            {
                itemGo.SetActive(false);
            }
            else if (item.MenuItemType == MenuItemType.Text)
            {
                var textComp = itemGo.GetComponentInChildren<TMP_Text>();

                if (item.Flags.HasFlag(MenuItemFlag.Centered))
                {
                    textComp.alignment = TextAlignmentOptions.TopGeoAligned;
                }

                textComp.text = item.GetText(0);
                textComp.spriteAsset = GameGlobals.Font.TryGetFont(item.FontName);

                // Text component needs to align in dimensions with parent rect.
                var textRect = textComp.GetComponent<RectTransform>();
                textRect.SetWidth(itemWidth / pixelRatioX);
                textRect.SetHeight(itemHeight / pixelRatioY);
            }
        }

        private void CreateLists(float pixelRatioX, float pixelRatioY)
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

                var halfMainWidth = (float)listEntry.item.DimX / pixelRatioX / 2;
                var halfMainHeight = (float)listEntry.item.DimY / pixelRatioY / 2;

                // Create Item GameObjects (22x)
                for (var i = 0; i < _visibleListItemAmount; i++)
                {
                    var itemGo = ResourceLoader.TryGetPrefabObject(PrefabType.UiButton, name: $"{i}", parent: listEntry.go)!;
                    container.ItemGOs[i] = itemGo;

                    var rect = itemGo.GetComponentInChildren<RectTransform>();
                    var halfItemHeight = (float)_listItemHeight / 2;
                    rect.SetPositionY((halfMainHeight - (i * _listItemHeight) - halfItemHeight));
                    rect.SetHeight(_listItemHeight);
                    rect.SetWidth(listEntry.item.DimX / pixelRatioX);

                    var textRect = itemGo.GetComponentInChildren<TMP_Text>().GetComponent<RectTransform>();
                    textRect.SetWidth((listEntry.item.DimX / pixelRatioX) - _margin);
                    textRect.SetHeight(_listItemHeight);

                    var button = itemGo.GetComponentInChildren<Button>();

                    var clickIndex = i; // Fixing "Closing over the loop variable" feature. ;-)
                    button.onClick.AddListener(() =>
                    {
                        OnListItemClicked(clickIndex);
                    });
                }

                // Create arrow buttons
                {
                    // UP
                    {
                        var go = ResourceLoader.TryGetPrefabObject(PrefabType.UiButtonTextured, name: "ARROW_UP", parent: listEntry.go)!;
                        var rect = go.GetComponentInChildren<RectTransform>();
                        var rend = go.GetComponentInChildren<MeshRenderer>();
                        var button = go.GetComponentInChildren<Button>();

                        go.transform.localScale = new Vector3(2, 2, 1);
                        container.ArrowUpGo = go;
                        rend.sharedMaterial = GameGlobals.Textures.ArrowUpMaterial;
                        button.onClick.AddListener(OnArrowUpClick);

                        rect.SetHeight(rend.sharedMaterial.mainTexture.height);
                        rect.SetWidth(rend.sharedMaterial.mainTexture.width);
                        rect.SetPositionX(halfMainWidth - _margin);
                        rect.SetPositionY(halfMainHeight - _margin);
                    }

                    // DOWN
                    {
                        var go = ResourceLoader.TryGetPrefabObject(PrefabType.UiButtonTextured, name: "ARROW_DOWN", parent: listEntry.go)!;
                        var rect = go.GetComponentInChildren<RectTransform>();
                        var rend = go.GetComponentInChildren<MeshRenderer>();
                        var button = go.GetComponentInChildren<Button>();

                        go.transform.localScale = new Vector3(2, 2, 1);
                        container.ArrowDownGo = go;
                        rend.sharedMaterial = GameGlobals.Textures.ArrowDownMaterial;
                        button.onClick.AddListener(OnArrowDownClick);

                        rect.SetHeight(rend.sharedMaterial.mainTexture.height);
                        rect.SetWidth(rend.sharedMaterial.mainTexture.width);
                        rect.SetPositionX(halfMainWidth - _margin);
                        rect.SetPositionY(-halfMainHeight + _margin);
                    }
                }

                _listMenuCache[entryName] = container;
            }
        }

        private void ResetView()
        {
            foreach (var list in _listMenuCache.Values)
            {
                list.CurrentListScrollValue = 0;
                list.ItemGOs.ForEach(i => i.SetActive(false));
            }

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
            for (var i = 0; i < Math.Min(_visibleListItemAmount, list.LogTopics.Count); i++)
            {
                var logItemOffset = i + list.CurrentListScrollValue;
                list.ItemGOs[i].SetActive(true);
                list.ItemGOs[i].GetComponentInChildren<TMP_Text>().text = list.LogTopics[logItemOffset].Description;
            }

            list.ArrowUpGo.SetActive(list.CurrentListScrollValue > 0);
            list.ArrowDownGo.SetActive(list.LogTopics.Count - _visibleListItemAmount - list.CurrentListScrollValue > 0);
        }

        private void OnMenuItemClicked(MenuItemSelectAction action, string commandName)
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

        private void OnListItemClicked(int index)
        {
            Debug.Log($"OnListItemClicked({index})");

            var realIndex = index + _activeListMenu.CurrentListScrollValue;
            var text = _activeListMenu.LogTopics[realIndex].Entries.Aggregate((i, j) => i + "\n---\n" + j);

            // Disable everything
            _menuCache.ForEach(i => i.Value.go.SetActive(false));
            // including background
            _background.SetActive(false);

            var contentViewer = _menuCache[_instanceContentViewer].go;
            contentViewer.GetComponentInChildren<TMP_Text>().text = text;
            contentViewer.SetActive(true);


            // FIXME
            // Initially:
            // [x]Handle content viewer separately
            // [x]Set backPic
            // [ ]Create scroll arrows
            // Now:
            // [ ]Set Scrollposition of contentViewer=0 + disable arrowUp
            // [x]Disable all elements
            // [x]Enable ContentViewer
            // [ ]Check if arrowDown needs to be active
            // Then:
            // [ ]Add Arrow-back (e.g. arrow down rotated)
            // [ ]Once clicked, setActive() previously active list and all left menu items
            //
        }

        private void OnArrowUpClick()
        {
            --_activeListMenu.CurrentListScrollValue;

            FillList(_activeListMenu);
        }

        public void OnArrowDownClick()
        {
            Debug.Log($"OnArrowDownClick()");
            ++_activeListMenu.CurrentListScrollValue;

            FillList(_activeListMenu);
        }

        private void ExecuteCommand(string commandName)
        {
            ResetView();
            FillLists();

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
