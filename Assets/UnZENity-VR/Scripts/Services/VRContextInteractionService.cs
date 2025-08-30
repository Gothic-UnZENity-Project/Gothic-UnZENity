#if GUZ_HVR_INSTALLED
using System.Linq;
using GUZ.Core;
using GUZ.Core._Adapter;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using GUZ.VR.Adapter.HVROverrides;
using GUZ.VR.Adapter.Marvin;
using GUZ.VR.Adapter.Player;
using HurricaneVR.Framework.Core.UI;
using HurricaneVRExtensions.Simulator;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Adapter
{
    public class VRContextInteractionService : IContextInteractionService
    {
        private const string _contextName = "VR";

        private VRPlayerController _playerController;

        public VRContextInteractionService()
        {
            GlobalEventDispatcher.LoadingSceneLoaded.AddListener(OnLoadingSceneLoaded);
            GlobalEventDispatcher.GothicInisInitialized.AddListener(() =>
            {
                SetRenderDistance(GameGlobals.Config.Gothic.IniVisualRange);
                _playerController.SetNormalControls();
            });
            GlobalEventDispatcher.PlayerPrefUpdated.AddListener(PlayerPrefsUpdated);
        }

        public string GetContextName()
        {
            return _contextName;
        }

        private void OnLoadingSceneLoaded()
        {
            // Needed for: World -> Open MainMenu -> Hit "Load"/"New Game"
            _playerController.MenuHandler.gameObject.SetActive(false);
        }

        public float GetFrameRate()
        {
            // If we have no VR device attached to our computer, we will get an NPE for activeLoader.
            if (GameGlobals.Config.Dev.EnableVRDeviceSimulator || XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                return 0;
            }

            var xrDisplay = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            xrDisplay.TryGetDisplayRefreshRate(out var xrRefreshRate);

            return xrRefreshRate;
        }

        public GameObject CreatePlayerController(Scene scene)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.Player);
            _playerController = go.GetComponentInChildren<VRPlayerController>();

            go.name = "Player - VR";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            _playerController.SetNormalControls(true);

            if (GameGlobals.Config.Dev.ActivateMarvinMode)
                go.GetComponentInChildren<MarvinRootHandler>(true).gameObject.SetActive(true);

            return _playerController.gameObject;
        }
        
        private void PlayerPrefsUpdated(string key, object value)
        {
            if (key == GothicIniConfig.IniKeyVisualRange)
            {
                SetRenderDistance(int.Parse((string)value));
            }
        }

        private void SetRenderDistance(int value)
        {
            // Starting with value=0 (20%) and ending with value=14 (300%)
            _playerController.Camera.GetComponent<Camera>().farClipPlane = GothicIniConfig.IniVisualRangeFactor * (value + 1);
        }

        public GameObject GetCurrentPlayerController()
        {
            return _playerController.gameObject;
        }

        public VRPlayerController GetVRPlayerController()
        {
            return _playerController;
        }

        public VRPlayerInputs GetVRPlayerInputs()
        {
            return _playerController.VrInputs;
        }

        public void CreateVRDeviceSimulator()
        {
            if (!GameGlobals.Config.Dev.EnableVRDeviceSimulator)
            {
                return;
            }

            // As we reference components from HVRPlayer inside HVRSimulator, we need to create the SimulatorGO on the same scene.
            var playerScene = SceneManager.GetSceneByName(Constants.ScenePlayer);
            var mainMenuScene = SceneManager.GetSceneByName(Constants.SceneMainMenu);
            var labScene = SceneManager.GetSceneByName(Constants.SceneLab);

            Scene currentScene = default;
            foreach (var sceneToCheck in new[] { playerScene, mainMenuScene, labScene })
            {
                if (sceneToCheck.IsValid())
                {
                    currentScene = sceneToCheck;
                    break;
                }
            }

            if (!currentScene.IsValid())
            {
                Logger.LogError("No valid scene for XRDeviceSimulator found. Skipping setup.", LogCat.VR);
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

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            var playerScene = SceneManager.GetSceneByName(Constants.ScenePlayer);
            
            // Check if we already loaded the Chapter change prefab
            var chapterPrefab = playerScene.GetRootGameObjects()
                .FirstOrDefault(i => i.GetComponentInChildren<VRIntroduceChapter>());
            
            if (chapterPrefab == null)
            {
                chapterPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.StoryIntroduceChapter);
                SceneManager.MoveGameObjectToScene(chapterPrefab, playerScene);
            }
            
            chapterPrefab.GetComponent<VRIntroduceChapter>().DisplayIntroduction(chapter, text, texture, wav, time);
        }

        public void DisableMenus()
        {
            _playerController.VrInputs.IsMenuButtonEnabled = false;
        }

        public void EnableMenus()
        {
            _playerController.VrInputs.IsMenuButtonEnabled = true;
        }
    }
}
#endif
