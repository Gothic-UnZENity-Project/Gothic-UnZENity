using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using ZenKit;
using Debug = UnityEngine.Debug;

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
        /// 1. Fetch all existing worlds
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
            try
            {
                var worldsToLoad = GameContext.GameVersionAdapter.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;

                // DEBUG - Remove this IF to enforce recreation of cache.
                if (!GameGlobals.Config.Dev.AlwaysRecreateCache && GameGlobals.StaticCache.DoCacheFilesExist(worldsToLoad))
                {
                    var metadata = await GameGlobals.StaticCache.ReadMetadata();
                    if (metadata.Version == Constants.StaticCacheVersion)
                    {
                        Debug.Log("World + Global data is already cached and metadata version matches. Skipping...");
                        GameManager.I.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
                        return;
                    }
                }

                var watch = Stopwatch.StartNew();
                var overallWatch = Stopwatch.StartNew();

                var vobBoundsCache = new VobBoundsCacheCreator();
                var textureArrayCache = new TextureArrayCacheCreator();

                GameGlobals.StaticCache.InitCacheFolder();

                foreach (var worldName in worldsToLoad)
                {
                    Debug.Log($"### PreCaching meshes for world: {worldName}");
                    var world = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version)!;
                    var worldChunkCache = new WorldChunkCacheCreator();
                    var stationaryLightCache = new StationaryLightCacheCreator();

                    await vobBoundsCache.CalculateVobBounds(world.RootObjects);
                    watch.LogAndRestart($"{worldName}: VobBounds calculated.");

                    await textureArrayCache.CalculateTextureArrayInformation(world.Mesh);
                    watch.LogAndRestart($"{worldName}: WorldMesh TextureArray calculated.");

                    await textureArrayCache.CalculateTextureArrayInformation(world.RootObjects);
                    watch.LogAndRestart($"{worldName}: World VOBs TextureArray calculated.");

                    await stationaryLightCache.CalculateStationaryLights(world.RootObjects);
                    watch.LogAndRestart($"{worldName}: Stationary lights calculated.");

                    await worldChunkCache.CalculateWorldChunks(world, stationaryLightCache.StationaryLightBounds);
                    watch.LogAndRestart($"{worldName}: World chunks calculated.");

                    await GameGlobals.StaticCache.SaveWorldCache(worldName, worldChunkCache.MergedChunksByLights, stationaryLightCache.StationaryLightInfos);

                    // DEBUG - Re-enable only when needed.
                    // await GameGlobals.StaticCache.SaveDebugCache(worldName, stationaryLightCache.StationaryLightBounds);

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

                await textureArrayCache.CalculateItemTextureArrayInformation();
                watch.LogAndRestart("Global: Texture array for oCItems calculated.");

                await vobBoundsCache.CalculateVobItemBounds();
                watch.LogAndRestart("VobBounds for oCItems calculated.");

                await GameGlobals.StaticCache.SaveGlobalCache(vobBoundsCache.Bounds, textureArrayCache.TextureArrayInformation);
                watch.LogAndRestart("Saved GlobalCache files.");
                overallWatch.Log("Overall PreCaching done.");

                // Cleanup
                MultiTypeCache.Dispose();

                // Every world of the game is cached successfully. Now let's move on!
                GameManager.I.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }
    }
}
