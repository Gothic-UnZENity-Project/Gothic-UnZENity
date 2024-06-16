using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Context;
using GUZ.Core.Creator;
using GUZ.Core.Globals;
using GUZ.Core.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GUZSceneManager
    {
        [Obsolete]
        public static GUZSceneManager I;
        
        public GameObject interactionManager;
        
        private static readonly string generalSceneName = Constants.SceneGeneral;
        private const int ensureLoadingBarDelayMilliseconds = 5;

        private string newWorldName;
        private string startVobAfterLoading;
        private Scene generalScene;
        private bool generalSceneLoaded;
        private Scene? currentScene;

        private GameObject startPoint;

        private bool debugFreshlyDoneLoading;

        private GameConfiguration _config;
        private LoadingManager _loading;

        public GUZSceneManager(GameConfiguration config, LoadingManager loading, GameObject interactionManagerObject)
        {
            I = this;
            interactionManager = interactionManagerObject;
            _config = config;
            _loading = loading;
        }
        
        public void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            GlobalEventDispatcher.LevelChangeTriggered.AddListener((world, spawn) => _ = LoadWorld(world, spawn));
        }

        /// <summary>
        /// Called once after bootstrapping scene is done.
        /// Then load either menu or a world defined inside DebugSettings.
        /// </summary>
        public async Task LoadStartupScenes()
        {
            try
            {
                if (!_config.enableMainMenu)
                {
                    await LoadWorld(Constants.selectedWorld, Constants.selectedWaypoint, true);
                }
                else
                {
                    await LoadMainMenu();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        // Outsourced after async Task LoadStartupScenes() as async makes Debugging way harder
        // (Breakpoints won't be caught during exceptions)
        public void Update()
        {
            if (!debugFreshlyDoneLoading)
            {
                return;
            }

            debugFreshlyDoneLoading = false;

            if (_config.spawnOldCampNpcs)
            {
                GameData.GothicVm.Call("STARTUP_SUB_OLDCAMP");
            }
        }

        private async Task LoadMainMenu()
        {
            TextureManager.I.LoadLoadingDefaultTextures();
            await LoadNewWorldScene(Constants.SceneMainMenu);
        }

        public async Task LoadWorld(string worldName, string startVob, bool newGame = false)
        {
            startVobAfterLoading = startVob;
            worldName = worldName.ToLower();
            
            if (worldName == newWorldName)
            {
                SetSpawnPoint(SceneManager.GetSceneByName(newWorldName));
                return;
            }
            
            newWorldName = worldName;

            var watch = Stopwatch.StartNew();

            GameData.Reset();
            
            await ShowLoadingScene(worldName, newGame);
            var newWorldScene = await LoadNewWorldScene(newWorldName);
            await WorldCreator.CreateAsync(_loading, newWorldName, _config);
            SetSpawnPoint(newWorldScene);

            HideLoadingScene();
            watch.Stop();
            Debug.Log($"Time spent for loading {worldName}: {watch.Elapsed}");
            
            debugFreshlyDoneLoading = true;
        }

        private async Task<Scene> LoadNewWorldScene(string worldName)
        {
            var newWorldScene = SceneManager.LoadScene(worldName, new LoadSceneParameters(LoadSceneMode.Additive));

            // Delay for at least one frame to allow the scene to be set active successfully
            // i.e. created GOs will be automatically put to right scene afterwards.
            await Task.Yield();

            // Remove previous scene if it exists
            if (currentScene.HasValue)
            {
                SceneManager.UnloadSceneAsync(currentScene.Value);
            }

            currentScene = newWorldScene;
            return newWorldScene;
        }

        /// <summary>
        /// Create loading scene and wait for a few milliseconds to go on, ensuring loading bar is selectable.
        /// Async: execute in sync, but whole process can be paused for x amount of frames.
        /// </summary>
        private async Task ShowLoadingScene(string worldName = null, bool newGame = false)
        {
            TextureManager.I.LoadLoadingDefaultTextures();

            generalScene = SceneManager.GetSceneByName(generalSceneName);
            if (generalScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(interactionManager, SceneManager.GetSceneByName(Constants.SceneBootstrap));
                SceneManager.UnloadSceneAsync(generalScene);

                GlobalEventDispatcher.GeneralSceneUnloaded.Invoke();
                generalSceneLoaded = false;
            }

            SetLoadingTextureForWorld(worldName, newGame);

            SceneManager.LoadScene(Constants.SceneLoading, new LoadSceneParameters(LoadSceneMode.Additive));

            // Delay for magic number amount to make sure that bar can be found
            // 1 and 2 caused issues for the 3rd time showing the loading scene in editor
            await Task.Delay(ensureLoadingBarDelayMilliseconds);
        }

        private void SetLoadingTextureForWorld(string worldName, bool newGame = false)
        {
            if (worldName == null)
                return;

            string textureString = newGame ? "LOADING.TGA" : $"LOADING_{worldName.Split('.')[0].ToUpper()}.TGA";
            TextureManager.I.SetTexture(textureString, TextureManager.I.gothicLoadingMenuMaterial);
        }

        private void HideLoadingScene()
        {
            SceneManager.UnloadSceneAsync(Constants.SceneLoading);

            _loading.ResetProgress();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case Constants.SceneBootstrap:
                    break;
                case Constants.SceneLoading:
                    GlobalEventDispatcher.LoadingSceneLoaded.Invoke();
                    break;
                case Constants.SceneGeneral:
                    SceneManager.MoveGameObjectToScene(interactionManager, generalScene);

                    var playerGo = GUZContext.InteractionAdapter.CreatePlayerController(scene);

                    TeleportPlayerToSpot(playerGo);
                    GlobalEventDispatcher.GeneralSceneLoaded.Invoke(playerGo);

                    break;
                case Constants.SceneMainMenu:
                    var sphere = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "LoadingSphere");
                    sphere.GetComponent<MeshRenderer>().material = TextureManager.I.loadingSphereMaterial;
                    SceneManager.SetActiveScene(scene);

                    GlobalEventDispatcher.MainMenuSceneLoaded.Invoke();
                    break;
                // any World
                default:
                    SceneManager.SetActiveScene(scene);
                    GlobalEventDispatcher.WorldSceneLoaded.Invoke();
                    break;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == Constants.SceneLoading && !generalSceneLoaded)
            {
                generalScene = SceneManager.LoadScene(generalSceneName, new LoadSceneParameters(LoadSceneMode.Additive));
                generalSceneLoaded = true;
            }
        }

        private void SetSpawnPoint(Scene worldScene)
        {
            var debugSpawnPoint = _config.spawnAtWaypoint;
            // DEBUG - Spawn at specifically named point.
            if (debugSpawnPoint.Any())
            {
                var point = WayNetHelper.GetWayNetPoint(debugSpawnPoint);
                
                if (point != null)
                {
                    startPoint = GameObject.Find(debugSpawnPoint);
                    return;
                }
            }
            
            var spots = GameObject.FindGameObjectsWithTag(Constants.SpotTag);
            
            // DEBUG - This _startVobAfterLoading_ is only used as debug method for the menu where we select the vob to spawn to.
            // DEBUG - Normally we would spawn at START(_GOTHIC2) or whatever the loaded save file tells us.
            var startPoint1 = spots.FirstOrDefault(go => go.name.EqualsIgnoreCase(startVobAfterLoading));
            if (startPoint1 != null)
            {
                startPoint = startPoint1;
                return;
            }

            var startPoint2 = spots.FirstOrDefault(
                go => go.name.EqualsIgnoreCase("START") || go.name.EqualsIgnoreCase("START_GOTHIC2")
            );
            startPoint = startPoint2;
        }

        public void TeleportPlayerToSpot(GameObject playerGo)
        {
            playerGo.transform.SetPositionAndRotation(startPoint.transform.position, startPoint.transform.rotation);
        }
    }
}