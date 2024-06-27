using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Context;
using GUZ.Core.Creator;
using GUZ.Core.Debugging;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager
{
    public class GUZSceneManager : SingletonBehaviour<GUZSceneManager>
    {
        public GameObject interactionManager;


        private static readonly string generalSceneName = Constants.SceneGeneral;
        private const int ensureLoadingBarDelayMilliseconds = 5;

        private string newWorldName;
        private string startVobAfterLoading;
        private Scene generalScene;
        private bool generalSceneLoaded;

        private Vector3 heroStartPosition;
        private Quaternion heroStartRotation;

        private bool debugFreshlyDoneLoading;
        
        protected override void Awake()
        {
            base.Awake();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Called once after bootstrapping scene is done.
        /// Then load either menu or a world defined inside DebugSettings.
        /// </summary>
        public async Task LoadStartupScenes()
        {
            try
            {
                if (!FeatureFlags.I.skipMainMenu)
                {
                    await LoadMainMenu();
                }
                else if (!FeatureFlags.I.useSaveSlot)
                {
                    SaveGameManager.LoadNewGame();
                    await LoadWorld(Constants.selectedWorld, Constants.selectedWaypoint);
                }
                else
                {
                    SaveGameManager.LoadSavedGame(FeatureFlags.I.saveSlot);
                    await LoadWorld(Constants.selectedWorld, Constants.selectedWaypoint);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        // Outsourced after async Task LoadStartupScenes() as async makes Debugging way harder
        private void Update()
        {
            if (!debugFreshlyDoneLoading)
                return;
            else
                debugFreshlyDoneLoading = false;

            // We load NPCs only! if we have a fresh game start.
            // TODO - We need to properly update this logic to reflect world switches for the first time as well (even from save games).
            if (FeatureFlags.I.createOcNpcs && !FeatureFlags.I.useSaveSlot)
                GameData.GothicVm.Call("STARTUP_SUB_OLDCAMP");
        }

        private async Task LoadMainMenu()
        {
            TextureManager.I.LoadLoadingDefaultTextures();
            await LoadNewWorldScene(Constants.SceneMainMenu);
            GameData.WorldScene = null;
        }

        public async Task LoadWorld(string worldName, string startVob = "")
        {
            // Our scenes are named *.zen - We therefore need to ensure the pattern of world name matches.
            worldName = worldName.ToLower();
            worldName += worldName.EndsWith(".zen") ? "" : ".zen";

            // Reset position before world start to load (and hero position is potentially being loaded from vob.ocNPC.
            heroStartPosition = Vector3.zero;
            heroStartRotation = Quaternion.identity;
            startVobAfterLoading = startVob;

            if (worldName == newWorldName)
            {
                SetSpawnPoint();
                return;
            }
            
            newWorldName = worldName;

            SaveGameManager.ChangeWorld(worldName);

            var watch = Stopwatch.StartNew();

            GameData.Reset();
            
            await ShowLoadingScene(worldName);
            var newWorldScene = await LoadNewWorldScene(newWorldName);
            await WorldCreator.CreateAsync();

            SetSpawnPoint();

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
            if (GameData.WorldScene.HasValue)
                SceneManager.UnloadSceneAsync(GameData.WorldScene.Value);

            GameData.WorldScene = newWorldScene;
            return newWorldScene;
        }

        /// <summary>
        /// Create loading scene and wait for a few milliseconds to go on, ensuring loading bar is selectable.
        /// Async: execute in sync, but whole process can be paused for x amount of frames.
        /// </summary>
        private async Task ShowLoadingScene(string worldName = null)
        {
            TextureManager.I.LoadLoadingDefaultTextures();

            generalScene = SceneManager.GetSceneByName(generalSceneName);
            if (generalScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(interactionManager, SceneManager.GetSceneByName(Constants.SceneBootstrap));
                SceneManager.UnloadSceneAsync(generalScene);

                GUZEvents.GeneralSceneUnloaded.Invoke();
                generalSceneLoaded = false;
            }
            
            // Unload main menu scene if it exists
            var mainScene = SceneManager.GetSceneByName(Constants.SceneMainMenu);
            if (mainScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(mainScene);
                GUZEvents.MainMenuSceneUnloaded.Invoke();
            }

            SetLoadingTextureForWorld(worldName);

            SceneManager.LoadScene(Constants.SceneLoading, new LoadSceneParameters(LoadSceneMode.Additive));

            // Delay for magic number amount to make sure that bar can be found
            // 1 and 2 caused issues for the 3rd time showing the loading scene in editor
            await Task.Delay(ensureLoadingBarDelayMilliseconds);
        }

        private void SetLoadingTextureForWorld(string worldName)
        {
            if (worldName == null)
                return;

            string textureString = SaveGameManager.IsNewGame ? "LOADING.TGA" : $"LOADING_{worldName.Split('.')[0].ToUpper()}.TGA";
            TextureManager.I.SetTexture(textureString, TextureManager.I.gothicLoadingMenuMaterial);
        }

        private void HideLoadingScene()
        {
            SceneManager.UnloadSceneAsync(Constants.SceneLoading);

            LoadingManager.I.ResetProgress();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case Constants.SceneBootstrap:
                    break;
                case Constants.SceneLoading:
                    GUZEvents.LoadingSceneLoaded.Invoke();
                    break;
                case Constants.SceneGeneral:
                    SceneManager.MoveGameObjectToScene(interactionManager, generalScene);

                    var playerGo = GUZContext.InteractionAdapter.CreatePlayerController(scene);

                    TeleportPlayerToSpot(playerGo);
                    GUZEvents.GeneralSceneLoaded.Invoke(playerGo);

                    break;
                case Constants.SceneMainMenu:
                    var sphere = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "LoadingSphere");
                    sphere.GetComponent<MeshRenderer>().material = TextureManager.I.loadingSphereMaterial;
                    SceneManager.SetActiveScene(scene);

                    GUZEvents.MainMenuSceneLoaded.Invoke();
                    break;
                // any World
                default:
                    SceneManager.SetActiveScene(scene);
                    GUZEvents.WorldSceneLoaded.Invoke();
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

        private void SetSpawnPoint()
        {
            // If we currently load world from a save game, we will use the stored hero position which was set during VOB loading.
            if (heroStartPosition != Vector3.zero && SaveGameManager.IsFirstWorldLoadingFromSaveGame)
            {
                // We only use the Vob location once per save game loading.
                SaveGameManager.IsFirstWorldLoadingFromSaveGame = false;
                return;
            }

            var debugSpawnPoint = FeatureFlags.I.spawnAtSpecificWayNetPoint;
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
            Transform startTransform;

            // DEBUG - This _startVobAfterLoading_ is only used as debug method for the menu where we select the vob to spawn to.
            // DEBUG - Normally we would spawn at START(_GOTHIC2) or whatever the loaded save file tells us.
            var startPoint1 = spots.FirstOrDefault(go => go.name.EqualsIgnoreCase(startVobAfterLoading));
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
            heroStartPosition = startPosition;
            heroStartRotation = startRotation;
        }

        public void TeleportPlayerToSpot(GameObject playerGo)
        {
            GUZContext.InteractionAdapter.SpawnPlayerToSpot(playerGo, heroStartPosition, heroStartRotation);
        }
    }
}
