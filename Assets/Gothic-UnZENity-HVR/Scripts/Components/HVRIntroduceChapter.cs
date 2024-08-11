using UnityEngine;

namespace GUZ.HVR.Components
{
    /// <summary>
    /// UI logic handler for Daedalus call of IntroduceChapter()
    /// </summary>
    public class HVRIntroduceChapter : MonoBehaviour
    {
        public void DisplayIntroduction(string chapter, string text, string texture, string wav, int time)
        {
            Debug.Log("Introduce called!");
        }

    }
}
