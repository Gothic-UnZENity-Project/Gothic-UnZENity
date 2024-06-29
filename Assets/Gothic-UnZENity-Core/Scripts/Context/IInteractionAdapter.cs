using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Context
{
    public interface IInteractionAdapter
    {
        string GetContextName();
        GameObject CreatePlayerController(Scene scene);
        void CreateXRDeviceSimulator();
        void AddClimbingComponent(GameObject go);
        void AddItemComponent(GameObject go, bool isLab = false);
    }
}
