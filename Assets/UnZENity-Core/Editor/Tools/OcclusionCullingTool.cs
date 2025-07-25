using System;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Caches.StaticCache;
using GUZ.Core.Config;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using GUZ.G1;
using GUZ.G2;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Editor.Tools
{
    /// How to use:
    /// 1. Load the scene for which you want the Occlusion Culling being baked
    /// 2. Run "UnZENity/Occlusion Culling/Load world mesh for G1/G2" in editor
    /// 3. Open "Window/Rendering/Occlusion Culling"
    /// 4. Bake OC data
    /// 5. Save the scene
    /// 6. Put the data you have into the binary repository at https://github.com/Gothic-UnZENity-Project/binary-dependencies/tree/main/UnZENity-Core/Scenes/Worlds
    /// 7. Update your Gothic-UnZENity version (Package Manager --> Update binary-dependencies package)
    /// 8. Test if OC data is used in a normal game
    public class OcclusionCullingTool : EditorWindow
    {
        [MenuItem("UnZENity/Build/Occlusion Culling/Load world mesh for G1", true)]
        private static bool ValidateG1OCLoading()
        {
            // If game is in playmode, disable button.
            return !EditorApplication.isPlaying;
        }

        [MenuItem("UnZENity/Build/Occlusion Culling/Load world mesh for G2", true)]
        private static bool ValidateG2OCLoading()
        {
            // If game is in playmode, disable button.
            return !EditorApplication.isPlaying;
        }

        [MenuItem("UnZENity/Build/Occlusion Culling/Load world mesh for G1", priority = 1000)]
        public static async Task LoadWorldMeshG1()
        {
            await LoadWorldMesh(GameVersion.Gothic1);
        }

        [MenuItem("UnZENity/Build/Occlusion Culling/Load world mesh for G2", priority = 1001)]
        public static async Task LoadWorldMeshG2()
        {
            await LoadWorldMesh(GameVersion.Gothic2);
        }

        private static async Task LoadWorldMesh(GameVersion version)
        {
            // Do not show Window when game is started.
            if (Application.isPlaying)
            {
                return;
            }

            // Prepare configuration needed during execution.
            var config = new ConfigManager();
            config.LoadRootJson();
            ResourceLoader.Init(version == GameVersion.Gothic1 ? config.Root.Gothic1Path : config.Root.Gothic2Path);

            var editorDataProvider = new EditorDataProvider();
            editorDataProvider.Config = new ConfigManager();
            editorDataProvider.Config.SetDeveloperConfig(CreateInstance<DeveloperConfig>());
            editorDataProvider.Config.Dev.SpeedUpLoading = true;
            editorDataProvider.StaticCache = new StaticCacheManager();
            GameGlobals.Instance = editorDataProvider;

            await Execute(version).AwaitAndLog();
        }

        private static async Task Execute(GameVersion version)
        {
            // Needed during Cache creation. (StationaryLightCache is calling worlds from Fire.zen and need to have this information.)
            GameContext.GameVersionAdapter = version == GameVersion.Gothic1 ? new G1Adapter() : new G2Adapter();

            var worldName = SceneManager.GetActiveScene().name;
            var world = ResourceLoader.TryGetWorld(worldName, version)!;
            Logger.LogEditor("DONE - Loading world from ZenKit", LogCat.PreCaching);

            if (world == null)
            {
                throw new ArgumentException($"Current scene >{worldName}< is no gothic world.");
            }

            var stationaryLightCache = new StationaryLightCacheCreator();
            var worldChunkCache = new WorldChunkCacheCreator();

            await stationaryLightCache.CalculateStationaryLights(world.RootObjects, 0).AwaitAndLog();
            Logger.LogEditor("DONE - Loading stationary light data", LogCat.PreCaching);
            await worldChunkCache.CalculateWorldChunks(world, stationaryLightCache.StationaryLightBounds, 0).AwaitAndLog();
            Logger.LogEditor("DONE - Calculating world chunks", LogCat.PreCaching);


            var worldChunkData = new StaticCacheManager.WorldChunkContainer
            {
                OpaqueChunks = worldChunkCache.MergedChunksByLights[TextureCache.TextureArrayTypes.Opaque],
                // We do not use transparent elements for OC data as it would cull even on transparent edges.
                TransparentChunks = new(),
                // We do not use water for OC data
                WaterChunks = new()
            };

            await MeshFactory.CreateWorld(worldChunkData, world.Mesh, null, null, useTextureArray: false).AwaitAndLog();
            Logger.LogEditor("DONE - Loading world mesh", LogCat.PreCaching);
        }

        private void OnGUI()
        {
            // Do not show Window when game is started.
            if (Application.isPlaying)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            GameData.Dispose();
        }
    }
}
