using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
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
                // if (GameGlobals.Glt.DoesCacheFileExist(worldName))
                // {
                //     Debug.Log($"{worldName} already cached. Skipping...");
                //     continue;
                // }

                Debug.Log($"### PreCaching meshes for world: {worldName}");
                var worldData = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version);

                var vobsRootGo = new GameObject("Vobs");
                var worldRootGo = new GameObject("World");

                // Build the world and vob meshes, populating the texture arrays.
                // We need to start creating Vobs as we need to calculate world slicing based on amount of lights at a certain space afterwards.
                Debug.Log("### PreCaching static VOB meshes.");
                await new VobCacheManager().CreateForCache(worldData!.RootObjects, GameGlobals.Loading, vobsRootGo);
                // During loading, the texture array gets filled. It's easier for now to simply dispose the data instead
                // of altering its collection with IF-ELSE statements in code.
                // TextureCache.RemoveCachedTextureArrayData();

                Debug.Log("### PreCaching World meshes.");
                await WorldCreator.CreateForCache(worldData, worldRootGo, GameGlobals.Loading);

                await GameGlobals.StaticCache.SaveCache(worldRootGo, vobsRootGo, worldName);

                // Clean up scene memory
                GameGlobals.TextureArray.Dispose();
                Destroy(vobsRootGo);
                Destroy(worldRootGo);
                // We accidentally create morph caches (as we didn't update the AbstractMeshCreator logic and added IF CacheState==true
                MorphMeshCache.Dispose();
                Debug.Log("### DEBUG Saving cache DONE.");


                // DEBUG restore
                // {
                //     var loadRoot = new GameObject("DebugRestore");
                //     loadRoot.transform.position = new(1000, 0, 0);
                //
                //     await GameGlobals.Glt.LoadGlt(loadRoot, worldName);
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
