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
            _background.GetComponent<MeshRenderer>().material = GameGlobals.Textures.GetMaterial(menuInstance.BackPic);

            for (var i = 0; ; i++)
            {
                var menuItemName = menuInstance.GetItem(i);

                // We passed the last item.
                if (menuItemName.IsNullOrEmpty())
                {
                    break;
                }

                LoadMenuItem(menuItemName);
            }
        }

        private void LoadMenuItem(string menuItemName)
        {
            var item = GameData.MenuVm.InitInstance<MenuItemInstance>(menuItemName);

            var itemGo = Instantiate(_itemTemplate, _canvas.transform, false);
            itemGo.name = menuItemName;
            itemGo.SetActive(true);

            itemGo.transform.localPosition = new Vector2(item.PosX / 100, item.PosY / 100);
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
