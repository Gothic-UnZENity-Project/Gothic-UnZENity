using GUZ.Core;
using GUZ.Core.Manager;
using HurricaneVR.Framework.Core.Player;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Constants = GUZ.Core.Globals.Constants;

namespace GUZ.HVR.Components
{
    public class GUZHVRPlayerController : HVRPlayerController
    {
        private GUZHVRPlayerInputs _guzInputs => (GUZHVRPlayerInputs)Inputs;

        [Separator("GUZ - Settings")]
        [SerializeField]
        public GameObject MainMenu;

        private void Start()
        {
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(OnPlayerPrefsUpdated);
        }

        protected override void Update()
        {
            base.Update();

            if (_guzInputs.IsMenuActivated && IsGameScene())
            {
                // Toggle visibility
                MainMenu.SetActive(!MainMenu.activeSelf);
            }
        }

        private void OnDestroy()
        {
            GlobalEventDispatcher.PlayerPrefUpdated.RemoveListener(OnPlayerPrefsUpdated);
        }

        /// <summary>
        /// Used in game scenes where you play the game (world.unity, ...)
        /// </summary>
        public void SetNormalControls()
        {
            DirectionStyle = (PlayerDirectionMode)PlayerPrefsManager.DirectionMode;
            RotationType = (RotationType)PlayerPrefsManager.RotationType;
            SnapAmount = PlayerPrefsManager.SnapRotationAmount;
            SmoothTurnSpeed = PlayerPrefsManager.SmoothRotationSpeed;
        }

        /// <summary>
        /// Disable certain actions to keep player stuck in current position.
        /// </summary>
        public void SetLockedControls()
        {
            // Disable physics
            Gravity = 0f;
            MaxFallSpeed = 0f;

            // Disable movement
            MovementEnabled = false;
            MoveSpeed = 0f;
            RunSpeed = 0f;

            // Disable rotation
            RotationEnabled = false;
            SmoothTurnSpeed = 0f;
            SnapAmount = 0f;
        }

        private void OnPlayerPrefsUpdated(string preferenceKey, object value)
        {
            // Just update everything.
            SetNormalControls();
        }

        /// <summary>
        /// Game scenes are all the ones where we play. Aka !=MainMenu, !=Loadings, ...
        /// </summary>
        private bool IsGameScene()
        {
            var activeSceneName = SceneManager.GetActiveScene().name;

            return activeSceneName switch
            {
                Constants.SceneMainMenu => false,
                Constants.SceneLoading => false,
                _ => true
            };
        }
    }
}
