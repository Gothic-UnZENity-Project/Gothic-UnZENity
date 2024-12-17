using System.Threading.Tasks;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Globals;
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

            var vobCache = new VobCacheCreator();

            foreach (var worldName in worldsToLoad)
            {
                // DEBUG - Remove this IF to enforce recreation of cache.
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
                // Create each VOB object once to get its bounding box.


                vobCache.CalculateVobBounds(worldData.RootObjects);


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

            vobCache.CalculateVobtemBounds();

            await GameGlobals.StaticCache.SaveGlobalCache(vobCache.Bounds);

            // Every world of the game is cached successfully. Now let's move on!
            GameManager.I.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
        }
    }
}
