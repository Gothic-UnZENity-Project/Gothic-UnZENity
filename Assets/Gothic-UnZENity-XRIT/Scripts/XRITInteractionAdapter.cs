using System;
using GUZ.Core.Context;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.XRIT
{
    public class XRITInteractionAdapter : IInteractionAdapter
    {
        public string GetContextName()
        {
            throw new NotImplementedException();
        }

        public GameObject CreatePlayerController(Scene scene)
        {
            throw new NotImplementedException();
        }

        public void AddClimbingComponent(GameObject go)
        {
            throw new NotImplementedException();
        }

        public void AddItemComponent(GameObject go, bool isLab = false)
        {
            throw new NotImplementedException();
        }
    }
}
