using GUZ.Core.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core._Adapter
{
    public interface IContextInteractionService
    {
        string GetContextName();
        float GetFrameRate();
        void SetupPlayerController(DeveloperConfig developerConfig);
        GameObject GetCurrentPlayerController();
        void LockPlayerInPlace();
        void UnlockPlayer();
        void TeleportPlayerTo(Vector3 position, Quaternion rotation = default);
        void InitUIInteraction();
        void IntroduceChapter(string chapter, string text, string texture, string wav, int time);
        void DisableMenus();
        void EnableMenus();
    }
}
