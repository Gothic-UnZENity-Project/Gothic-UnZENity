using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.UI;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.Serialization;

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

        public MoveSpeedController MoveSpeedController;
        public TurnSettingDropdownController TurnSettingDropdownController;
        public AudioMixerHandler MusicVolumeHandler;
        public AudioMixerHandler SoundEffectsVolumeHandler;

        public GameObject MainMenuImageBackground;
        public GameObject MainMenuBackground;
        public GameObject MainMenuSaveLoadBackground;
        public GameObject MainMenuText;


        private void Start()
        {
            SetSettingsValues();
            SetMaterials();
        }

        public void SetMaterials()
        {
            MainMenuImageBackground.GetComponent<MeshRenderer>().material = TextureManager.I.mainMenuImageBackgroundMaterial;
            MainMenuSaveLoadBackground.GetComponent<MeshRenderer>().material =
                TextureManager.I.mainMenuSaveLoadBackgroundMaterial;
            MainMenuBackground.GetComponent<MeshRenderer>().material = TextureManager.I.mainMenuBackgroundMaterial;
            MainMenuText.GetComponent<MeshRenderer>().material = TextureManager.I.mainMenuTextImageMaterial;
        }

        public void SetSettingsValues()
        {
            if (MoveSpeedController == null || TurnSettingDropdownController == null)
                return;

            MoveSpeedController.ChangeMoveSpeed(PlayerPrefs.GetFloat(Constants.moveSpeedPlayerPref));
            TurnSettingDropdownController.DropdownItemSelected(PlayerPrefs.GetInt(Constants.turnSettingPlayerPref));
            MusicVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.musicVolumePlayerPref, 1f));
            SoundEffectsVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.soundEffectsVolumePlayerPref, 1f));
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

            MainMenuBackground.SetActive(menu != LoadMenu);
            MainMenuSaveLoadBackground.SetActive(menu == LoadMenu);
        }

        public void QuitGameFunction()
        {
            Application.Quit();
        }
    }
}
