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
            _movementTypeDropdown.value = PlayerPrefsManager.MovementType;
            _turnTypeDropdown.value = PlayerPrefsManager.TurnType;
            _snapTurnAngleSlider.value = PlayerPrefsManager.SnapTurnAngle;
            _smoothTurnSpeedSlider.value = PlayerPrefsManager.SmoothTurnSpeed;
        }

        public void OnMovementTypeChanged(int value)
        {
            PlayerPrefsManager.MovementType = value;
        }

        public void OnTurnTypeChanged(int value)
        {
            PlayerPrefsManager.TurnType = value;
        }
        
        public void OnSnapTurnAngleChanged(float value)
        {
            PlayerPrefsManager.SnapTurnAngle = (int)value;
        }
        
        public void OnSmoothTurnSpeedChanged(float value)
        {
            PlayerPrefsManager.SmoothTurnSpeed = (int)value;
        }
    }
}
