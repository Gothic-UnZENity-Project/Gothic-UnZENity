#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core.Context;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.HVR
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
            throw new NotImplementedException();
        }

        public void CreateXRDeviceSimulator()
        {
            throw new NotImplementedException("This method should never been called on Flat adapter.");
        }

        public void AddClimbingComponent(GameObject go)
        {
            throw new NotImplementedException();
        }

        public void AddItemComponent(GameObject go, bool isLab)
        {
            throw new NotImplementedException();
        }

        public void IntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
