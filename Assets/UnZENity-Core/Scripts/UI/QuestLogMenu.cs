using System.Linq;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenKit.Daedalus;

namespace GUZ.Core.UI
{
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _canvas;
        // Menu entries (e.g. text:Current missions) are created dynamically. We therefore use this GO as reference (kind of Prefab).
        [SerializeField] private GameObject _textTemplate;
        [SerializeField] private GameObject _buttonTemplate;
        [SerializeField] private GameObject _background;

        // Sub-elements which need to be disabled initially.
        private string[] _initiallyDisabledMenuItems =
        {
            "MENU_ITEM_LIST_MISSIONS_ACT", "MENU_ITEM_LIST_MISSIONS_FAILED", "MENU_ITEM_LIST_MISSIONS_OLD",
            "MENU_ITEM_LIST_LOG", "MENU_ITEM_CONTENT_VIEWER"
        };


        private void Start()
        {
            LoadFromVM();
        }

        private void LoadFromVM()
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

                LoadMenuItem(menuInstance, pixelRatioX, pixelRatioY, menuItemName);
                // break; // DEBUG
            }
        }

        private void LoadMenuItem(MenuInstance main, float pixelRatioX, float pixelRatioY, string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            GameObject itemGo;

            if (item.Flags.HasFlag(MenuItemFlag.Selectable))
            {
                itemGo = Instantiate(_buttonTemplate, _canvas.transform, false);
                var button = itemGo.GetComponentInChildren<Button>();

                // FIXME - Won't register as of now.
                button.onClick.AddListener(() =>
                {
                    OnMenuItemClicked(item.GetOnSelAction(0), item.GetOnSelActionS(0));
                });
            }
            else
            {
                itemGo = Instantiate(_textTemplate, _canvas.transform, false);
            }

            itemGo.name = menuItemName;
            itemGo.SetActive(true);

            var rect = itemGo.GetComponent<RectTransform>();
            rect.SetLeft(item.PosX / pixelRatioX);
            rect.SetTop(item.PosY / pixelRatioY);

            if (item.DimX > 0)
            {
                rect.SetRight((main.DimX - item.PosX - item.DimX) / pixelRatioX);
            }

            var textComp = itemGo.GetComponentInChildren<TMP_Text>();
            textComp.text = item.GetText(0);

            if (item.Flags.HasFlag(MenuItemFlag.Centered))
            {
                textComp.alignment = TextAlignmentOptions.TopGeoAligned;
            }

            if (_initiallyDisabledMenuItems.Contains(menuItemName))
            {
                itemGo.SetActive(false);
            }
        }

        public void ToggleVisibility()
        {
            // Toggle visibility
            gameObject.SetActive(!gameObject.activeSelf);

            if (gameObject.activeSelf)
            {
                // TBD
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
            // TBD
        }
    }
}
