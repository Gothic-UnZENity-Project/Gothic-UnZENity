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
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _canvas;
        // Menu entries (e.g. text:Current missions) are created dynamically. We therefore use this GO as reference (kind of Prefab).
        [SerializeField] private GameObject _textTemplate;
        [SerializeField] private GameObject _buttonTemplate;
        [SerializeField] private GameObject _listMenuTemplate;
        [SerializeField] private GameObject _background;

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
        private Dictionary<SaveTopicStatus, List<SaveLogTopic>> _logTopicsCache = new();


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
                itemGo = Instantiate(_listMenuTemplate, _canvas.transform, false);
            }
            else if (item.Flags.HasFlag(MenuItemFlag.Selectable))
            {
                itemGo = Instantiate(_buttonTemplate, _canvas.transform, false);
                var button = itemGo.GetComponentInChildren<Button>();

                button.onClick.AddListener(() =>
                {
                    OnMenuItemClicked(item.GetOnSelAction(0), item.GetOnSelActionS(0));
                });
            }
            else
            {
                itemGo = Instantiate(_textTemplate, _canvas.transform, false);
            }

            _menuCache[menuItemName] = (item, itemGo);

            itemGo.name = menuItemName;
            itemGo.SetActive(true);

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
            // Create 22 buttons as list elements for all lists
            // TODO - Gothic handled it via FontSize. For now, we can go with a fixed amount.
            {
                foreach (var entryName in _listItemInstanceNames)
                {
                    var listEntry = _menuCache[entryName];
                    for (var i = 0; i < _visibleListItemAmount; i++)
                    {
                        var itemGo = Instantiate(_buttonTemplate, listEntry.go.transform, false);
                        itemGo.name = $"{i}";
                    }
                }
            }
        }

        private void ResetView()
        {
            // Reset visibility for List menus
            foreach (var entryName in _listItemInstanceNames)
            {
                var listEntry = _menuCache[entryName];
                listEntry.go.SetActive(false);
            }
        }

        private void FillLists()
        {
            _logTopicsCache.Add(SaveTopicStatus.Running, GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Running));
            _logTopicsCache.Add(SaveTopicStatus.Success, GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Success));
            _logTopicsCache.Add(SaveTopicStatus.Failure, GameGlobals.Story.GetLogTopics(SaveTopicSection.Missions, SaveTopicStatus.Failure));
            _logTopicsCache.Add(SaveTopicStatus.Free, GameGlobals.Story.GetLogTopics(SaveTopicSection.Notes, SaveTopicStatus.Free));

            foreach (var entryName in _listItemInstanceNames)
            {
                var listEntry = _menuCache[entryName];

            }
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

        private void ExecuteCommand(string commandName)
        {
            var split = commandName.Split(' ');

            // Some commands are named like "EFFECTS MENU_ITEM_XYZ". We remove the unnecessary prefix (e.g. EFFECT).
            var command = split.Last();

            var itemCache = _menuCache[command];
            itemCache.go.SetActive(true);
        }
    }
}
