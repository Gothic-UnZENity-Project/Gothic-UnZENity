using GUZ.Core.UI.Menus.Adapter.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Adapter
{
    public interface IInteractionAdapter
    {
        string GetContextName();
        float GetFrameRate();
        GameObject CreatePlayerController(Scene scene, Vector3 position = default, Quaternion rotation = default);
        GameObject GetCurrentPlayerController();
        void CreateVRDeviceSimulator();
        void LockPlayerInPlace();
        void UnlockPlayer();
        void TeleportPlayerTo(Vector3 position, Quaternion rotation = default);
        void InitUIInteraction();
        void IntroduceChapter(string chapter, string text, string texture, string wav, int time);
        void UpdateMainMenu(IMenuInstance mainMenu);
    }
}
