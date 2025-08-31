using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Util;
using GUZ.Core.Vob.WayNet;
using MyBox;
using Reflex.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager.Scenes
{
    public class WorldSceneManager : MonoBehaviour, ISceneManager
    {
        [Inject] private readonly VobManager _vobManager;
        [Inject] private readonly WayNetService _wayNetService;
        [Inject] private readonly MeshService _meshService;

        public void Init()
        {
#pragma warning disable CS4014 // Do not wait. We want to update player movement (VR) and camera view (progress bar)
            LoadWorldContentAsync();
#pragma warning restore CS4014
        }
        
        /// <summary>
        /// Order of loading:
        /// 1. Cache data
        /// 2. World - Mesh of the world
        /// 3. WayPoints - Needed for spawning NPCs when world is loaded the first time
        /// 4. VOBs - First entry, as we slice world chunks based on light VOBs
        /// 5. NPCs - If we load the world for the first time, we leverage their current routine's values
        /// 6. Stationary lights
        /// </summary>
        private async Task LoadWorldContentAsync()
        {
            GameContext.ContextInteractionService.DisableMenus();
            
            var watch = Stopwatch.StartNew();
            var config = GameGlobals.Config;

            var worldRoot = new GameObject("World");
            var vobRoot = new GameObject("VOBs");
            var npcRoot = new GameObject("NPCs");
            // We need to disable all vob meshes during loading. Otherwise, loading time will increase from 10 seconds to 10 minutes. ;-)
            worldRoot.SetActive(false);
            vobRoot.SetActive(false);
            npcRoot.SetActive(false);

            var fullWatch = Stopwatch.StartNew();
            try
            {
                // 1.1
                // Global cache and global calculations (TextureArray) only need to be done once.
                if (!GameGlobals.StaticCache.IsGlobalCacheLoaded)
                {
                    // Load global Static cache and arrange it in memory
                    await GameGlobals.StaticCache.LoadGlobalCache();
                    watch.LogAndRestart("StaticCache - Global loaded");

                    await _meshService.CreateTextureArray();
                    watch.LogAndRestart("Texture array created");
                }

                // 1.2
                // Load world cache
                await GameGlobals.StaticCache.LoadWorldCache(GameGlobals.SaveGame.CurrentWorldName).AwaitAndLog();
                watch.LogAndRestart("StaticCache - World loaded");

                // 2. Load world based on cached Chunks
                if (config.Dev.EnableWorldMesh)
                {
                    await _meshService.CreateWorld(
                        GameGlobals.StaticCache.LoadedWorldChunks,
                        GameGlobals.SaveGame.CurrentWorldData.Mesh,
                        GameGlobals.Loading,
                        worldRoot
                    ).AwaitAndLog();
                    watch.LogAndRestart("World loaded");
                }

                // 3. WayNet
                _wayNetService.Create(config.Dev, GameGlobals.SaveGame.CurrentWorldData);
                watch.LogAndRestart("WayNet initialized");


                // 4. VOBs
                // Build the world and vob meshes, populating the texture arrays.
                // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterward.
                if (config.Dev.EnableVOBs)
                {
                    // If we load a SaveGame, then nearby NPCs are stored as VOB and will be created as GOs inside NpcManager. We need to prepare it before.
                    GameGlobals.Npcs.SetRootGo(npcRoot);

                    await _vobManager.CreateWorldVobsAsync(config.Dev, GameGlobals.Loading, GameGlobals.SaveGame.CurrentWorldData.Vobs, vobRoot)
                        .AwaitAndLog();
                    watch.LogAndRestart("VOBs created");
                }

                // 5. NPCs
                // If the world is visited for the first time, then we need to load Npcs via Wld_InsertNpc()
                GameGlobals.Npcs.CacheHero();
                if (config.Dev.EnableNpcs)
                {
                    // await NpcCreator.CreateAsync(config.Dev, GameGlobals.Loading).AwaitAndLog();
                    await GameGlobals.Npcs.CreateWorldNpcs(GameGlobals.Loading).AwaitAndLog();
                    watch.LogAndRestart("NPCs created");
                }

                // 6. Stationary lights
                // They are affecting (1) World Mesh and (2) VOB meshes.
                // We therefore need to initialize them after both are created.
                GameGlobals.Lights.InitStationaryLights();
                watch.LogAndRestart("Stationary lights initialized");


                // World fully loaded
                // TODO - Does this call add benefits for memory?
                ResourceLoader.ReleaseLoadedData();

                // TODO - Still needed?
                worldRoot.SetActive(true);
                vobRoot.SetActive(true);
                npcRoot.SetActive(true);

                GameGlobals.Sky.InitWorld();

                TeleportPlayerToStart();

                // There are many handlers which listen to this event. If any of these fails, we won't get notified without a try-catch.
                try
                {
                    GlobalEventDispatcher.WorldSceneLoaded.Invoke();
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString(), LogCat.Loading);
                }

                GameGlobals.Loading.StopLoading();
                SceneManager.UnloadSceneAsync(Constants.SceneLoading);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex.ToString(), LogCat.Loading);
            }
            finally
            {
                GameContext.ContextInteractionService.EnableMenus();
                fullWatch.Log("Full world loaded");
            }
        }

        /// <summary>
        /// There are three options, where the player can spawn:
        /// 1. We set it inside GameConfiguration.SpawnAtWaypoint (only during first load of the game)
        /// 2. We got the spawn information from a loaded SaveGame from the Hero's VOB
        /// 3. We get the waypoint to spawn at from a ChangeLevel trigger
        /// 4. We load the START_* waypoint
        /// </summary>
        private void TeleportPlayerToStart()
        {
            // 1.
            var debugSpawnAtWayPoint = GameGlobals.Config.Dev.SpawnAtWaypoint;

            // If we currently load world from a save game, we will use the stored hero position which was set during VOB loading.
            if (GameGlobals.SaveGame.IsFirstWorldLoadingFromSaveGame)
            {
                // We only use the Vob location once per save game loading.
                GameGlobals.SaveGame.IsFirstWorldLoadingFromSaveGame = false;

                if (debugSpawnAtWayPoint.NotNullOrEmpty())
                {
                    // We need to look up WPs and FPs. Therefore this slow check (which is fine, as it's only done for debugging purposes)
                    var debugGo = GameObject.Find(debugSpawnAtWayPoint);

                    if (debugGo != null)
                    {
                        TeleportPlayerToStart(debugGo.transform.position, debugGo.transform.rotation);
                        return;
                    }
                }
            }

            // 2.
            if (GameGlobals.Player.HeroSpawnPosition != default)
            {
                TeleportPlayerToStart(GameGlobals.Player.HeroSpawnPosition, GameGlobals.Player.HeroSpawnRotation);
                return;
            }
            
            var spots = GameData.FreePoints;
            KeyValuePair<string, FreePoint> startPoint;
            
            // 3.
            if(GameGlobals.Player.LastLevelChangeTriggerVobName != null)
            {
                startPoint = spots.FirstOrDefault(
                    go => go.Key.EqualsIgnoreCase(GameGlobals.Player.LastLevelChangeTriggerVobName));
                TeleportPlayerToStart(startPoint.Value.Position, startPoint.Value.Rotation);
                GameGlobals.Player.LastLevelChangeTriggerVobName = null;
                return;
            }

            // 4.
            startPoint = spots.FirstOrDefault(
                go => go.Key.EqualsIgnoreCase("START") || go.Key.EqualsIgnoreCase("START_GOTHIC2")
            );

            if (startPoint.Key.IsNullOrEmpty())
            {
                Logger.LogError("No suitable START_* waypoint found!", LogCat.Loading);
                return;
            }

            TeleportPlayerToStart(startPoint.Value.Position, startPoint.Value.Rotation);
        }

        private void TeleportPlayerToStart(Vector3 position, Quaternion rotation)
        {
            GameContext.ContextInteractionService.TeleportPlayerTo(position, rotation);
            GameContext.ContextInteractionService.UnlockPlayer();
            GameGlobals.Player.ResetSpawn();
        }
    }
}
