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

            GameGlobals.SaveGame.SaveGame(15, $"UnZENity-Test - {DateTime.Now}");

            Debug.Log("DONE");
        }

        private void CompareSaves()
        {
            var save1 = GameGlobals.SaveGame.GetSaveGame(SaveSlot1)!;
            var save2 = GameGlobals.SaveGame.GetSaveGame(SaveSlot2)!;

            var world1 = save1.LoadWorld(WorldToCompare)!;
            var world2 = save2.LoadWorld(WorldToCompare)!;

            Debug.Log($"Comparing >{WorldToCompare}< in save slots >{save1.Metadata.Title}< and >{save2.Metadata.Title}<.");

            Debug.Log("Comparing VOB counts");
            {
                var vobsCountA = world1.RootObjects.Count(i => i.Type != VirtualObjectType.oCNpc);
                var vobsCountB = world2.RootObjects.Count;
                Debug.Log($"Found VOBs (excluding oCNPCs): saveA={vobsCountA}, saveB={vobsCountB}");
                Debug.Assert(vobsCountA == vobsCountB, "VOBs (excluding oCNPCs) inside RootObjects do not match.");

                var npcCountA = world1.RootObjects.Count(i => i.Type == VirtualObjectType.oCNpc);
                var npcCountB = world2.RootObjects.Count(i => i.Type == VirtualObjectType.oCNpc);
                Debug.Log($"Found NPCs: save1={npcCountA}, save2={npcCountB}");
                Debug.Assert(npcCountA == npcCountB, "oCNpc VOBs inside RootObjects do not match in size.");
            }

            Debug.Log("Compare NPCs (excluding Monster)");
            {
                var npcsA = world1.RootObjects.Where(i => i.Type == VirtualObjectType.oCNpc).Select(i => (ZenKit.Vobs.Npc)i).ToList();
                var npcsB = world2.RootObjects.Where(i => i.Type == VirtualObjectType.oCNpc).Select(i => (ZenKit.Vobs.Npc)i).ToList();

                foreach (var npcA in npcsA)
                {
                    // Skip monsters but not if it's the hero
                    if (npcA.Id <= 0 && !npcA.Player)
                    {
                        continue;
                    }

                    var npcB = npcsB.FirstOrDefault(i => i.Name == npcA.Name);

                    if (npcB == null)
                    {
                        Debug.LogError($"NPC {npcA.Name} not found in saveB.");
                        continue;
                    }

                    Debug.Assert(Math.Abs(npcA.Position.LengthSquared() - npcB.Position.LengthSquared()) < 1f, $"NPC {npcA.Name} position does not match by 1f squared.");
                    // Debug.Assert(npcA.Rotation == npcB.Rotation, $"NPC {npcA.Name} rotation does not match.");
                }
            }

            return;

            Debug.Log("Comparing VOBs (excluding NPCs+Monsters)");
            {
                var vobsA = world1.RootObjects.Where(i => i.Type != VirtualObjectType.oCNpc).ToList();
                var vobsB = world2.RootObjects.Where(i => i.Type != VirtualObjectType.oCNpc).ToList();

                Debug.Assert(vobsA.Count == vobsB.Count, "VOBs inside RootObjects do not match.");

                for (var i = 0; i < vobsB.Count; i++)
                {
                    var vobA = vobsA[i];
                    var vobB = vobsB[i];

                    Debug.Assert(vobA.Position == vobB.Position, $"VOB {vobA.Name} position does not match.");
                    Debug.Assert(vobA.Rotation == vobB.Rotation, $"VOB {vobA.Name} rotation does not match.");
                    // Debug.Assert(vob.Scale == vob2.Scale, $"VOB {vob.Name} scale does not match.");
                }
            }
        }
    }
}
