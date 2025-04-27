using System;
using System.IO;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.UI.Menus.Adapter.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.Core.UI.Menus
{
    public class SaveMenu : AbstractMenu
    {
        private GameObject[] SaveSlots = new GameObject[16];

        private GameObject Thumbnail;
        private TMP_Text World;
        private TMP_Text SavedAt;
        private TMP_Text GameTime;
        private TMP_Text PlayTime;
        private TMP_Text Version;

        private readonly SaveGame[] _saves = new SaveGame[16];

        [SerializeField] private bool _isSaving;
        private bool _isLoading => !_isSaving;

        private string _saveLoadStatus;

        public override void InitializeMenu(IMenuInstance menuInstance)
        {
            base.InitializeMenu(menuInstance);
            Setup();
        }

        private void Setup()
        {
            _saveLoadStatus = _isSaving ? "SAVE" : "LOAD";

            var thumbnailGo = MenuItemCache["MENUITEM_LOADSAVE_THUMBPIC"].go;
            Thumbnail = ResourceLoader.TryGetPrefabObject(PrefabType.UiThumbnail, name: "Thumbnail",
                parent: thumbnailGo)!;

            Thumbnail.transform.localPosition = Vector3.zero;
            Thumbnail.transform.localRotation = Quaternion.Euler(270, 0, 0);

            World = MenuItemCache["MENUITEM_LOADSAVE_LEVELNAME_VALUE"].go.GetComponent<TMP_Text>();
            SavedAt = MenuItemCache["MENUITEM_LOADSAVE_DATETIME_VALUE"].go.GetComponent<TMP_Text>();
            GameTime = MenuItemCache["MENUITEM_LOADSAVE_GAMETIME_VALUE"].go.GetComponent<TMP_Text>();
            PlayTime = MenuItemCache["MENUITEM_LOADSAVE_PLAYTIME_VALUE"].go.GetComponent<TMP_Text>();
            // Version = this.transform.FindChildRecursively("MENUITEM_LOADSAVE_LEVELNAME_VALUE")?.GetComponent<TMP_Text>();

            for (int i = 1; i <= 15; i++)
            {
                var saveSlot = MenuItemCache[$"MENUITEM_{_saveLoadStatus}_SLOT{i}"].go;

                var text = saveSlot.GetComponentInChildren<TMP_Text>();
                text.text = "---"; // daedalus menu item has text "unknown", but in game it shows as ---
                SaveSlots[i] = saveSlot;
            }

            FillSaveGameEntries();
        }

        protected override void Undefined(string itemName, string commandName)
        {
            return;
        }

        protected override void StartMenu(string itemName, string commandName)
        {
            MenuHandler.OpenMenu(commandName);
        }

        protected override void StartItem(string itemName, string commandName)
        {
            SaveLoadGame(commandName);
        }

        protected override void Close(string itemName, string commandName)
        {
            if (commandName == "SAVEGAME_LOAD")
            {
                SaveLoadGame(itemName);
            }

            MenuHandler.ToggleVisibility();
        }

        protected override void ConsoleCommand(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void PlaySound(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override void ExecuteCommand(string itemName, string commandName)
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsMenuItemInitiallyActive(string menuItemName)
        {
            return (MenuItemCache[menuItemName].item.Flags & MenuItemFlag.Disabled) == 0;
        }

        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            Thumbnail.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        }

        private void OnEnable()
        {
            FillSaveGameEntries();
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
                _saves[folderSaveId] = save;

                // Set metadata to slot
                var saveSlotGo = SaveSlots[folderSaveId];

                var eventTrigger = saveSlotGo.GetComponent<EventTrigger>();

                var pointerEnterEntry = eventTrigger.triggers.Find(x => x.eventID == EventTriggerType.PointerEnter);
                pointerEnterEntry.callback.AddListener(_ => OnLoadGameSlotPointerEnter(folderSaveId));

                var pointerExitEntry = eventTrigger.triggers.Find(x => x.eventID == EventTriggerType.PointerExit);
                pointerExitEntry.callback.AddListener(_ => OnLoadGameSlotPointerExit());

                saveSlotGo.GetComponentInChildren<TMP_Text>().text = save.Metadata.Title;
            }
        }

        public void OnLoadGameSlotPointerEnter(int id)
        {
            var save = _saves[id];

            if (save == null)
            {
                return;
            }

            Thumbnail.GetComponent<MeshRenderer>().material.mainTexture
                = TextureCache.TryGetTexture(save.Thumbnail, "savegame_" + save.Metadata.Title);
            Thumbnail.SetActive(true);
            World.text = save.Metadata.World;
            SavedAt.text = save.Metadata.SaveDate;
            GameTime.text = $"{save.Metadata.TimeDay} - {save.Metadata.TimeHour}:{save.Metadata.TimeMinute}";
            PlayTime.text = save.Metadata.PlayTime.ToString();
            // Version.text = save.Metadata.VersionAppName;
        }

        public void OnLoadGameSlotPointerExit()
        {
            Thumbnail.SetActive(false);
            World.text = "";
            SavedAt.text = "";
            GameTime.text = "";
            PlayTime.text = "";
            // Version.text = "";
        }

        public void SaveLoadGame(string inputName)
        {
            string numberPart = inputName.Substring($"MENUITEM_{_saveLoadStatus}_SLOT".Length);

            if (!int.TryParse(numberPart, out int id))
            {
                id = -2;
            }
            if (_isLoading)
            {
                var save = _saves[id];

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
