#if GUZ_HVR_INSTALLED
using System.Linq;
using GUZ.Core;
using GUZ.Core.Context;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.VR.Components;
using HurricaneVRExtensions.Simulator;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using PrefabType = GUZ.Core.PrefabType;

namespace GUZ.VR
{
    public class VRInteractionAdapter : IInteractionAdapter
    {
        private const string _contextName = "VR";

        public string GetContextName()
        {
            return _contextName;
        }

        public GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.Player, position, rotation);
            var playerController = go.GetComponentInChildren<VRPlayerController>();

            go.name = "Player - VR";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            if (scene.name is Constants.SceneMainMenu or Constants.SceneLoading)
            {
                playerController.SetLockedControls();
            }
            // Normal game
            else
            {
                playerController.SetNormalControls();

                // Add MainMenu entry to the game
                var mainMenuPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.MainMenu);
                playerController.MainMenu = mainMenuPrefab;

                mainMenuPrefab.SetParent(playerController.gameObject, true, true);
                mainMenuPrefab.SetActive(false);
                // TODO: Same as on MainMenu.unity scene. Will be replaced by a proper FollowPlayer component later.
                mainMenuPrefab.transform.localPosition = new(0, 1.5f, 4);
            }

            return playerController.gameObject;
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
            // We assume, that this Component (HVRPlayerManager) is set inside the HVR root for a player rig.
            var playerRig = currentScene.GetRootGameObjects().First(i => i.GetComponentInChildren<VRPlayerManager>());

            simulatorGo.AddComponent<HVRBodySimulator>().Rig = playerRig;
            simulatorGo.AddComponent<HVRHandsSimulator>().Rig = playerRig;
            simulatorGo.AddComponent<HVRSimulatorControlsGUI>();

            SceneManager.MoveGameObjectToScene(simulatorGo, currentScene);
        }

        public void SetTeleportationArea(GameObject teleportationGo)
        {
            /*
             * We need to set the Teleportation area after adding mesh to VOBs. Therefore we call it via event after world was loaded.
             */
            var interactionManager = GameGlobals.Scene.InteractionManager.GetComponent<XRInteractionManager>();
            var teleportationArea = teleportationGo.AddComponent<TeleportationArea>();
            if (interactionManager != null)
            {
                teleportationArea.interactionManager = interactionManager;
            }
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
