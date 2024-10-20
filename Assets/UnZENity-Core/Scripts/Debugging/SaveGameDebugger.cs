using System.IO;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Debugging
{
    public class SaveGameDebugger : MonoBehaviour
    {
        public bool DoSaveGame;

        private void OnValidate()
        {
            if (!DoSaveGame)
            {
                return;
            }
            DoSaveGame = false;

            var world = ResourceLoader.TryGetWorld("world.zen")!;
            var save = new SaveGame(GameVersion.Gothic1);
            var saveGamePath = Path.GetFullPath(Path.Join(GameContext.GameVersionAdapter.RootPath, $"Saves/savegame{15}"));

            save.Metadata.Title = "UnZENity-TestSave";

            save.Save(saveGamePath, world, "world");
        }
    }
}
