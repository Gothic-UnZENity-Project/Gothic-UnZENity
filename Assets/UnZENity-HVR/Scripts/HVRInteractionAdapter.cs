#if GUZ_HVR_INSTALLED
using System.Linq;
using GUZ.Core;
using GUZ.Core.Context;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.HVR.Components;
using HurricaneVRExtensions.Simulator;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using PrefabType = GUZ.Core.PrefabType;

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
            // We need to instantiate the Prefab in here as we need to set the default position+rotation. Otherwise HVR will always spawn at 0,0,0.
            var newPrefab = Resources.Load<GameObject>("HVR/Prefabs/Player");
            var go = Object.Instantiate(newPrefab, position, rotation);
            var playerController = go.GetComponentInChildren<GUZHVRPlayerController>();

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

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            var generalScene = SceneManager.GetSceneByName(Constants.SceneGeneral);
            
            // Check if we already loaded the Chapter change prefab
            GameObject chapterPrefab = generalScene.GetRootGameObjects()
                .FirstOrDefault(i => i.GetComponentInChildren<HVRIntroduceChapter>());
            
            if (chapterPrefab == null)
            {
                chapterPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.StoryIntroduceChapter);
                SceneManager.MoveGameObjectToScene(chapterPrefab, generalScene);
            }
            
            chapterPrefab.GetComponent<HVRIntroduceChapter>().DisplayIntroduction(chapter, text, texture, wav, time);
        }
    }
}
#endif
