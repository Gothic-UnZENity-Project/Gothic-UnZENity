#if GUZ_HVR_INSTALLED
using System.Linq;
using GUZ.Core;
using GUZ.Core.Adapter;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Player.Menu;
using GUZ.VR.Components;
using GUZ.VR.Components.HVROverrides;
using HurricaneVR.Framework.Core.UI;
using HurricaneVRExtensions.Simulator;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace GUZ.VR.Adapter
{
    public class VRInteractionAdapter : IInteractionAdapter
    {
        private const string _contextName = "VR";

        private VRPlayerController _playerController;

        public VRInteractionAdapter()
        {
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(OnLoadingSceneLoaded);
        }

        public string GetContextName()
        {
            return _contextName;
        }

        private void OnLoadingSceneLoaded()
        {
            // Needed for: World -> Open MainMenu -> Hit "Load"/"New Game"
            _playerController.MainMenu.gameObject.SetActive(false);
        }

        public float GetFrameRate()
        {
            // If we have no VR device attached to our computer, we will get an NPE for activeLoader.
            if (GameGlobals.Config.EnableVRDeviceSimulator)
            {
                return 0;
            }

            var xrDisplay = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            xrDisplay.TryGetDisplayRefreshRate(out var xrRefreshRate);

            return xrRefreshRate;
        }

        public GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.Player, position, rotation);
            _playerController = go.GetComponentInChildren<VRPlayerController>();

            go.name = "Player - VR";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            if (scene.name is Constants.SceneMainMenu or Constants.SceneLoading)
            {
                _playerController.SetLockedControls();
            }
            // Normal game
            else
            {
                _playerController.SetNormalControls();

                // Add MainMenu entry to the game
                var mainMenuPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.MainMenu);
                _playerController.MainMenu = mainMenuPrefab!.GetComponent<MainMenu>();

                mainMenuPrefab.SetParent(_playerController.gameObject, true, true);
                mainMenuPrefab.SetActive(false);
                // TODO: Same as on MainMenu.unity scene. Will be replaced by a proper FollowPlayer component later.
                mainMenuPrefab.transform.localPosition = new(0, 1.5f, 4);
            }

            return _playerController.gameObject;
        }

        public void CreateVRDeviceSimulator()
        {
            if (!GameGlobals.Config.EnableVRDeviceSimulator)
            {
                return;
            }

            // As we reference components from HVRPlayer inside HVRSimulator, we need to create the SimulatorGO on the same scene.
            var generalScene = SceneManager.GetSceneByName(Constants.ScenePlayer);
            var mainMenuScene = SceneManager.GetSceneByName(Constants.SceneMainMenu);
            var labScene = SceneManager.GetSceneByName(Constants.SceneLab);

            Scene currentScene = default;
            foreach (var sceneToCheck in new[] { generalScene, mainMenuScene, labScene })
            {
                if (sceneToCheck.IsValid())
                {
                    currentScene = sceneToCheck;
                    break;
                }
            }

            if (!currentScene.IsValid())
            {
                Debug.LogError("No valid scene for XRDeviceSimulator found. Skipping setup.");
                return;
            }

            var simulatorGo = new GameObject("HVR - XRDeviceSimulator");
            // We assume, that this Component (VRPlayerController) is set 1-level inside the HVR root for a player rig.
            var playerRig = currentScene.GetComponentInChildren<VRPlayerController>()!.transform.parent.gameObject;

            simulatorGo.AddComponent<HVRBodySimulator>().Rig = playerRig;
            simulatorGo.AddComponent<HVRHandsSimulator>().Rig = playerRig;
            simulatorGo.AddComponent<VRSimulatorControlsGUI>();

            SceneManager.MoveGameObjectToScene(simulatorGo, currentScene);
        }

        public void LockPlayerInPlace()
        {
            _playerController.SetLockedControls();
        }

        public void UnlockPlayer()
        {
            _playerController.SetUnlockedControls();
        }

        public void TeleportPlayerTo(Vector3 position, Quaternion rotation = default)
        {
            _playerController.Teleporter.Teleport(position);
            
            // Changing the rotation inside HVRTeleporter didn't work for y-axis. Therefore, setting it now.
            _playerController.transform.rotation = rotation;
        }

        public void InitUIInteraction()
        {
            // Find all ui canvases and add to HVR Input module (To activate red laser pointer for clicking/grabbing)
            // WARNING: As it leverages FindObjectsOfType which looks through all opened scenes trees, this can become quite slow. Execute very carefully!
            var allCanvases = Object.FindObjectsOfType<Canvas>(true);
            HVRInputModule.Instance.UICanvases = allCanvases.ToList();
        }

        // FIXME - Still needed? Test with VR teleportation in game
        public void SetTeleportationArea(GameObject teleportationGo)
        {
            /*
             * We need to set the Teleportation area after adding mesh to VOBs. Therefore we call it via event after world was loaded.
             */
            // var interactionManager = GameGlobals.Scene.InteractionManager.GetComponent<XRInteractionManager>();
            // var teleportationArea = teleportationGo.AddComponent<TeleportationArea>();
            // if (interactionManager != null)
            // {
            //     teleportationArea.interactionManager = interactionManager;
            // }
        }

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            var generalScene = SceneManager.GetSceneByName(Constants.ScenePlayer);
            
            // Check if we already loaded the Chapter change prefab
            GameObject chapterPrefab = generalScene.GetRootGameObjects()
                .FirstOrDefault(i => i.GetComponentInChildren<VRIntroduceChapter>());
            
            if (chapterPrefab == null)
            {
                chapterPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.StoryIntroduceChapter);
                SceneManager.MoveGameObjectToScene(chapterPrefab, generalScene);
            }
            
            chapterPrefab.GetComponent<VRIntroduceChapter>().DisplayIntroduction(chapter, text, texture, wav, time);
        }
    }
}
#endif
