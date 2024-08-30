using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Context
{
    public interface IInteractionAdapter
    {
        string GetContextName();
        GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default);
        void CreateVRDeviceSimulator();
        void AddClimbingComponent(GameObject go);
        void AddItemComponent(GameObject go, bool isLab = false);
        void IntroduceChapter(string chapter, string text, string texture, string wav, int time);
    }
}
