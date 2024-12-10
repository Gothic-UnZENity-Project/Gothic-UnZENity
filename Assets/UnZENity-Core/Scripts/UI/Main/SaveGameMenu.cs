using System;
using System.IO;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit;

namespace GUZ.Core.UI.Main
{
    public class SaveGameMenu : SingletonBehaviour<SaveGameMenu>
    {
        public GameObject[] SaveSlots;

        public TMP_Text Title;
        public GameObject Thumbnail;
        public TMP_Text World;
        public TMP_Text SavedAt;
        public TMP_Text GameTime;
        public TMP_Text Version;

        private readonly SaveGame[] _saves = new SaveGame[15];

        private bool _isSaving;
        private bool _isLoading => !_isSaving;


        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            Thumbnail.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
        }

        public void SetIsLoading()
        {
            FillSaveGameEntries();
            Title.text = "LOAD GAME";
            _isSaving = false;
        }

        public void SetIsSaving()
        {
            FillSaveGameEntries();
            Title.text = "SAVE GAME";
            _isSaving = true;
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

            Thumbnail.GetComponent<MeshRenderer>().material.mainTexture
                = TextureCache.TryGetTexture(save.Thumbnail, "savegame_" + save.Metadata.Title);
            Thumbnail.SetActive(true);
            World.text = save.Metadata.World;
            SavedAt.text = save.Metadata.SaveDate;
            GameTime.text = $"{save.Metadata.TimeDay} - {save.Metadata.TimeHour}:{save.Metadata.TimeMinute}";
            Version.text = save.Metadata.VersionAppName;
        }

        public void OnLoadGameSlotPointerExit()
        {
            Thumbnail.SetActive(false);
            World.text = "";
            SavedAt.text = "";
            GameTime.text = "";
            Version.text = "";
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
