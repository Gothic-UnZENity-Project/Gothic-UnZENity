using System.Collections.Generic;
using System.IO;
using GUZ.Core.World;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Manager
{
    /// <summary>
    /// Usage:
    ///
    /// Loading:
    /// 1. LoadNewGame() | LoadSavedGame()  -> Initializes the save game state
    /// 2. ChangeWorld(worldName:str)       -> Load the required world. Will be fetched from save or from game data itself.
    ///
    /// Saving:
    /// 1. + 2. Load*() and ChangeWorld()   -> Needs to be called before to fill the data.
    /// 3. SaveGame(saveGameId:int)         -> Will use the currently loaded world from runtime and stores changes.
    ///
    /// Helper methods:
    /// * GetSaveGame(saveGameId:int)       -> Return a save game object (or null) if requested. (e.g. used for LoadMenu to prepare data.
    /// </summary>
    public static class SaveGameManager
    {
        public static int SaveGameId;
        public static bool IsNewGame => SaveGameId <= 0;
        public static bool IsLoadedGame => !IsNewGame;
        public static bool IsFirstWorldLoadingFromSaveGame; // Check if we load save game right now!

        /// <summary>
        /// Values can be:
        /// - true - When we start a new game and load first world | when we visit another world for the first time
        /// - false - We load a save game and spawn where we left off last time | when we visit another world for a n-th time
        ///
        /// Visiting a world for the first time can be triggered with or without leveraging a save game.
        /// It only matters if it's the first time! (Save games only include world saves if we visited it before.)
        /// </summary>
        public static bool IsWorldLoadedForTheFirstTime;

        public static SaveGame Save;

        private static readonly Dictionary<string, (ZenKit.World zkWorld, WorldData uWorld)> _worlds = new();
        public static string CurrentWorldName;
        public static ZenKit.World CurrentZkWorld => _worlds[CurrentWorldName].zkWorld;
        public static WorldData CurrentWorldData => _worlds[CurrentWorldName].uWorld;


        public static void LoadNewGame()
        {
            SaveGameId = 0;
            Save = new SaveGame(GameVersion.Gothic1);
        }

        /// <summary>
        /// Hint: G1 save game folders start with 1. We leverage the same numbering.
        /// </summary>
        public static void LoadSavedGame(int saveGameId)
        {
            LoadSavedGame(saveGameId, GetSaveGame(saveGameId));
        }

        public static void LoadSavedGame(int saveGameId, SaveGame save)
        {
            if (save == null)
            {
                Debug.LogError($"SaveGame with id {saveGameId} doesn't exist.");
                return;
            }

            SaveGameId = saveGameId;
            Save = save;
            IsFirstWorldLoadingFromSaveGame = true;
        }

        /// <summary>
        /// Loading logic order:
        /// 1. Check if the world is already loaded (cached) for this game session (i.e. we visited it already in this session)
        /// 2. Try to load the world state from the save game
        /// 3. Either use this saved world data or load it from normal .zen file
        /// </summary>
        public static void ChangeWorld(string worldName)
        {
            CurrentWorldName = worldName;

            // 1. World was already loaded.
            if (_worlds.ContainsKey(worldName))
            {
                IsWorldLoadedForTheFirstTime = true;
                return;
            }

            var world = ResourceLoader.TryGetWorld(worldName);
            ZenKit.World saveGameWorld = null;

            // 2. Try to load world from save game.
            if (IsLoadedGame)
            {
                saveGameWorld = Save.LoadWorld(worldName);
            }

            // If there is no world saved, we visit it for the first time.
            IsWorldLoadedForTheFirstTime = saveGameWorld == null;

            // 3. Store this world into runtime data as it's now loaded and cached during gameplay. (To save later when needed.)
            _worlds[worldName] = new()
            {
                zkWorld = world,
                uWorld = new WorldData
                {
                    // Not contained inside saveGame
                    Mesh = (CachedMesh)world.Mesh.Cache(),
                    BspTree = (CachedBspTree)world.BspTree.Cache(),
                    // Contained inside normal .zen file and also saveGame.
                    Vobs = saveGameWorld == null ? world.RootObjects : saveGameWorld.RootObjects,
                    WayNet = (CachedWayNet)(saveGameWorld == null ? world.WayNet.Cache() : saveGameWorld.WayNet.Cache())
                }
            };
        }

        [CanBeNull]
        public static SaveGame GetSaveGame(int folderSaveId)
        {
            // Load metadata
            var save = new SaveGame(GameVersion.Gothic1);
            var saveGamePath = GetSaveGamePath(folderSaveId);

            if (!Directory.Exists(saveGamePath))
            {
                Debug.LogError($"SaveGame inside folder >{saveGamePath}< doesn't exist.");
                return null;
            }

            save.Load(GetSaveGamePath(folderSaveId));

            return save;
        }

        /// <summary>
        /// Saving means:
        /// 1. Collect changed data from all the worlds visited during this gameplay
        /// 2. Alter its values inside the ZenKit data
        /// 3. Save world-by-world into the save game itself
        /// </summary>
        //FIXME - untested!
        public static void SaveGame(int saveGameId)
        {
            foreach (var world in _worlds)
            {
                PrepareWorldDataForSaving(world.Value.zkWorld, world.Value.uWorld);
                Save.Save(GetSaveGamePath(saveGameId), world.Value.zkWorld, world.Key);
            }
        }

        /// <summary>
        /// We write data from Unity data back into ZenKit data.
        /// Hint: Not all elements need to be replaced and therefore have no setter (e.g. .Mesh, .WayNet).
        /// We therefore only set what's needed.
        /// </summary>
        //FIXME - untested!
        private static void PrepareWorldDataForSaving(ZenKit.World zkWorld, WorldData uWorld)
        {
            zkWorld.RootObjects = uWorld.Vobs;
        }

        private static string GetSaveGamePath(int folderSaveId)
        {
            var g1Dir = GameGlobals.Settings.GothicIPath;
            return Path.GetFullPath(Path.Join(g1Dir, $"Saves/savegame{folderSaveId}"));
        }
    }
}
