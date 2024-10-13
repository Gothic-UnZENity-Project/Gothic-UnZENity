#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Manager;
using HurricaneVR.Framework.Core.Player;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Constants = GUZ.Core.Globals.Constants;

namespace GUZ.VR.Components
{
    public class VRPlayerController : HVRPlayerController
    {
        private VRPlayerInputs _guzInputs => (VRPlayerInputs)Inputs;

        [Separator("GUZ - Settings")]
        [SerializeField]
        public GameObject MainMenu;

        protected override void Start()
        {
            base.Start();
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
        /// Disable certain actions to keep player stuck in current position during cutscene.
        /// </summary>
        public void SetLockedControlsCutscene()
        {
            MovementEnabled = false;
            RotationEnabled = true;
        }

        /// <summary>
        /// Disable certain actions to keep player stuck in current position.
        /// </summary>
        public void SetLockedControls()
        {
            // HINT: Disable physics
            // We can't disable physics as it would prevent HVRTeleport.Teleport() from finishing (as it checks for Player.IsGrounded every frame).
            // Therefore, we need to ground the player always on a plane and disable movement only.

            MovementEnabled = false;
            RotationEnabled = false;
        }

        public void SetUnlockedControls()
        {
            MovementEnabled = true;
            RotationEnabled = true;
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
#endif
