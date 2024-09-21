using System.Threading.Tasks;
using GUZ.Core.Creator;
using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class WorldSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            LoadWorldContentAsync();
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
            
            // 1.
            // Build the world and vob meshes, populating the texture arrays.
            // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterwards.
            if (config.EnableVOBs)
            {
                await VobCreator.CreateAsync(config, GameGlobals.Loading, SaveGameManager.CurrentWorldData.Vobs, Constants.VobsPerFrame);
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

            ResourceLoader.ReleaseLoadedData();
            
            GlobalEventDispatcher.WorldFullyLoaded.Invoke();
        }
    }
}
