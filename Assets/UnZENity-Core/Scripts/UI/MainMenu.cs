using GUZ.Core.Globals;
using GUZ.Core.UI;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Menu
{
    public class MainMenu : MonoBehaviour
    {
        public GameObject RootMenu;
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

        public void ToggleVisibility()
        {
            // Toggle visibility
            gameObject.SetActive(!gameObject.activeSelf);

            if (gameObject.activeSelf)
            {
                // Reset if we were in a sub menu last time.
                SwitchMenu(RootMenu);
            }
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
            GameManager.I.LoadWorld(Constants.SelectedWorld, 0, SceneManager.GetActiveScene().name);
#pragma warning restore CS4014
        }

        public void SwitchMenu(GameObject menu)
        {
            // Reset fonts of all newly-visible menu items. Otherwise, the previously hovered elements will be visible again when going "Back".
            menu.GetComponentsInChildren<TMP_Text>()
                .ForEach(i => i.spriteAsset = GameGlobals.Font.DefaultSpriteAsset);
            
            RootMenu.SetActive(menu == RootMenu);
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
