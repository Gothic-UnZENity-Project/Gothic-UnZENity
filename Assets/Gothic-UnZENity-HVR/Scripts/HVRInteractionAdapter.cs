#if GUZ_HVR_INSTALLED
using System.Linq;
using GUZ.Core.Context;
using GUZ.Core.Globals;
using GVR;
using HurricaneVR.Framework.Core.Player;
using HurricaneVRExtensions.Simulator;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GUZ.HVR
{
    public class HVRInteractionAdapter : IInteractionAdapter
    {
        private const string CONTEXT_NAME = "HVR";

        public string GetContextName()
        {
            return CONTEXT_NAME;
        }

        public GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default)
        {
            var newPrefab = Resources.Load<GameObject>("HVR/Prefabs/VRPlayer");
            var go = Object.Instantiate(newPrefab, position, rotation);
            go.name = "VRPlayer - HVR";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            if (Constants.SceneMainMenu == scene.name)
            {
                var controllerComp = go.GetComponentInChildren<HVRPlayerController>();

                // Disable physics
                controllerComp.Gravity = 0f;
                controllerComp.MaxFallSpeed = 0f;

                // Disable movement
                controllerComp.MoveSpeed = 0f;
                controllerComp.RunSpeed = 0f;

                // Disable rotation
                controllerComp.SmoothTurnSpeed = 0f;
                controllerComp.SnapAmount = 0f;
            }

            return go;
        }

        public void CreateXRDeviceSimulator()
        {
            // As we reference components from HVRPlayer inside HVRSimulator, we need to create the SimulatorGO on the same scene.
            var generalScene = SceneManager.GetSceneByName(Constants.SceneGeneral);
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
            // We assume, that this Component (HVRPlayerManager) is set inside the HVR root for a player rig.
            var playerRig = currentScene.GetRootGameObjects().First(i => i.GetComponentInChildren<HVRPlayerManager>());

            simulatorGo.AddComponent<HVRBodySimulator>().Rig = playerRig;
            simulatorGo.AddComponent<HVRHandsSimulator>().Rig = playerRig;
            simulatorGo.AddComponent<HVRSimulatorControlsGUI>();

            SceneManager.MoveGameObjectToScene(simulatorGo, currentScene);
        }

        public void AddClimbingComponent(GameObject go)
        {
            // Currently nothing to do. Everything's set up inside oCMobLadder.prefab already.
        }

        public void AddItemComponent(GameObject go, bool isLab)
        {
            // Currently nothing to do. Everything's set up inside oCItem.prefab already.
        }
    }
}
#endif
