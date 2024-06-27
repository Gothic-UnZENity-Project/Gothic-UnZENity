using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Context
{
    public interface IInteractionAdapter
    {
        string GetContextName();
        GameObject CreatePlayerController(Scene scene);
        void SpawnPlayerToSpot(GameObject playerGo, Vector3 position, Quaternion rotation);
        void CreateXRDeviceSimulator();
        void AddClimbingComponent(GameObject go);
        void AddItemComponent(GameObject go, bool isLab = false);
    }
}
