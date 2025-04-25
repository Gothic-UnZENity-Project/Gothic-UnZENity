using System;
using GUZ.Core.Globals;
using MyBox;
using TMPro;
using UnityEngine;

namespace GUZ.Core.UI.Menus
{
    [Obsolete("Use SettingsMenu.cs for dynamic setting setup instead.")]
    public class SettingsMainMenu : AbstractMenu
    {
        public GameObject SettingsMenu;
        public GameObject MovementMenu;
        public GameObject ImmersionMenu;
        public GameObject TeleportMenu;
        public GameObject SoundMenu;

        public AudioMixerHandler SoundEffectsVolumeHandler;

        public GameObject MainMenuBackground;

        private void Start()
        {
            SetSettingsValues();
            SetMaterials();
            MenuHandler = transform.parent.GetComponent<MenuHandler>();
        }

        public void ExitSettingsMenu()
        {
            Back("","");
        }

        private void OnEnable()
        {
            // Reset if we were in a sub menu last time.
            SwitchMenu(SettingsMenu);
        }

        public void SetMaterials()
        {
            MainMenuBackground.GetComponent<MeshRenderer>().material = GameGlobals.Textures.MainMenuBackgroundMaterial;
        }

        public void SetSettingsValues()
        {
            SoundEffectsVolumeHandler.SliderUpdate(PlayerPrefs.GetFloat(Constants.PlayerPrefSoundEffectsVolume, 1f));
        }

        public void SwitchMenu(GameObject menu)
        {
            // Reset fonts of all newly-visible menu items. Otherwise, the previously hovered elements will be visible again when going "Back".
            menu.GetComponentsInChildren<TMP_Text>()
                .ForEach(i => i.spriteAsset = GameGlobals.Font.DefaultSpriteAsset);
            SettingsMenu.SetActive(menu == SettingsMenu);
            MovementMenu.SetActive(menu == MovementMenu);
            ImmersionMenu.SetActive(menu == ImmersionMenu);
            TeleportMenu.SetActive(menu == TeleportMenu);
            SoundMenu.SetActive(menu == SoundMenu);
        }
        
        
        protected override void Undefined(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void StartMenu(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void StartItem(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void Close(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void ConsoleCommand(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void PlaySound(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }

        protected override void ExecuteCommand(string itemName, string commandName)
        {
            throw new NotImplementedException();
        }
    }
}
