using System;
using System.Collections;
using System.Linq;
using MyBox;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Debugging
{
    public class SaveGameDebugger : MonoBehaviour
    {
        [Separator("Save")]
        public bool DoSaveGame;

        [Separator("Compare")]
        public bool CompareSaveGames;
        [Range(1, 15)]
        public int SaveSlot1 = 1;
        [Range(1, 15)]
        public int SaveSlot2 = 15;
        public string WorldToCompare = "WORLD.zen";

        private void OnValidate()
        {
            if (DoSaveGame)
            {
                DoSaveGame = false;
                StartCoroutine(ExecuteSave());
            }

            if (CompareSaveGames)
            {
                CompareSaveGames = false;
                CompareSaves();
            }
        }

        /// <summary>
        /// We need to start saving at the end of the frame so that a Screenshot can be taken. Otherwise, it's null.
        /// </summary>
        private IEnumerator ExecuteSave()
        {
            yield return new WaitForEndOfFrame();

            GameGlobals.SaveGame.SaveGame(15, $"UnZENity-TestSave - {DateTime.Now}");

            Debug.Log("DONE");
        }

        private void CompareSaves()
        {
            var save1 = GameGlobals.SaveGame.GetSaveGame(SaveSlot1)!;
            var save2 = GameGlobals.SaveGame.GetSaveGame(SaveSlot2)!;

            var world1 = save1.LoadWorld(WorldToCompare)!;
            var world2 = save2.LoadWorld(WorldToCompare)!;

            // Compare counts
            {
                Debug.Assert(world1.RootObjects.Count == world2.RootObjects.Count, "VOBs inside RootObjects do not match.");
                Debug.Assert(world1.RootObjects.Count(i => i.Type == VirtualObjectType.oCNpc) == world2.RootObjects.Count(i => i.Type == VirtualObjectType.oCNpc),
                    "oCNpc VOBs inside RootObjects do not match.");
            }
        }
    }
}
