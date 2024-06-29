using System.IO;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit;

namespace GUZ.Core.Player.Menu
{
    public class SaveGameMenu : SingletonBehaviour<SaveGameMenu>
    {
        public GameObject[] SaveSlots;

        [FormerlySerializedAs("thumbnail")] public GameObject Thumbnail;
        [FormerlySerializedAs("world")] public TMP_Text World;
        [FormerlySerializedAs("savedAt")] public TMP_Text SavedAt;
        [FormerlySerializedAs("gameTime")] public TMP_Text GameTime;
        [FormerlySerializedAs("version")] public TMP_Text Version;

        private readonly SaveGame[] _saves = new SaveGame[15];

        /// <summary>
        /// Pre-fill the Load Game entries with names and textures (if existing)
        /// </summary>
        private void Start()
        {
            Thumbnail.GetComponent<MeshRenderer>().material =
                GameGlobals.Textures.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);

            var g1Dir = GameGlobals.Settings.GothicIPath;
            var saveGameListPath = Path.GetFullPath(Path.Join(g1Dir, "Saves"));

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
                var save = SaveGameManager.GetSaveGame(folderSaveId);
                _saves[folderSaveId - 1] = save;

                // Set metadata to slot
                var saveSlotGo = SaveSlots[folderSaveId - 1];
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
            var save = _saves[id];

            if (save == null)
            {
                return;
            }

#pragma warning disable CS4014 // It's intended, that this async call is not awaited.
            SaveGameManager.LoadSavedGame(id, save);
            GameGlobals.Scene.LoadWorld(save.Metadata.World);
#pragma warning restore CS4014
        }
    }
}
