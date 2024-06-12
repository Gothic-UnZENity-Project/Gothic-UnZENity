using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.UI;
using GUZ.Core.Util;
using UnityEngine;

namespace GUZ.Core.Player.Menu
{
    public class MenuManager : SingletonBehaviour<MenuManager>
    {
        public GameObject MainMenu;
        public GameObject LoadMenu;
        public GameObject SettingsMenu;
        public GameObject TeleportMenu;
        public GameObject MovementMenu;
        public GameObject SoundMenu;

        [SerializeField]
        private MoveSpeedController moveSpeedController;

        [SerializeField]
        private TurnSettingDropdownController turnSettingDropdownController;

        [SerializeField] private AudioMixerHandler musicVolumeHandler;
        [SerializeField] private AudioMixerHandler soundEffectsVolumeHandler;

        void Awake()
        {
            SetSettingsValues();
        }

        public void SetSettingsValues()
        {
            if (moveSpeedController == null || turnSettingDropdownController == null)
                return;

            moveSpeedController.ChangeMoveSpeed(PlayerPrefs.GetFloat(Constants.moveSpeedPlayerPref));
            turnSettingDropdownController.DropdownItemSelected(PlayerPrefs.GetInt(Constants.turnSettingPlayerPref));
            musicVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.musicVolumePlayerPref, 1f));
            soundEffectsVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.soundEffectsVolumePlayerPref, 1f));
        }

        public void PlayFunction()
        {
#pragma warning disable CS4014 // It's intended, that this async call is not awaited.
            GUZSceneManager.I.LoadWorld(Constants.selectedWorld, Constants.selectedWaypoint, true);
#pragma warning restore CS4014
        }

        public void SwitchMenu(GameObject menu)
        {
            MainMenu.SetActive(menu == MainMenu);
            LoadMenu.SetActive(menu == LoadMenu);
            SettingsMenu.SetActive(menu == SettingsMenu);
            TeleportMenu.SetActive(menu == TeleportMenu);
            MovementMenu.SetActive(menu == MovementMenu);
            SoundMenu.SetActive(menu == SoundMenu);
        }

        public void QuitGameFunction()
        {
            Application.Quit();
        }
    }
}
