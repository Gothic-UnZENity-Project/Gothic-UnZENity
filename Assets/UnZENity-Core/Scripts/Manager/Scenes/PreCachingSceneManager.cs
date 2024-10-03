using System.Threading.Tasks;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager.Vobs;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Manager.Scenes
{
    public class PreCachingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _loadingArea;

        private static readonly string[] _gothic1Worlds = 
        {
            "World.zen",
            "Freemine.zen",
            "Oldmine.zen",
            "OrcGraveyard.zen",
            "OrcTempel.zen"
        };
        
        private static readonly string[] _gothic2Worlds =
        {
            "NewWorld.zen",
            "OldWorld.zen",
            "AddonWorld.zen",
            "DragonIsland.zen"
        };


        public void Init()
        {
            GameGlobals.Loading.InitLoading(_loadingArea);
            GameContext.InteractionAdapter.TeleportPlayerTo(_loadingArea.transform.position);

#pragma warning disable CS4014 // Do not wait. We want to update player movement (VR) and camera view (progress bar) 
            CreateCaches();
#pragma warning restore CS4014
        }

        /// <summary>
        /// 1. Fetch all existing worlds (done)
        ///
        /// 1.1 Load static VOBs (many elements except like Spot+Startpoint+Iems+[Elements with have no mesh])
        ///   Cache them
        ///
        /// 1.2 Load world mesh of a level
        ///   Sliced by lighting VOBs
        ///   Cache it
        /// </summary>
        private async Task CreateCaches()
        {
            var worldsToLoad = GameContext.GameVersionAdapter.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;

            foreach (var worldName in worldsToLoad)
            {
                // DEBUG Enforce recreation of cache if commented out.
                if (GameGlobals.StaticCache.DoCacheFilesExist(worldName))
                {
                    if (GameGlobals.StaticCache.ReadMetadata(worldName).Version == Constants.StaticCacheVersion)
                    {
                        Debug.Log($"{worldName} already cached and metadata version matches. Skipping...");
                        continue;
                    }
                }

                Debug.Log($"### PreCaching meshes for world: {worldName}");
                var worldData = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version);

                var vobsRootGo = new GameObject("Vobs");
                // vobsRootGo.SetActive(false); // Save some frames as we don't render the created meshes in 1km distance. ;-)
                var worldRootGo = new GameObject("World");
                worldRootGo.SetActive(false); // Save some frames as we don't render the created meshes in 1km distance. ;-)

                // Build the world and vob meshes, populating the texture arrays.

                // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterward.
                Debug.Log("### PreCaching static VOB meshes.");
                await new VobCacheManager()
                    .CreateAsync(GameGlobals.Loading, worldData!.RootObjects, vobsRootGo)
                    .AwaitAndLog();

                Debug.Log("### PreCaching World meshes.");
                await WorldCreator
                    .CreateForCache(worldData, worldRootGo, GameGlobals.Loading)
                    .AwaitAndLog();

                Debug.Log("## Saving caches.");
                await GameGlobals.StaticCache
                    .SaveCache(worldRootGo, vobsRootGo, worldName)
                    .AwaitAndLog();

                // Clean up scene memory (Hint: TextureArray data is removed from within StaticCache.SaveCache itself)
                Destroy(vobsRootGo);
                Destroy(worldRootGo);

                Debug.Log($"### Saving cache for {worldName} done.");


                // DEBUG restore
                // {
                //     var loadRoot = new GameObject("DebugRestore");
                //     loadRoot.transform.position = new(1000, 0, 0);
                //
                //     await GameGlobals.StaticCache.LoadCache(loadRoot, worldName);
                //
                //     Debug.Log("DEBUG Loading done!");
                //     return;
                // }
            }

            // Every world of the game is cached successfully. Now let's move on!
            GameManager.I.LoadScene(Constants.SceneMainMenu, Constants.ScenePreCaching);
        }
    }
}
