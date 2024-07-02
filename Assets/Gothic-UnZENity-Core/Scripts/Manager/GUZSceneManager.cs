using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Context;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GuzSceneManager
    {
        public GameObject InteractionManager;


        private static readonly string _generalSceneName = Constants.SceneGeneral;
        private const int _ensureLoadingBarDelayMilliseconds = 5;

        private string _newWorldName;
        private string _startVobAfterLoading;
        private Scene _generalScene;
        private bool _generalSceneLoaded;
        private Scene _currentScene;

        private Vector3 _heroStartPosition;
        private Quaternion _heroStartRotation;

        private bool _debugFreshlyDoneLoading;

        private GameConfiguration _config;
        private LoadingManager _loading;

        public GuzSceneManager(GameConfiguration config, LoadingManager loading, GameObject interactionManagerObject)
        {
            InteractionManager = interactionManagerObject;
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
                if (_config.EnableMainMenu)
                {
                    await LoadMainMenu();
                }
                else if (_config.LoadFromSaveSlot)
                {
                    SaveGameManager.LoadSavedGame(_config.SaveSlotToLoad);

                    await LoadWorld(SaveGameManager.Save.Metadata.World);
                }
                else
                {
                    SaveGameManager.LoadNewGame();
                    await LoadWorld(Constants.SelectedWorld, Constants.SelectedWaypoint);
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
            if (!_debugFreshlyDoneLoading)
            {
                return;
            }

            _debugFreshlyDoneLoading = false;

            // We load NPCs only! if we have a fresh game start.
            // TODO - We need to properly update this logic to reflect world switches for the first time as well (even from save games).
            if (_config.SpawnNPCs && !_config.LoadFromSaveSlot)
            {
                GameData.GothicVm.Call("STARTUP_SUB_OLDCAMP");
            }
        }

        private async Task LoadMainMenu()
        {
            GameGlobals.Textures.LoadLoadingDefaultTextures();
            await LoadNewWorldScene(Constants.SceneMainMenu);
        }

        public async Task LoadWorld(string worldName, string startVob = "")
        {
            // Our scenes are named *.zen - We therefore need to ensure the pattern of world name matches.
            worldName = worldName.ToLower();
            worldName += worldName.EndsWith(".zen") ? "" : ".zen";

            // Reset position before world start to load (and hero position is potentially being loaded from vob.ocNPC.
            _heroStartPosition = Vector3.zero;
            _heroStartRotation = Quaternion.identity;
            _startVobAfterLoading = startVob;

            if (worldName == _newWorldName)
            {
                SetSpawnPoint();
                return;
            }

            _newWorldName = worldName;
            SaveGameManager.ChangeWorld(worldName);

            var watch = Stopwatch.StartNew();

            GameData.Reset();

            await ShowLoadingScene(worldName);
            await LoadNewWorldScene(_newWorldName);
            await WorldCreator.CreateAsync(_loading, _config);
            SetSpawnPoint();

            HideLoadingScene();
            watch.Stop();
            Debug.Log($"Time spent for loading {worldName}: {watch.Elapsed}");

            _debugFreshlyDoneLoading = true;
        }

        private async Task<Scene> LoadNewWorldScene(string worldName)
        {
            var newWorldScene = SceneManager.LoadScene(worldName, new LoadSceneParameters(LoadSceneMode.Additive));

            // Delay for at least one frame to allow the scene to be set active successfully
            // i.e. created GOs will be automatically put to right scene afterwards.
            await Task.Yield();

            // Remove previous scene if it exists
            if (_currentScene.IsValid())
            {
                SceneManager.UnloadSceneAsync(_currentScene);
            }

            _currentScene = newWorldScene;
            return newWorldScene;
        }

        /// <summary>
        /// Create loading scene and wait for a few milliseconds to go on, ensuring loading bar is selectable.
        /// Async: execute in sync, but whole process can be paused for x amount of frames.
        /// </summary>
        private async Task ShowLoadingScene(string worldName = null)
        {
            GameGlobals.Textures.LoadLoadingDefaultTextures();

            _generalScene = SceneManager.GetSceneByName(_generalSceneName);
            if (_generalScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(InteractionManager,
                    SceneManager.GetSceneByName(Constants.SceneBootstrap));
                SceneManager.UnloadSceneAsync(_generalScene);

                GlobalEventDispatcher.GeneralSceneUnloaded.Invoke();
                _generalSceneLoaded = false;
            }

            // Unload main menu scene if it exists
            var mainScene = SceneManager.GetSceneByName(Constants.SceneMainMenu);
            if (mainScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(mainScene);
                GlobalEventDispatcher.MainMenuSceneUnloaded.Invoke();
            }

            SetLoadingTextureForWorld(worldName);

            SceneManager.LoadScene(Constants.SceneLoading, new LoadSceneParameters(LoadSceneMode.Additive));

            // Delay for magic number amount to make sure that bar can be found
            // 1 and 2 caused issues for the 3rd time showing the loading scene in editor
            await Task.Delay(_ensureLoadingBarDelayMilliseconds);
        }

        private void SetLoadingTextureForWorld(string worldName)
        {
            if (worldName == null)
            {
                return;
            }

            var textureString = SaveGameManager.IsNewGame
                ? "LOADING.TGA"
                : $"LOADING_{worldName.Split('.')[0].ToUpper()}.TGA";
            GameGlobals.Textures.SetTexture(textureString, GameGlobals.Textures.GothicLoadingMenuMaterial);
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
                    SceneManager.MoveGameObjectToScene(InteractionManager, _generalScene);

                    var playerGo = GuzContext.InteractionAdapter.CreatePlayerController(scene);

                    TeleportPlayerToSpot(playerGo);
                    GlobalEventDispatcher.GeneralSceneLoaded.Invoke(playerGo);

                    break;
                case Constants.SceneMainMenu:
                    var sphere = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "LoadingSphere");
                    sphere.GetComponent<MeshRenderer>().material = GameGlobals.Textures.LoadingSphereMaterial;
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
            if (scene.name == Constants.SceneLoading && !_generalSceneLoaded)
            {
                _generalScene =
                    SceneManager.LoadScene(_generalSceneName, new LoadSceneParameters(LoadSceneMode.Additive));
                _generalSceneLoaded = true;
            }
        }

        private void SetSpawnPoint()
        {
            // If we currently load world from a save game, we will use the stored hero position which was set during VOB loading.
            if (_heroStartPosition != Vector3.zero && SaveGameManager.IsFirstWorldLoadingFromSaveGame)
            {
                // We only use the Vob location once per save game loading.
                SaveGameManager.IsFirstWorldLoadingFromSaveGame = false;
                return;
            }

            var debugSpawnPoint = _config.SpawnAtWaypoint;
            // DEBUG - Spawn at specifically named point.
            if (debugSpawnPoint.Any())
            {
                var point = WayNetHelper.GetWayNetPoint(debugSpawnPoint);

                if (point != null)
                {
                    SetStart(GameObject.Find(debugSpawnPoint).transform);
                    return;
                }
            }

            var spots = GameObject.FindGameObjectsWithTag(Constants.SpotTag);

            // DEBUG - This _startVobAfterLoading_ is only used as debug method for the menu where we select the vob to spawn to.
            // DEBUG - Normally we would spawn at START(_GOTHIC2) or whatever the loaded save file tells us.
            var startPoint1 = spots.FirstOrDefault(go => go.name.EqualsIgnoreCase(_startVobAfterLoading));
            if (startPoint1 != null)
            {
                SetStart(startPoint1.transform);
                return;
            }

            var startPoint2 = spots.FirstOrDefault(
                go => go.name.EqualsIgnoreCase("START") || go.name.EqualsIgnoreCase("START_GOTHIC2")
            );
            SetStart(startPoint2.transform);
        }

        public void SetStart(Transform start)
        {
            SetStart(start.position, start.rotation);
        }

        public void SetStart(Vector3 startPosition, Quaternion startRotation)
        {
            _heroStartPosition = startPosition;
            _heroStartRotation = startRotation;
        }

        public void TeleportPlayerToSpot(GameObject playerGo)
        {
            GuzContext.InteractionAdapter.SpawnPlayerToSpot(playerGo, _heroStartPosition, _heroStartRotation);
        }
    }
}
