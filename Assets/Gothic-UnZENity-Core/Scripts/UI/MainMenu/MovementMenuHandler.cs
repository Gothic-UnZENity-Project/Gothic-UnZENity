using GUZ.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.Core.UI.MainMenu
{
    public class MovementMenuHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _movementTypeDropdown;
        [SerializeField] private TMP_Dropdown _turnTypeDropdown;
        [SerializeField] private Slider _snapTurnAngleSlider;
        [SerializeField] private Slider _smoothTurnSpeedSlider;
        
        
        private void Start()
        {
            // Init field values.
            _movementTypeDropdown.value = PlayerPrefsManager.DirectionMode;
            _turnTypeDropdown.value = PlayerPrefsManager.RotationType;
            _snapTurnAngleSlider.value = PlayerPrefsManager.SnapRotationAmount;
            _smoothTurnSpeedSlider.value = PlayerPrefsManager.SmoothRotationSpeed;
        }

        public void OnMovementTypeChanged(int value)
        {
            PlayerPrefsManager.DirectionMode = value;
        }

        public void OnTurnTypeChanged(int value)
        {
            PlayerPrefsManager.RotationType = value;
        }
        
        public void OnSnapTurnAngleChanged(float value)
        {
            PlayerPrefsManager.SnapRotationAmount = (int)value;
        }
        
        public void OnSmoothTurnSpeedChanged(float value)
        {
            PlayerPrefsManager.SmoothRotationSpeed = (int)value;
        }
    }
}
