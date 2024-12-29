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
                var watch = Stopwatch.StartNew();
                var overallWatch = Stopwatch.StartNew();

                var worldsToLoad = GameContext.GameVersionAdapter.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;
                var vobBoundsCache = new VobBoundsCacheCreator();
                var textureArrayCache = new TextureArrayCacheCreator();

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
                    var worldChunkCache = new WorldChunkCacheCreator();
                    var worldData = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version)!;
                    // Create each VOB object once to get its bounding box.

                    worldData.Mesh.Materials.ForEach(material => Debug.Log($"Material: {material.Texture}"));

                    textureArrayCache.CalculateTextureArrayInformation(worldData.Mesh);
                    watch.LogAndRestart($"{worldName}: WorldMesh TextureArray calculated.");

                    textureArrayCache.CalculateTextureArrayInformation(worldData.RootObjects);
                    watch.LogAndRestart($"{worldName}: Vob TextureArray calculated.");

                    vobBoundsCache.CalculateVobBounds(worldData!.RootObjects);
                    watch.LogAndRestart($"{worldName}: VobBounds calculated.");


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

                textureArrayCache.CalculateItemTextureArrayInformation();
                vobBoundsCache.CalculateVobtemBounds();
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
