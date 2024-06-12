using System.IO;
using GUZ.Core.Manager.Settings;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Player.Menu
{
    public class SaveGameMenu: SingletonBehaviour<SaveGameMenu>
    {
        public GameObject[] SaveSlots;

        public GameObject thumbnail;
        public TMP_Text world;
        public TMP_Text savedAt;
        public TMP_Text gameTime;
        public TMP_Text version;
        public TMP_Text loadHint;

        private SaveGame[] _save = new SaveGame[15];

        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            var g1Dir = SettingsManager.GameSettings.GothicIPath;
            var saveGameListPath = Path.GetFullPath(Path.Join(g1Dir, "Saves"));

            foreach (var fullPath in Directory.EnumerateDirectories(saveGameListPath))
            {
                var saveGameFolderName = Path.GetFullPath(fullPath).Remove(0, saveGameListPath.Length+1);

                if (!saveGameFolderName.StartsWith("savegame"))
                {
                    continue;
                }

                // G1 save games start with ID 1
                var saveId = int.Parse(saveGameFolderName.Remove(0, "savegame".Length)) - 1;

                // Load metadata
                var save = new SaveGame(GameVersion.Gothic1);
                save.Load(fullPath);
                _save[saveId] = save;

                // Set metadata to slot
                var saveSlotGO = SaveSlots[saveId];
                saveSlotGO.GetComponentInChildren<TMP_Text>().text = save.Metadata.Title;
            }
        }

        public void OnLoadGameSlotPointerEnter(int id)
        {
            var save = _save[id];

            if (save == null)
            {
                return;
            }

            world.text = save.Metadata.World;
            savedAt.text = save.Metadata.SaveDate;
            gameTime.text = save.Metadata.PlayTime.ToString();
            version.text = save.Metadata.VersionAppName;
            loadHint.text = "DEBUG - Slot X - RETURN zum Laden des gespeicherten Spielstandes!";
        }

        public void OnLoadGameSlotPointerExit()
        {
            world.text = "";
            savedAt.text = "";
            gameTime.text = "";
            version.text = "";
            loadHint.text = "";
        }

        public void OnLoadGameSlotClick(int id)
        {
            // TBD
        }
    }
}
