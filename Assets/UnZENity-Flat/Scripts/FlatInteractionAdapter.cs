using System;
using GUZ.Core;
using GUZ.Core.Context;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Flat
{
    public class FlatInteractionAdapter : IInteractionAdapter
    {
        private const string CONTEXT_NAME = "Flat";

        public string GetContextName()
        {
            return CONTEXT_NAME;
        }

        public GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.Player, position, rotation);

            go.name = "Player - Flat";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            return go.GetComponentInChildren<Rigidbody>().gameObject;
        }

        public void CreateVRDeviceSimulator()
        {
            throw new NotImplementedException("This method should never been called on Flat adapter.");
        }

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            throw new NotImplementedException();
        }
    }
}
