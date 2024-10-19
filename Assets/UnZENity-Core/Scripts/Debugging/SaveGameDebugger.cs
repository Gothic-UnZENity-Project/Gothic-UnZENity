using UnityEngine;

namespace GUZ.Core.Debugging
{
    public class SaveGameDebugger : MonoBehaviour
    {
        public bool DoSaveGame;

        private void OnValidate()
        {
            if (DoSaveGame)
            {
                DoSaveGame = false;

                GameGlobals.SaveGame.SaveGame(5);
            }
        }
    }
}
