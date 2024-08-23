using GUZ.Core.Globals;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;

namespace GUZ.Core.UI.MainMenu
{
    public class GameplayMenuHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text _dragCollisionText;

        
        private void Start()
        {
            // Init field values.
            _dragCollisionText.text =
                PlayerPrefsManager.ItemCollisionWhileDragged ? Constants.YesLabel : Constants.NoLabel;
        }

        public void OnDragCollisionClicked()
        {
            PlayerPrefsManager.ItemCollisionWhileDragged = !PlayerPrefsManager.ItemCollisionWhileDragged;
            
            _dragCollisionText.text = PlayerPrefsManager.ItemCollisionWhileDragged ? Constants.YesLabel : Constants.NoLabel;
        }
    }
}
