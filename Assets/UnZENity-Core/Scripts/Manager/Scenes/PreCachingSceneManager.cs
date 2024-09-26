using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
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
        public static string GLTCacheFilePath { get; private set; }
        public static string GLTCacheFileEnding = "glb";

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

            // We're not allowed to call Application.* from a static field directly. Loading it now.
            GLTCacheFilePath = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/";

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
                if (DoesCacheFileExist(worldName))
                {
                    Debug.Log($"{worldName} already cached. Skipping...");
                    continue;
                }

                Debug.Log($"### PreCaching meshes for world: {worldName}");
                var worldData = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version);

                var vobsRootGo = new GameObject("Vobs");
                var worldRootGo = new GameObject("World");

                Debug.Log("### PreCaching static VOB meshes.");
                await new VobCacheManager().CreateForCache(worldData!.RootObjects, GameGlobals.Loading, vobsRootGo);

                // During loading, the texture array gets filled. It's easier for now to simply dispose the data instead
                // of altering its collection with IF-ELSE statements in code.
                TextureCache.RemoveCachedTextureArrayData();
                CheckForInvalidData(vobsRootGo);

                Debug.Log("### PreCaching World meshes.");
                await WorldCreator.CreateForCache(worldData, worldRootGo, GameGlobals.Loading);
                CheckForInvalidData(worldRootGo);


                await SaveGLT(worldRootGo, vobsRootGo, worldName);

                // Clean up memory
                Destroy(vobsRootGo);
                Destroy(worldRootGo);

                // DEBUG restore
                // {
                //     var loadRoot = new GameObject("TestRestore");
                //     loadRoot.transform.position = new(1000, 0, 0);
                //     await LoadGlt(loadRoot, worldName);
                // }
            }

            // Every world of the game is cached successfully. Now let's move on!
            GameManager.I.LoadScene(Constants.SceneMainMenu, Constants.ScenePreCaching);
        }

        private bool DoesCacheFileExist(string worldName)
        {
            return File.Exists($"{GLTCacheFilePath}/{worldName}.{GLTCacheFileEnding}");
        }

        /// <summary>
        /// We want to store meshes only. We therefore need to check if textures are also included and raise an exception if so.
        /// Textures should always be created via TextureArray. This pre-check is a good performance check if something is wrong with mesh generation in general.
        /// </summary>
        private void CheckForInvalidData(GameObject rootGo)
        {
            var renderers = rootGo.GetComponentsInChildren<MeshRenderer>(true);

            var allowedShaderNames = new string[]
            {
                // TODO - This shader is used for materials used from e.g. HurricaneVR (chest and the inventory rings inside).
                // TODO   We can safely remove this once we load only Gothic data without prefabs at cache time.
                Constants.ShaderLit.name,
                Constants.ShaderWorldLitName
            };

            var nonTextureArrayMeshes = renderers
                .Where(i => i.materials.Length != 1 || !allowedShaderNames.Contains(i.material.shader.name))
                .ToList();

            if (nonTextureArrayMeshes.Any())
            {
                Debug.LogError("Bug: Meshes are loaded which should be inside texture array instead of loading textures now. We don't want to have these textures (*.jpg) inside cache and stop now!");
                foreach (var invalidElement in nonTextureArrayMeshes)
                {
                    Debug.LogError($"Invalid mesh where texture is assigned during caching time: {invalidElement.name}", invalidElement);
                }
                throw new ArgumentException("See Error log");
            }
        }

        private async Task SaveGLT(GameObject worldRootGo, GameObject vobsRootGo, string worldName)
        {
            // Create a new export settings instance
            var exportSettings = new ExportSettings()
            {
                Format = GltfFormat.Binary, // If you want to test something change to GltFormat.Json (e.g. check for extracted texture.jpgs and object structure)
                ComponentMask = ComponentType.Mesh,
                FileConflictResolution = FileConflictResolution.Overwrite
            };
            
            var export = new GameObjectExport(exportSettings);
            
            // A scene is a structure for glTF to structure GameObjects. We will use it to separate VOBs and World meshes.
            export.AddScene(new []{worldRootGo}, "WorldCache");
            export.AddScene(new []{vobsRootGo}, "VobCache");

            try
            {
                Directory.CreateDirectory(GLTCacheFilePath);

                var success = await export.SaveToFileAndDispose($"{GLTCacheFilePath}/{worldName}.{GLTCacheFileEnding}");
                if (!success)
                {
                    Debug.LogError("Something went wrong exporting the World+VOB glTF");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw new Exception("Error exporting glTF", e);
            }
        }
        
        private async Task DebugLoadGLT(GameObject rootGo, string worldName)
        {
            var filePathName = $"{GLTCacheFilePath}/{worldName}.{GLTCacheFileEnding}";

            var gltf = new GltfImport();
            var loading = gltf.LoadFile(filePathName);
            while (!loading.IsCompleted)
            {
                await Task.Yield();
            }

            // Success == Result == true
            if (loading.Result)
            {
                for (var i = 0; i < gltf.SceneCount; i++)
                {
                    var loadingWorld = gltf.InstantiateSceneAsync(rootGo.transform, i);
                    while (!loadingWorld.IsCompleted)
                    {
                        await Task.Yield();
                    }
                }
            }
        }
    }
}
