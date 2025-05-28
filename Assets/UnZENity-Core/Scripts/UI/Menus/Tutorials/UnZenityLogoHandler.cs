using UnityEngine;

namespace GUZ.Core.UI.Menus
{
    public class UnZenityLogoHandler : MonoBehaviour
    {
        [SerializeField]
        private GameObject _tutorialHandler;

        public void OnClick()
        {
            if (_tutorialHandler == null)
                return;
            
            _tutorialHandler.gameObject.SetActive(!_tutorialHandler.gameObject.activeSelf);
        }
    }
}
