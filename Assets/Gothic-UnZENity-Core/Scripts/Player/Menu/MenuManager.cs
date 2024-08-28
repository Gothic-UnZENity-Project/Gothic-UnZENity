using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.UI;
using GUZ.Core.Util;
using MyBox;
using TMPro;
using UnityEngine;

namespace GUZ.Core.Player.Menu
{
    public class MenuManager : SingletonBehaviour<MenuManager>
    {
        public GameObject MainMenu;
        public GameObject LoadMenu;
        public GameObject SettingsMenu;
        public GameObject MovementMenu;
        public GameObject ImmersionMenu;
        public GameObject TeleportMenu;
        public GameObject SoundMenu;

        public AudioMixerHandler SoundEffectsVolumeHandler;

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
            MainMenuSaveLoadBackground.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.MainMenuSaveLoadBackgroundMaterial;
            MainMenuBackground.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuBackgroundMaterial;
            MainMenuText.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuTextImageMaterial;
        }

        public void SetSettingsValues()
        {
            SoundEffectsVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.PlayerPrefSoundEffectsVolume, 1f));
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
            // Reset fonts of all newly-visible menu items. Otherwise the previously hovered elements will be visible again when going "Back".
            menu.GetComponentsInChildren<TMP_Text>()
                .ForEach(i => i.spriteAsset = GameGlobals.Font.DefaultSpriteAsset);
            
            MainMenu.SetActive(menu == MainMenu);
            LoadMenu.SetActive(menu == LoadMenu);
            SettingsMenu.SetActive(menu == SettingsMenu);
            MovementMenu.SetActive(menu == MovementMenu);
            ImmersionMenu.SetActive(menu == ImmersionMenu);
            TeleportMenu.SetActive(menu == TeleportMenu);
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
