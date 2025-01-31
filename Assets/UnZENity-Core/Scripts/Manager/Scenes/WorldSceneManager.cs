using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager.Scenes
{
    public class WorldSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
#pragma warning disable CS4014 // Do not wait. We want to update player movement (VR) and camera view (progress bar) 
            LoadWorldContentAsync();
#pragma warning restore CS4014
        }
        
        /// <summary>
        /// Order of loading:
        /// 1. World - Mesh of the world
        /// 2. VOBs - First entry, as we slice world chunks based on light VOBs
        /// 3. WayPoints - Needed for spawning NPCs when world is loaded the first time
        /// 4. NPCs - If we load the world for the first time, we leverage their current routine's values
        /// </summary>
        private async Task LoadWorldContentAsync()
        {
            var watch = Stopwatch.StartNew();
            var config = GameGlobals.Config;

            var worldRoot = new GameObject("World");
            var vobRoot = new GameObject("VOBs");
            // We need to disable all vob meshes during loading. Otherwise loading time will increase from 10 seconds to 10 minutes. ;-)
            worldRoot.SetActive(false);
            vobRoot.SetActive(false);

            var fullWatch = Stopwatch.StartNew();
            try
            {
                // 0.
                // Load Static cache and arrange it in memory
                await GameGlobals.StaticCache.LoadGlobalCache();
                watch.LogAndRestart("StaticCache - Global loaded");
                await GameGlobals.StaticCache.LoadWorldCache(GameGlobals.SaveGame.CurrentWorldName).AwaitAndLog();
                watch.LogAndRestart("StaticCache - World loaded");

                // TODO - Can be cached and doesn't need to be recreated each world scene loading.
                await MeshFactory.CreateTextureArray();
                watch.LogAndRestart("Texture array created");

                // 1. Load world based on cached Chunks
                if (config.Dev.EnableWorldMesh)
                {
                    await MeshFactory.CreateWorld(
                        GameGlobals.StaticCache.LoadedWorldChunks,
                        GameGlobals.SaveGame.CurrentWorldData.Mesh,
                        GameGlobals.Loading,
                        worldRoot
                    ).AwaitAndLog();
                    watch.LogAndRestart("World loaded");
                }

                // 2.
                // Build the world and vob meshes, populating the texture arrays.
                // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterward.
                if (config.Dev.EnableVOBs)
                {
                    await VobCreator.CreateAsync(config.Dev, GameGlobals.Loading, GameGlobals.SaveGame.CurrentWorldData.Vobs, vobRoot)
                        .AwaitAndLog();
                    watch.LogAndRestart("VOBs created");
                }

                // 3. Stationary lights
                // They are affecting (1) World Mesh and (2) VOB meshes.
                // We therefore need to initialize them after both is created.
                GameGlobals.Lights.InitStationaryLights();
                watch.LogAndRestart("Stationary lights initialized");

                // 3.
                WayNetCreator.Create(config.Dev, GameGlobals.SaveGame.CurrentWorldData);

                // 4.
                // If the world is visited for the first time, then we need to load Npcs via Wld_InsertNpc()
                if (config.Dev.EnableNpcs)
                {
                    await NpcCreator.CreateAsync(config.Dev, GameGlobals.Loading).AwaitAndLog();
                    watch.LogAndRestart("NPCs created");
                }

                // World fully loaded
                // TODO - Does this call add benefits for memory?
                ResourceLoader.ReleaseLoadedData();

                worldRoot.SetActive(true);
                vobRoot.SetActive(true);

                // StationaryLight.InitStationaryLights();

                TeleportPlayerToStart();

                // There are many handlers which listen to this event. If any of these fails, we won't get notified without a try-catch.
                try
                {
                    GlobalEventDispatcher.WorldSceneLoaded.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                SceneManager.UnloadSceneAsync(Constants.SceneLoading);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                fullWatch.Log("Full world loaded");
            }
        }

        /// <summary>
        /// There are three options, where the player can spawn:
        /// 1. We set it inside GameConfiguration.SpawnAtWaypoint (only during first load of the game)
        /// 2. We got the spawn information from a loaded SaveGame from the Hero's VOB
        /// 3. We load the START_* waypoint
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

            // 3.
            var spots = GameData.FreePoints;
            var startPoint = spots.FirstOrDefault(
                go => go.Key.EqualsIgnoreCase("START") || go.Key.EqualsIgnoreCase("START_GOTHIC2")
            );

            if (startPoint.Key.IsNullOrEmpty())
            {
                Debug.LogError("No suitable START_* waypoint found!");
                return;
            }

            TeleportPlayerToStart(startPoint.Value.Position, startPoint.Value.Rotation);
        }

        private void TeleportPlayerToStart(Vector3 position, Quaternion rotation)
        {
            GameContext.InteractionAdapter.TeleportPlayerTo(position, rotation);
            GameContext.InteractionAdapter.UnlockPlayer();
            GameGlobals.Player.ResetSpawn();
        }
    }
}
