using System.Collections;
using UnityEngine;

namespace GUZ.Core.Debugging
{
    public class SaveGameDebugger : MonoBehaviour
    {
        public bool DoSaveGame;

        private void OnValidate()
        {
            if (!DoSaveGame)
            {
                return;
            }
            DoSaveGame = false;
            StartCoroutine(ExecuteSave());
        }

        /// <summary>
        /// We need to start saving at the end of the frame so that a Screenshot can be taken. Otherwise, it's null.
        /// </summary>
        private IEnumerator ExecuteSave()
        {
            yield return new WaitForEndOfFrame();

            GameGlobals.SaveGame.SaveGame(15, "UnZENity-Test Save");

            Debug.Log("DONE");
        }
    }
}
