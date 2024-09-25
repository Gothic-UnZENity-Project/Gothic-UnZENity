using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
using GUZ.Core.Creator;
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
            // FIXME reoder: Freemine after World
            "Freemine.zen",
            "World.zen",
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

        private async Task CreateCaches()
        {
            var levelsToLoad = GameContext.GameVersionAdapter.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;
            
            /*
             * 1. Fetch all existing worlds (done)
             *
             * 1.1 Load static VOBs (many elements except like Spot+Startpoint+Iems+[Elements with have no mesh])
             *   Cache them
             *
             * 1.2 Load world mesh of a level
             *  Sliced by lighting VOBs
             *   Cache it
             */

            foreach (var worldName in levelsToLoad)
            {
                var worldData = ResourceLoader.TryGetWorld(worldName, GameContext.GameVersionAdapter.Version);

                var vobsRootGo = new GameObject("Vobs");
                var worldRootGo = new GameObject("World");

                await new VobCacheManager().CreateForCache(worldData!.RootObjects, GameGlobals.Loading, vobsRootGo);
                await WorldCreator.CreateForCache(worldData, worldRootGo, GameGlobals.Loading);

                await SaveGLTF(worldRootGo, vobsRootGo, worldName);

                // DEBUG restore
                {
                    var loadRoot = new GameObject("TestRestore");
                    loadRoot.transform.position = new(100, 0, 0);
                    await LoadGlt(loadRoot, worldName);
                }

                // DEBUG - Currently pausing after first world creation!
                return;
            }
        }

        private async Task SaveGLTF(GameObject worldRootGo, GameObject vobsRootGo, string worldName)
        {
            var folder = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/glTF";

            // Create a new export settings instance
            var exportSettings = new ExportSettings()
            {
                Format = GltfFormat.Binary,
                ComponentMask = ComponentType.Mesh,
                FileConflictResolution = FileConflictResolution.Overwrite
            };
            
            var export = new GameObjectExport(exportSettings);
            
            // A scene is a structure for glTF to structure GameObjects. We will use it to separate VOBs and World meshes.
            export.AddScene(new []{worldRootGo}, "WorldCache");
            export.AddScene(new []{vobsRootGo}, "VobCache");

            try
            {
                Directory.CreateDirectory(folder);

                var success = await export.SaveToFileAndDispose($"{folder}/{worldName}.glb");
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
        
        private async Task LoadGlt(GameObject rootGo, string worldName)
        {
            var filePathName = $"{Application.persistentDataPath}/Cache/{GameContext.GameVersionAdapter.Version}/glTF/{worldName}.glb";

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
