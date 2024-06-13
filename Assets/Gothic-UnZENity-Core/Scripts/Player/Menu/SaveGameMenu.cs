using System.IO;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
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

        private SaveGame[] _save = new SaveGame[15];

        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            thumbnail.GetComponent<MeshRenderer>().material = TextureManager.I.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);

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
                var saveSlotGo = SaveSlots[saveId];
                saveSlotGo.GetComponentInChildren<TMP_Text>().text = save.Metadata.Title;
            }
        }

        public void OnLoadGameSlotPointerEnter(int id)
        {
            var save = _save[id];

            if (save == null)
            {
                return;
            }

            thumbnail.GetComponent<MeshRenderer>().material.mainTexture
                = TextureCache.TryGetTexture(save.Thumbnail, "savegame_"+save.Metadata.Title);
            thumbnail.SetActive(true);
            world.text = save.Metadata.World;
            savedAt.text = save.Metadata.SaveDate;
            gameTime.text = $"{save.Metadata.TimeDay} - {save.Metadata.TimeHour}:{save.Metadata.TimeMinute}";
            version.text = save.Metadata.VersionAppName;
        }

        public void OnLoadGameSlotPointerExit()
        {
            thumbnail.SetActive(false);
            world.text = "";
            savedAt.text = "";
            gameTime.text = "";
            version.text = "";
        }

        public void OnLoadGameSlotClick(int id)
        {
            // TBD
        }
    }
}
