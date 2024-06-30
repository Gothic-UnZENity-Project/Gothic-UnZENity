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
            MainMenuImageBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuImageBackgroundMaterial;
            MainMenuSaveLoadBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuSaveLoadBackgroundMaterial;
            MainMenuBackground.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuBackgroundMaterial;
            MainMenuText.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuTextImageMaterial;
        }

        public void SetSettingsValues()
        {
            if (MoveSpeedController == null || TurnSettingDropdownController == null)
            {
                return;
            }

            MoveSpeedController.ChangeMoveSpeed(PlayerPrefs.GetFloat(Constants.MoveSpeedPlayerPref));
            TurnSettingDropdownController.DropdownItemSelected(PlayerPrefs.GetInt(Constants.TurnSettingPlayerPref));
            MusicVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.MusicVolumePlayerPref, 1f));
            SoundEffectsVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.SoundEffectsVolumePlayerPref, 1f));
        }

        public void PlayFunction()
        {
#pragma warning disable CS4014 // It's intended, that this async call is not awaited.
            SaveGameManager.LoadNewGame();
            GameGlobals.Scene.LoadWorld(Constants.SelectedWorld, Constants.SelectedWaypoint);
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
