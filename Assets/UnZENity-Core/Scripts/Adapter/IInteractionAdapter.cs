using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Adapter
{
    public interface IInteractionAdapter
    {
        string GetContextName();
        GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default);
        void CreateVRDeviceSimulator();
        void LockPlayerInPlace();
        public void TeleportTo(Vector3 position, Quaternion rotation = default);
        void InitUIInteraction();
        void SetTeleportationArea(GameObject teleportationGo);
        void IntroduceChapter(string chapter, string text, string texture, string wav, int time);
    }
}
