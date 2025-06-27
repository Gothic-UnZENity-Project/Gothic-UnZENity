using System;
using GUZ.Core;
using GUZ.Core.Adapter;
using GUZ.Core.UI.Menus.Adapter.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Flat
{
    public class FlatInteractionAdapter : IInteractionAdapter
    {
        private GameObject _playerController;


        private const string _contextName = "Flat";

        public string GetContextName()
        {
            return _contextName;
        }

        public float GetFrameRate()
        {
            return Application.targetFrameRate;
        }

        public GameObject CreatePlayerController(Scene scene)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.Player);

            go.name = "Player - Flat";

            // During normal gameplay, we need to move the VRPlayer to General scene. Otherwise, it will be created inside
            // world scene and removed whenever we change the world.
            SceneManager.MoveGameObjectToScene(go, scene);

            _playerController = go.GetComponentInChildren<Rigidbody>().gameObject;

            return _playerController;
        }

        public GameObject GetCurrentPlayerController()
        {
            return _playerController.gameObject;
        }

        public void CreateVRDeviceSimulator()
        {
            // NOP
        }

        public void LockPlayerInPlace()
        {
            // Not yet implemented
        }

        public void UnlockPlayer()
        {
            // Not yet implemented
        }

        public void  TeleportPlayerTo(Vector3 position, Quaternion rotation = default)
        {
            _playerController.transform.SetLocalPositionAndRotation(position, rotation);
        }

        public void InitUIInteraction()
        {
            // NOP
        }

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            throw new NotImplementedException();
        }

        public void DisableMenus()
        {
            throw new NotImplementedException();
        }

        public void EnableMenus()
        {
            throw new NotImplementedException();
        }
    }
}
