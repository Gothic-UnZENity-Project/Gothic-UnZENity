using System.Linq;
using GUZ.Core;
using GUZ.Core.Context;
using GUZ.Core.Globals;
using GUZ.XRIT.Components.Vobs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;

namespace GUZ.XRIT
{
    public class XritInteractionAdapter : IInteractionAdapter
    {
        private const string _contextName = "XRIT"; // XR Interaction Toolkit

        public string GetContextName()
        {
            return _contextName;
        }

        public GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default)
        {
            string prefabName;
            string goName;

            if (Constants.SceneMainMenu == scene.name)
            {
                prefabName = $"{_contextName}/Prefabs/VRPlayer/VRPlayer-MainMenu";
                goName = "VRPlayer - XRIT - MainMenu";
            }
            else
            {
                prefabName = $"{_contextName}/Prefabs/VRPlayer/VRPlayer";
                goName = "VRPlayer - XRIT";
            }

            var newPrefab = Resources.Load<GameObject>(prefabName);
            var go = Object.Instantiate(newPrefab, position, rotation);
            go.name = goName;

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            return go;
        }

        public void CreateXRDeviceSimulator()
        {
            var simulator = ResourceLoader.TryGetPrefabObject(PrefabType.XRDeviceSimulator);
            simulator.name = "XRDeviceSimulator - XRIT";
            SceneManager.GetActiveScene().GetRootGameObjects().Append(simulator);
        }

        public void AddClimbingComponent(GameObject go)
        {
            // We will set some default values for collider and grabbing now.
            // Adding it now is easier than putting it on a prefab and updating it at runtime (as grabbing didn't work this way out-of-the-box).
            // e.g. grabComp's colliders aren't recalculated if we have the XRGrabInteractable set in Prefab.
            var grabComp = go.AddComponent<XRGrabInteractable>();
            var rigidbodyComp = go.GetComponent<Rigidbody>();
            var meshColliderComp = go.GetComponentInChildren<MeshCollider>();

            meshColliderComp.convex = true; // We need to set it to overcome Physics.ClosestPoint warnings.
            go.tag = Constants.ClimbableTag;
            rigidbodyComp.isKinematic = true;
            
            // Throws errors and isn't needed as we don't want to move the kinematic ladder when released.
            grabComp.throwOnDetach = false;
            grabComp.trackPosition = false;
            grabComp.trackRotation = false;
            grabComp.selectMode = InteractableSelectMode.Multiple; // With this, we can grab with both hands!
        }

        public void AddItemComponent(GameObject go, bool isLab = false)
        {
            // This will set some default values for collider and grabbing now.
            // Adding it now is easier than putting it on a prefab and updating it at runtime (as grabbing didn't work this way out-of-the-box).
            var grabComp = go.AddComponent<XRGrabInteractable>();

            var colliderComp = go.GetComponent<MeshCollider>();
            colliderComp.convex = true;

            var itemGrabComp = go.AddComponent<XritItemGrabInteractable>();
            itemGrabComp.Rb = go.GetComponent<Rigidbody>();

            // There is no culling in Lab
            if (!isLab)
            {
                grabComp.selectEntered.AddListener(itemGrabComp.SelectEntered);
                grabComp.selectExited.AddListener(itemGrabComp.SelectExited);
            }
        }
    }
}
