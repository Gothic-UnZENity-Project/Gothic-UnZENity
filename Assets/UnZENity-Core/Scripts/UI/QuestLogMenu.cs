using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.UI
{
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _canvas;
        // Menu entries (e.g. text:Current missions) are created dynamically. We therefore use this GO as reference (kind of Prefab).
        [SerializeField] private GameObject _itemTemplate;
        [SerializeField] private GameObject _background;

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
            var virtualPixelX = menuInstance.DimX;
            var virtualPixelY = menuInstance.DimY;
            var realPixelX = backPic.mainTexture.width;
            var realPixelY = backPic.mainTexture.height;

            var pixelRatioX = virtualPixelX / realPixelX; // for normal G1, should be 16 (=8192 / 512)
            var pixelRatioY = virtualPixelY / realPixelY;

            for (var i = 0; ; i++)
            {
                var menuItemName = menuInstance.GetItem(i);

                // We passed the last item.
                if (menuItemName.IsNullOrEmpty())
                {
                    break;
                }

                LoadMenuItem(pixelRatioX, pixelRatioY, menuItemName);
                // break; // DEBUG
            }
        }

        private void LoadMenuItem(int pixelRatioX, int pixelRatioY, string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            var itemGo = Instantiate(_itemTemplate, _canvas.transform, false);
            itemGo.name = menuItemName;
            itemGo.SetActive(true);

            var rect = itemGo.GetComponent<RectTransform>();
            rect.SetLeft((float)item.PosX / pixelRatioX);
            rect.SetTop((float)item.PosY / pixelRatioY);
            // rect.SetWidth(item.DimX - item.PosX);
            // rect.SetHeight(item.DimY - item.PosY);

            // itemGo.GetComponent<RectTransform>().

            itemGo.GetComponentInChildren<TMP_Text>().text = item.GetText(0);
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
    }
}
