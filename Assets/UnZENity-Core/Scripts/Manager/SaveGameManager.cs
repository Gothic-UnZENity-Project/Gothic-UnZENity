using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Mesh = ZenKit.Mesh;
using Texture = ZenKit.Texture;
using TextureFormat = ZenKit.TextureFormat;
using Vector3 = System.Numerics.Vector3;

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
    public class SaveGameManager
    {
        public int SaveGameId;
        public bool IsNewGame => SaveGameId < 1;
        public bool IsLoadedGame => !IsNewGame;
        public bool IsFirstWorldLoadingFromSaveGame; // Check if we load save game right now!

        /// <summary>
        /// Values can be:
        /// - true - When we start a new game and load first world | when we visit another world for the first time
        /// - false - We load a save game and spawn where we left off last time | when we visit another world for a n-th time
        ///
        /// Visiting a world for the first time can be triggered with or without leveraging a save game.
        /// It only matters if it's the first time! (Save games only include world saves if we visited it before.)
        /// </summary>
        public bool IsWorldLoadedForTheFirstTime;

        public SaveGame Save;

        private readonly Dictionary<string, WorldContainer> _worlds = new();
        public string CurrentWorldName;
        public WorldContainer CurrentWorldData => _worlds[CurrentWorldName];

        // When we load data like World.Mesh, we can't cache it for memory reasons.
        // But we need to store the reference to the parent object (World) in here as long as we want to work with the sub-data.


        public void Init()
        {
            // Nothing to do for now.
        }

        public void LoadNewGame()
        {
            SaveGameId = 0;
            Save = new SaveGame(GameContext.GameVersionAdapter.Version);
            IsFirstWorldLoadingFromSaveGame = true;
        }

        /// <summary>
        /// Hint: G1 save game folders start with 1. We leverage the same numbering.
        /// </summary>
        public void LoadSavedGame(int saveGameId)
        {
            LoadSavedGame(saveGameId, GetSaveGame(saveGameId));
        }

        public void LoadSavedGame(int saveGameId, SaveGame save)
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
        public void ChangeWorld(string worldName)
        {
            CurrentWorldName = worldName;

            // 1. World was already loaded.
            if (_worlds.ContainsKey(worldName))
            {
                IsWorldLoadedForTheFirstTime = false;
                return;
            }

            IsWorldLoadedForTheFirstTime = true;
            ZenKit.World originalWorld = ResourceLoader.TryGetWorld(worldName)!; // Always needed for some data not present in SaveGame.
            ZenKit.World saveGameWorld = null;
            bool worldFoundInSaveGame = false;

            // 2. Try to load world from save game.
            if (IsLoadedGame)
            {
                saveGameWorld = Save.LoadWorld(worldName);
                worldFoundInSaveGame = saveGameWorld != null;
            }

            ZenKit.World worldToUse;
            if (worldFoundInSaveGame)
            {
                worldToUse = saveGameWorld;
                IsWorldLoadedForTheFirstTime = false;
            }
            else
            {
                // If there is no save game used or world not saved, we visit it for the first time.
                worldToUse = originalWorld;
            }

            // 3. Store this world into runtime data as it's now loaded and cached during gameplay. (To save later when needed.)
            // TODO - If we get memory consumption issue, we can consider removing some data to free memory once world is loaded later.
            _worlds[worldName] = new WorldContainer
            {
                OriginalWorld = originalWorld,
                SaveGameWorld = saveGameWorld,

                // Only existing in normal world
                Mesh = (Mesh)originalWorld.Mesh, // Do not cache or memory consumption will be way too high
                BspTree = (CachedBspTree)originalWorld.BspTree.Cache(),

                // Only existing in SaveGame world
                Npcs = worldToUse.Npcs, // (if it's a new world, it's simply null)

                // Contained inside both: normal .zen file and also saveGame.
                Vobs = worldToUse!.RootObjects,
                WayNet = (CachedWayNet)worldToUse.WayNet.Cache()
            };
        }

        [CanBeNull]
        public SaveGame GetSaveGame(int folderSaveId)
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
        ///
        /// Hint: Needs to be called after EndOfFrame to ensure we can do a screenshot as thumbnail.
        /// </summary>
        public void SaveGame(int saveGameId, string title)
        {
            var saveGame = new SaveGame(GameContext.GameVersionAdapter.Version);
            saveGame.Metadata.Title = title;
            saveGame.Metadata.SaveDate = DateTime.Now.ToString();
            saveGame.Thumbnail = CreateThumbnail();
            saveGame.Metadata.World = CurrentWorldName.ToUpper();

            foreach (var worldData in _worlds)
            {
                var world = worldData.Value.OriginalWorld;
                // FIXME - We need to create a new combined world first.
                PrepareWorldDataForSaving(worldData.Value);
                saveGame.Save(GetSaveGamePath(saveGameId), world, worldData.Key.TrimEndIgnoreCase(".ZEN").ToUpper());
            }
        }

        private Texture CreateThumbnail()
        {
            int pixelsPerAxis = 256; // Default size of a G1 Thumbnail
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture(ScreenCapture.StereoScreenCaptureMode.BothEyes);
            Texture2D formattedScreenshot;

            // Alter dimensions of screenshots to align with Gothic thumbnail format.
            {
                RenderTexture rt = RenderTexture.GetTemporary(pixelsPerAxis, pixelsPerAxis);
                rt.filterMode = FilterMode.Bilinear;

                RenderTexture.active = rt;
                Graphics.Blit(screenshot, rt);

                formattedScreenshot = new Texture2D(pixelsPerAxis, pixelsPerAxis, UnityEngine.TextureFormat.RGB565, false);
                formattedScreenshot.ReadPixels(new Rect(0, 0, pixelsPerAxis, pixelsPerAxis), 0, 0);
                formattedScreenshot.Apply();

                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                // Gothic textures need to be flipped to be shown correctly.
                var originalPixels = formattedScreenshot.GetPixels();
                var yFlippedPixels = new Color[originalPixels.Length];
                for (var row = 0; row < pixelsPerAxis; ++row)
                {
                    // We iterate through every row and reverse the whole row (aka flipping y-axis)
                    Array.Copy(originalPixels, row * pixelsPerAxis, yFlippedPixels, (pixelsPerAxis - row - 1) * pixelsPerAxis, pixelsPerAxis);
                }
                formattedScreenshot.SetPixels(yFlippedPixels);
            }

            TextureBuilder builder = new TextureBuilder(pixelsPerAxis, pixelsPerAxis);

            builder.AddMipmap(formattedScreenshot.GetRawTextureData(), TextureFormat.R5G6B5);

            return builder.Build(TextureFormat.R5G6B5);
        }

        /// <summary>
        /// We write data from Unity data back into ZenKit data.
        /// Hint: Not all elements need to be replaced and therefore have no setter (e.g. .Mesh, .WayNet).
        /// We therefore only set what's needed.
        ///
        /// This includes:
        /// 1. Fill all the different VOB types which we need to store separately.
        /// 2. Collect all VOBs in a plain structure (except Npcs far away)
        /// 3. Add all far-away NPCs+Monsters to the .Npcs list
        /// </summary>
        private void PrepareWorldDataForSaving(WorldContainer data)
        {
            data.OriginalWorld.NpcSpawnEnabled = true;

            VobProperties[] allVobs = GameObject.FindObjectsOfType<VobProperties>(includeInactive: true);
            List<VobProperties> allVobsExcludingNpcs = new List<VobProperties>();
            List<VobProperties> allVisibleNpcs = new List<VobProperties>();
            List<VobProperties> allFarAwayNpcs = new List<VobProperties>();
            VobProperties hero = null;

            // 1. Fill all the different VOB types which we need to store separately.
            foreach (var vobComp in allVobs)
            {
                var vobData = vobComp.Properties;
                if (vobData == null)
                {
                    Debug.LogError("A VOB has no ZenKit.Vobs.* property attached and therefore can't be stored inside SaveGame.", vobComp);
                    continue;
                }

                if (vobData.Type == VirtualObjectType.oCNpc)
                {
                    if (vobData.Name == GameGlobals.Settings.IniPlayerInstanceName)
                    {
                        hero = vobComp; // For special temp reasons.
                        allVisibleNpcs.Add(vobComp);
                    }
                    else if (vobComp.gameObject.activeInHierarchy)
                    {
                        allVisibleNpcs.Add(vobComp);
                    }
                    else
                    {
                        allFarAwayNpcs.Add(vobComp);
                    }
                }
                else
                {
                    allVobsExcludingNpcs.Add(vobComp);
                }
            }

            // 1.1 Store current position of all NPCs into their VOBs
            foreach (var npc in allVisibleNpcs)
            {
                var npcData = ((NpcProperties)npc).NpcData;
                PrepareNpcForSaving(npcData);
            }

            // 1.2 Do the same for all the other NPCs
            foreach (var npc in allFarAwayNpcs)
            {
                var npcData = ((NpcProperties)npc).NpcData;
                PrepareNpcForSaving(npcData);
            }

            // 1.3 We assume, that all VOBs (Doors, etc.) have their settings already inside the VOBs. Therefore, no additional handling needed.
            // ...

            // 2. Collect all VOBs in a plain structure (except Npcs far away)
            // Every VOB is created via Prefab, it's root GameObject has the VobTag and a VobProperties component with the actual ZenKit.VirtualObject property.
            // If the saving crashes here based on NPEs, then a Prefab isn't set correctly.
            {
                var rootObjects = new List<IVirtualObject>();

                // Add elements in order of appearance in an original save game file.
                rootObjects.AddRange(allVisibleNpcs.Select(i => i.Properties));
                rootObjects.AddRange(allVobsExcludingNpcs.Select(i => i.Properties));

                data.OriginalWorld.RootObjects = rootObjects;
            }

            // 3. Add all far-away NPCs+Monsters to the .Npcs list
            {
                data.OriginalWorld.Npcs = allFarAwayNpcs.Select(i => (ZenKit.Vobs.Npc)i.Properties).ToList();
            }
        }

        /// <summary>
        /// When a NpcInstance is initialized or saved, we afterward need to copy some data to VOB for handling and saving.
        /// </summary>
        private void PrepareNpcForSaving(NpcContainer data)
        {
            var instance = data.Instance;
            var vob = data.Vob;
            var go = data.Go;
            vob.NpcInstance = GameData.GothicVm.GetSymbolByIndex(instance.Index)!.Name;

            vob.Name = GameData.GothicVm.GetSymbolByIndex(instance.Index)!.Name; // e.g. PC_THIEF
            vob.Position = go.transform.position.ToNumericsVector();
            vob.Rotation = go.transform.rotation.ToMatrix3x3();

            vob.HasRoutine = instance.DailyRoutine > 0;
            if (instance.DailyRoutine > 0)
            {
                vob.CurrentRoutine = GameData.GothicVm.GetSymbolByIndex(instance.DailyRoutine)!.Name;
            }

            if (instance.StartAiState > 0)
            {
                vob.StartAiState = GameData.GothicVm.GetSymbolByIndex(instance.StartAiState)!.Name;
            }

            vob.Guild = instance.Guild;
            vob.Level = instance.Level;
            vob.FightTactic = instance.FightTactic;
            vob.RespawnTime = instance.SpawnDelay;
            vob.Xp = instance.Exp;
            vob.XpNextLevel = instance.ExpNext;
            vob.Lp = instance.Lp;
            vob.BsInterruptableOverride = instance.BodyStateInterruptableOverride;
            vob.ModelScale = vob.ModelScale.Length() == 0 ? Vector3.One : vob.ModelScale; // Set default if empty.

            for (var i = 0; i < 5; i++)
            {
                vob.SetMission(i, instance.GetMission((NpcMissionSlot)i));
            }

            for (var i = 0; i < 8; i++)
            {
                vob.SetAttribute(i, instance.GetAttribute((NpcAttribute)i));
            }

            for (var i = 0; i < 4; i++)
            {
                vob.SetHitChance(i, instance.GetHitChance((NpcTalent)i));
            }

            for (var i = 0; i < 8; i++)
            {
                vob.SetProtection(i, instance.GetProtection((DamageType)i));
            }

            var vobAiVars = new int[100];
            for (var i = 0; i < vobAiVars.Length; i++)
            {
                vobAiVars[i] = instance.GetAiVar(i);
            }
            vob.AiVars = vobAiVars;
        }

        private string GetSaveGamePath(int folderSaveId)
        {
            var gothicDir = GameContext.GameVersionAdapter.RootPath;
            return Path.GetFullPath(Path.Join(gothicDir, $"Saves/savegame{folderSaveId}"));
        }
    }
}
