using System;
using System.IO;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenKit;

namespace GUZ.Core.UnZENity_Core.Scripts.UI
{
    public class SaveMenu : AbstractMenu
    {
        public GameObject[] SaveSlots;

        private GameObject Thumbnail;
        private TMP_Text World;
        private TMP_Text SavedAt;
        private TMP_Text GameTime;
        private TMP_Text Version;

        private readonly SaveGame[] _saves = new SaveGame[15];

        private bool _isSaving;
        private bool _isLoading => !_isSaving;

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            CreateRootElements("MENU_SAVEGAME_SAVE");

            // World = transform.FindChildRecursively("MENUITEM_LOADSAVE_LEVELNAME_VALUE")?.GetComponent<TMP_Text>();
            // SavedAt = transform.FindChildRecursively("MENUITEM_LOADSAVE_DATETIME_VALUE")?.GetComponent<TMP_Text>();
            // GameTime = transform.FindChildRecursively("MENUITEM_LOADSAVE_GAMETIME_VALUE")
            //     ?.GetComponent<TMP_Text>();
            // Version = this.transform.FindChildRecursively("MENUITEM_LOADSAVE_LEVELNAME_VALUE")?.GetComponent<TMP_Text>();

            // for (int i = 1; i <= 15; i++)
            // {
            //     SaveSlots[i] = transform.FindChildRecursively($"MENUITEM_SAVE_SLOT_{i}")?.gameObject;
            //
            //     var button = SaveSlots[i].GetComponentInChildren<Button>();
            //     button.onClick.AddListener(() => OnLoadGameSlotClick(i));
            //
            //     var eventTrigger = SaveSlots[i].GetComponentInChildren<EventTrigger>();
            //     var pointerEnterEntry = new EventTrigger.Entry
            //     {
            //         eventID = EventTriggerType.PointerEnter,
            //     };
            //     pointerEnterEntry.callback.AddListener(_ => OnLoadGameSlotPointerEnter(i));
            //     eventTrigger.triggers.Add(pointerEnterEntry);
            //
            //     var pointerExitEntry = new EventTrigger.Entry
            //     {
            //         eventID = EventTriggerType.PointerExit,
            //     };
            //     pointerExitEntry.callback.AddListener(_ => OnLoadGameSlotPointerExit());
            //     eventTrigger.triggers.Add(pointerExitEntry);
            // }
            //
            // FillSaveGameEntries();
        }

        protected override void Undefined(string commandName)
        {
            Debug.Log($"Main Menu Undefined: {commandName}");
        }

        protected override void Back(string commandName)
        {
            _menuManager.BackMenu();
        }

        protected override void StartMenu(string commandName)
        {
            _menuManager.OpenMenu(commandName);
        }

        protected override void StartItem(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void Close(string commandName)
        {
            _menuManager.CloseAllMenus();
        }

        protected override void ConsoleCommand(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void PlaySound(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteCommand(string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return true;
        }

        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            Thumbnail.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        }

        private void FillSaveGameEntries()
        {
            var gothicDir = GameContext.GameVersionAdapter.RootPath;
            var saveGameListPath = Path.GetFullPath(Path.Join(gothicDir, "Saves"));

            foreach (var fullPath in Directory.EnumerateDirectories(saveGameListPath))
            {
                var saveGameFolderName = Path.GetFullPath(fullPath).Remove(0, saveGameListPath.Length + 1);

                if (!saveGameFolderName.StartsWith("savegame"))
                {
                    continue;
                }

                // G1 save games start with ID 1
                var folderSaveId = int.Parse(saveGameFolderName.Remove(0, "savegame".Length));

                // Load metadata
                var save = GameGlobals.SaveGame.GetSaveGame(folderSaveId);
                _saves[folderSaveId - 1] = save;

                // Set metadata to slot
                var saveSlotGo = SaveSlots[folderSaveId - 1];
                saveSlotGo.GetComponentInChildren<TMP_Text>().text = save.Metadata.Title;
            }
        }

        public void OnLoadGameSlotPointerEnter(int id)
        {
            var save = _saves[id - 1];

            if (save == null)
            {
                return;
            }

            // Thumbnail.GetComponent<MeshRenderer>().material.mainTexture
                // = TextureCache.TryGetTexture(save.Thumbnail, "savegame_" + save.Metadata.Title);
            // Thumbnail.SetActive(true);
            World.text = save.Metadata.World;
            SavedAt.text = save.Metadata.SaveDate;
            GameTime.text = $"{save.Metadata.TimeDay} - {save.Metadata.TimeHour}:{save.Metadata.TimeMinute}";
            // Version.text = save.Metadata.VersionAppName;
        }

        public void OnLoadGameSlotPointerExit()
        {
            // Thumbnail.SetActive(false);
            World.text = "";
            SavedAt.text = "";
            GameTime.text = "";
            // Version.text = "";
        }

        public void OnLoadGameSlotClick(int id)
        {
            if (_isLoading)
            {
                var save = _saves[id - 1];

                if (save == null)
                {
                    return;
                }

                // Can be triggered from Scene:mainMenu or Scene:AnyWorld, therefore removing active scene.
                GameManager.I.LoadWorld(save.Metadata.World, id, SceneManager.GetActiveScene().name);
            }
            else
            {
                GameGlobals.SaveGame.SaveCurrentGame(id, $"UnZENity - {DateTime.Now}");
                FillSaveGameEntries();
            }
        }
    }
}
