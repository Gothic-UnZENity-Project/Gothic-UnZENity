using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Export;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
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
             * Fetch all existing worlds (done)
             * Load world mesh of a level
             *  Sliced by lighting VOBs
             * Cache it
             *
             * Load static VOBs (man elements except Spot+Startpoint+Iems+[Elements with have no mesh]
             * Cache them
             */
            
            foreach (var level in levelsToLoad)
            {
                var worldData = ResourceLoader.TryGetWorld(level, GameContext.GameVersionAdapter.Version);
                var rootGo = new GameObject("World");
                
                await WorldCreator.CreateMesh(worldData, rootGo, GameGlobals.Loading);

                await SaveGlt(rootGo);

                // DEBUG
                {
                    var loadRoot = new GameObject("TestRestore");
                    loadRoot.transform.position = new(10, 10, 0);
                    await LoadGlt(loadRoot, "test.gltf");
                }

                return;
            }
        }
        
        private async Task SaveGlt(GameObject worldRootGo)
        {
            var path = Application.persistentDataPath + "/test.gltf";
            
            
            // Create a new export settings instance
            var exportSettings = new ExportSettings()
            {
                Format = GltfFormat.Json, // FIXME - Move to Binary to save space later!
                ComponentMask = ComponentType.Mesh
            };
            
            var export = new GameObjectExport(exportSettings);
            
            // A scene is a structure for glTF to structure GameObjects. We will use it to separate VOBs and World meshes.
            export.AddScene(new []{worldRootGo}, "WorldMesh");

            var success = await export.SaveToFileAndDispose(path);

            if (!success)
            {
                Debug.LogError("Something went wrong exporting the World+VOB glTF");
            }
        }
        
        private async Task LoadGlt(GameObject rootGo, string path)
        {
            var gltfComp = rootGo.AddComponent<GltfAsset>();
                
            gltfComp.Url = Application.persistentDataPath + "/" + path;

            while (!gltfComp.IsDone)
            {
                await Task.Yield();
            }
            await Task.Yield();

            var gameObjects = new List<GameObject>();
            GetAllChildGameObjects(rootGo.transform.GetChild(0).gameObject, gameObjects);

            // We need to re-add MeshCollider as it isn't cached by glTF
            gameObjects.ForEach(go => go.AddComponent<MeshCollider>());
        }

        private void GetAllChildGameObjects(GameObject root, List<GameObject> returnGameObjects)
        {
            var currentGOs = root.GetAllDirectChildren();
            returnGameObjects.AddRange(currentGOs);

            foreach (var go in currentGOs)
            {
                GetAllChildGameObjects(go, returnGameObjects);
            }
        }
    }
}
