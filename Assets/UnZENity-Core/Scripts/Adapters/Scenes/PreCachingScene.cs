using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GUZ.Core.Adapters.UI.LoadingBars;
using GUZ.Core.Domain.StaticCache;
using GUZ.Core.Extensions;
using GUZ.Core.Const;
using GUZ.Core.Core.Logging;
using GUZ.Core.Manager;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.StaticCache;
using GUZ.Core.Util;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Core.Adapters.Scenes
{
    public class PreCachingScene : MonoBehaviour, IScene
    {
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly LoadingService _loadingService;
        [Inject] private readonly StaticCacheService _staticCacheService;
        [Inject] private readonly BootstrapService _bootstrapService;

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

        
        [Inject] private readonly AudioService _audioService;

        
        public void Init()
        {
            GameContext.ContextInteractionService.DisableMenus();
            GameContext.ContextInteractionService.InitUIInteraction();
            GameContext.ContextInteractionService.TeleportPlayerTo(_loadingBarHandler.transform.position);

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
                var worldsToLoad = GameContext.ContextGameVersionService.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;
                
                if (!_configService.Dev.AlwaysRecreateCache && _staticCacheService.DoCacheFilesExist(worldsToLoad))
                {
                    var metadata = await _staticCacheService.ReadMetadata();
                    if (metadata.Version == Constants.StaticCacheVersion)
                    {
                        Logger.Log("World + Global data is already cached and metadata version matches. Skipping...", LogCat.PreCaching);
                        _bootstrapService.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
                        return;
                    }
                }
                
                //
                // Now we (re)create whole cache.
                //
                
                // Sleeper temple music (similar to installation music)
                _audioService.Play("KAT_DAY_STD");
                
                GameContext.ContextInteractionService.DisableMenus();
                _loadingBarHandler.LevelCount = worldsToLoad.Length;
                _loadingService.InitLoading(_loadingBarHandler);

                var watch = Stopwatch.StartNew();
                var overallWatch = Stopwatch.StartNew();

                var vobBoundsCache = new VobBoundsCacheCreatorDomain().Inject(); // FIXME - We need to move it to a Domain object and call it via Service.
                var vobColliderCache = new VobItemColliderCacheCreatorDomain().Inject();
                var textureArrayCache = new TextureArrayCacheCreatorDomain().Inject();

                _staticCacheService.InitCacheFolder();

                for (var worldIndex = 0; worldIndex < worldsToLoad.Length; worldIndex++)
                {
                    var worldName = worldsToLoad[worldIndex];
                        
                    Logger.Log($"### PreCaching meshes for world: {worldName}", LogCat.PreCaching);
                    var world = ResourceLoader.TryGetWorld(worldName, GameContext.ContextGameVersionService.Version)!;
                    var stationaryLightCache = new StationaryLightCacheCreatorDomain().Inject();
                    var worldChunkCache = new WorldChunkCacheCreatorDomain().Inject();

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

                    await _staticCacheService.SaveWorldCache(worldName, worldChunkCache.MergedChunksByLights, stationaryLightCache.StationaryLightInfos);

                    // DEBUG - Re-enable only when needed.
                    // await _staticCacheService.SaveDebugCache(worldName, stationaryLightCache.StationaryLightBounds);

                    // DEBUG restore
                    // {
                    //     var loadRoot = new GameObject("DebugRestore");
                    //     loadRoot.transform.position = new(1000, 0, 0);
                    //
                    //     await _staticCacheService.LoadCache(loadRoot, worldName);
                    //
                    //     Logger.Log("DEBUG Loading done!");
                    //     return;
                    // }
                }

                await textureArrayCache.CalculateItemTextureArrayInformation();
                watch.LogAndRestart("Global: Texture array for oCItems calculated.");

                await vobBoundsCache.CalculateVobItemBounds();
                watch.LogAndRestart("VobBounds for oCItems calculated.");

                await vobColliderCache.CalculateVobItemColliderCache(vobBoundsCache.Bounds);
                watch.LogAndRestart("Collider for oCItems calculated.");

                await _staticCacheService.SaveGlobalCache(vobBoundsCache.Bounds, vobColliderCache.ItemCollider, textureArrayCache.TextureArrayInformation);
                watch.LogAndRestart("Saved GlobalCache files.");
                overallWatch.Log("Overall PreCaching done.");

                // Cleanup
                _multiTypeCacheService.Dispose();

                // Every world of the game is cached successfully. Now let's move on!
                _bootstrapService.LoadScene(Constants.SceneLogo, Constants.ScenePreCaching);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString(), LogCat.PreCaching);
                throw;
            }
            finally
            {
                _loadingService.StopLoading();

                // We need to grant the player always the option to quit the game via menu if something fails.
                GameContext.ContextInteractionService.EnableMenus();
            }
        }
    }
}
