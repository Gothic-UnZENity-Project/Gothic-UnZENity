using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.UI.Menus.LoadingBars;
using GUZ.Core.Util;
using UnityEngine;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager.Scenes
{
    public class PreCachingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField]
        private PreCachingLoadingBarHandler _loadingBarHandler;

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
            GameContext.InteractionAdapter.TeleportPlayerTo(_loadingBarHandler.transform.position);

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
                
                // Sleeper temple music (similar to installation music)
                GameGlobals.Music.Play("KAT_DAY_STD");
                if (!GameGlobals.Config.Dev.AlwaysRecreateCache && GameGlobals.StaticCache.DoCacheFilesExist(worldsToLoad))
                {
                    var metadata = await GameGlobals.StaticCache.ReadMetadata();
                    if (metadata.Version == Constants.StaticCacheVersion)
                    {
                        Logger.Log("World + Global data is already cached and metadata version matches. Skipping...", LogCat.PreCaching);
                        GameManager.I.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
                        return;
                    }
                }
                
                GameContext.InteractionAdapter.DisableMenus();
                _loadingBarHandler.LevelCount = worldsToLoad.Length;
                GameGlobals.Loading.InitLoading(_loadingBarHandler);

                var watch = Stopwatch.StartNew();
                var overallWatch = Stopwatch.StartNew();

                var vobBoundsCache = new VobBoundsCacheCreator();
                var textureArrayCache = new TextureArrayCacheCreator();

                GameGlobals.StaticCache.InitCacheFolder();

                for (var worldIndex = 0; worldIndex < worldsToLoad.Length; worldIndex++)
                {
                    var worldName = worldsToLoad[worldIndex];
                        
                    Logger.Log($"### PreCaching meshes for world: {worldName}", LogCat.PreCaching);
                    var world = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version)!;
                    var stationaryLightCache = new StationaryLightCacheCreator();
                    var worldChunkCache = new WorldChunkCacheCreator();

                    await vobBoundsCache.CalculateVobBounds(world.RootObjects, worldIndex);
                    watch.LogAndRestart($"{worldName}: VobBounds calculated.");

                    await textureArrayCache.CalculateTextureArrayInformation(world.Mesh, worldIndex);
                    watch.LogAndRestart($"{worldName}: WorldMesh TextureArray calculated.");

                    await textureArrayCache.CalculateTextureArrayInformation(world.RootObjects, worldIndex);
                    watch.LogAndRestart($"{worldName}: World VOBs TextureArray calculated.");

                    await stationaryLightCache.CalculateStationaryLights(world.RootObjects, worldIndex);
                    watch.LogAndRestart($"{worldName}: Stationary lights calculated.");

                    await worldChunkCache.CalculateWorldChunks(world, stationaryLightCache.StationaryLightBounds, worldIndex);
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
                    //     Logger.Log("DEBUG Loading done!");
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
                Logger.LogError(e.ToString(), LogCat.PreCaching);
                throw;
            }
            finally
            {
                // We need to grant the player always the option to quit the game via menu if something fails.
                GameContext.InteractionAdapter.EnableMenus();
            }
        }
    }
}
