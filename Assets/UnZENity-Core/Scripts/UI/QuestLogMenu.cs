using GUZ.Core.Globals;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.UI
{
    public class QuestLogMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _background;

        private void Start()
        {
            LoadFromVM();
        }

        private void LoadFromVM()
        {
            var menuInstance = GameData.MenuVm.InitInstance<MenuInstance>("MENU_LOG");
            _background.GetComponent<MeshRenderer>().material = GameGlobals.Textures.GetMaterial(menuInstance.BackPic);
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
