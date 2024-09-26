using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// 1. VOBs - First entry, as we slice world chunks based on light VOBs
        /// 2. WayPoints - Needed for spawning NPCs when world is loaded the first time
        /// 3. NPCs - If we load the world for the first time, we leverage their current routine's values
        /// 4. World - Mesh of the world
        /// </summary>
        private async Task LoadWorldContentAsync()
        {
            var config = GameGlobals.Config;

            // 01. Load world and VOB caches.

            var root = new GameObject("Data");
            await GameGlobals.Glt.LoadGlt(root, SaveGameManager.CurrentWorldName);

            // 1.
            // Build the world and vob meshes, populating the texture arrays.
            // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterwards.
            if (config.EnableVOBs)
            {
                // FIXME - Set rootGO from glTF cache values.
                await GameGlobals.Vobs.CreateAsync(GameGlobals.Loading, SaveGameManager.CurrentWorldData.Vobs, null);
            }

            // 2.
            WayNetCreator.Create(config, SaveGameManager.CurrentWorldData);

            // 3.
            // If the world is visited for the first time, then we need to load Npcs via Wld_InsertNpc()
            if (config.EnableNpcs)
            {
                await NpcCreator.CreateAsync(config, GameGlobals.Loading, Constants.NpcsPerFrame);
            }

            // 4.
            if (config.EnableWorldMesh)
            {
                await WorldCreator.CreateAsync(config, GameGlobals.Loading);
            }

            GameGlobals.Sky.InitSky();
            StationaryLight.InitStationaryLights();

            // World fully loaded
            ResourceLoader.ReleaseLoadedData();
            TeleportPlayerToStart();
            SceneManager.UnloadSceneAsync(Constants.SceneLoading);
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
            var debugSpawnAtWayPoint = GameGlobals.Config.SpawnAtWaypoint;

            // If we currently load world from a save game, we will use the stored hero position which was set during VOB loading.
            if (SaveGameManager.IsFirstWorldLoadingFromSaveGame)
            {
                // We only use the Vob location once per save game loading.
                SaveGameManager.IsFirstWorldLoadingFromSaveGame = false;

                if (debugSpawnAtWayPoint.NotNullOrEmpty())
                {
                    // We need to look up WPs and FPs. Therefore this slow check (which is fine, as it's only done for debugging purposes)
                    var debugGo = GameObject.Find(debugSpawnAtWayPoint);

                    if (debugGo != null)
                    {
                        GameContext.InteractionAdapter.TeleportPlayerTo(debugGo.transform.position,
                            debugGo.transform.rotation);
                        return;
                    }
                }
            }
            
            // 2.
            if (GameGlobals.Player.HeroSpawnPosition != default)
            {
                GameContext.InteractionAdapter.TeleportPlayerTo(GameGlobals.Player.HeroSpawnPosition, GameGlobals.Player.HeroSpawnRotation);
                GameGlobals.Player.ResetSpawn();
                return;
            }
            
            // 3.
            var spots = GameObject.FindGameObjectsWithTag(Constants.SpotTag);
            var startPoint = spots.FirstOrDefault(
                go => go.name.EqualsIgnoreCase("START") || go.name.EqualsIgnoreCase("START_GOTHIC2")
            );

            if (startPoint == null)
            {
                Debug.LogError("No suitable START_* waypoint found!");
                return;
            }
            
            GameContext.InteractionAdapter.TeleportPlayerTo(startPoint.transform.position, startPoint.transform.rotation);
            GameContext.InteractionAdapter.UnlockPlayer();

            GlobalEventDispatcher.WorldSceneLoaded.Invoke();
        }
    }
}
