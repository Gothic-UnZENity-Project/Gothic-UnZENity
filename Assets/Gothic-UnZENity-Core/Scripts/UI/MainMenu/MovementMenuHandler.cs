using GUZ.Core.Manager;
using TMPro;
using UnityEngine;

namespace GUZ.Core.UI.MainMenu
{
    public class MovementMenuHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _movementTypeDropdown;
        [SerializeField] private TMP_Dropdown _turnTypeDropdown;
        
        
        private void Start()
        {
            // Init field values.
            _movementTypeDropdown.value = PlayerPrefsManager.MovementType;
            _turnTypeDropdown.value = PlayerPrefsManager.TurnType;
        }

        public void OnMovementTypeSelected(int value)
        {
            PlayerPrefsManager.MovementType = value;
        }

        public void OnTurnTypeSelected(int value)
        {
            PlayerPrefsManager.TurnType = value;
        }
    }
}
