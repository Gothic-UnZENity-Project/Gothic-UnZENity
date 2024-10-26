using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyBox;
using UnityEngine;
using ZenKit.Util;
using ZenKit.Vobs;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core.Debugging
{
    public class SaveGameDebugger : MonoBehaviour
    {
        [Separator("Save")]
        public bool DoSaveGame;
        [Range(1, 15)]
        public int SaveSlot = 15;


        [Separator("Compare")]
        public bool CompareSaveGames;
        [Range(1, 15)]
        public int SaveSlot1ToCompare = 1;
        [Range(1, 15)]
        public int SaveSlot2ToCompare = 15;
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

            GameGlobals.SaveGame.SaveGame(SaveSlot, $"UnZENity-Test - {DateTime.Now}");

            Debug.Log("DONE");
        }

        private void CompareSaves()
        {
            var save1 = GameGlobals.SaveGame.GetSaveGame(SaveSlot1ToCompare)!;
            var save2 = GameGlobals.SaveGame.GetSaveGame(SaveSlot2ToCompare)!;

            var world1 = save1.LoadWorld(WorldToCompare)!;
            var world2 = save2.LoadWorld(WorldToCompare)!;
            Debug.Log($"Comparing >{WorldToCompare}< in save slots >{save1.Metadata.Title}< and >{save2.Metadata.Title}<.");

            var vobsByTypeA = world1.RootObjects.GroupBy(i => i.Type).ToDictionary(i => i.Key, i => i.ToList());
            var vobsByTypeB = world2.RootObjects.GroupBy(i => i.Type).ToDictionary(i => i.Key, i => i.ToList());

            Debug.Log("Comparing VOB counts");
            {
                var vobsCountA = vobsByTypeA.ToDictionary(i => i.Key, i => i.Value.Count());
                var vobsCountB = vobsByTypeB.ToDictionary(i => i.Key, i => i.Value.Count());

                foreach (var countA in vobsCountA)
                {
                    var countB = vobsCountB.ContainsKey(countA.Key) ? vobsCountB[countA.Key] : 0;

                    if (countA.Value == countB)
                    {
                        Debug.Log($"VOBs of type >{countA.Key}< matches slotA={countA.Value}, slotB={countB}.");

                        ComparePropertiesByType(countA.Key, vobsByTypeA[countA.Key], vobsByTypeB[countA.Key]);
                    }
                    else
                    {
                        Debug.LogError($"VOBs of type >{countA.Key}< does not match slotA={countA.Value}, slotB={countB}.");
                    }
                }

                // Check if there are any VobTypes which aren't in SaveGame1, but SaveGame2
                vobsCountB
                    .Where(i => !vobsCountA.Keys.Contains(i.Key))
                    .ToList()
                    .ForEach(i => Debug.LogError($"VOBs of type >{i.Key}< are missing in slotB."));
            }


            return;

            Debug.Log("--------------------");
            Debug.Log("Compare close NPCs (excluding Monster)");
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

                    var distance = Vector3.Distance(npcA.Position, npcB.Position);
                    Debug.Assert(Vector3.Distance(npcA.Position, npcB.Position) < 5f, $"NPC {npcA.Name} position isn't similar with >{distance}<.");

                    Debug.Assert(npcA.Lp == npcB.Lp, $"NPC {npcA.Name} Lp does not match.");
                    Debug.Assert(npcA.Level == npcB.Level, $"NPC {npcA.Name} level does not match.");
                    Debug.Assert(npcA.Xp == npcB.Xp, $"NPC {npcA.Name} Xp does not match.");
                    Debug.Assert(npcA.XpNextLevel == npcB.XpNextLevel, $"NPC {npcA.Name} XpNextLevel does not match.");
                    Debug.Assert(npcA.GetMission(0) == npcB.GetMission(0), $"NPC {npcA.Name} Mission(0) does not match.");
                    Debug.Assert(npcA.GetAttribute(2) == npcB.GetAttribute(2), $"NPC {npcA.Name} Attribute(2) does not match.");
                    Debug.Assert(npcA.Guild == npcB.Guild, $"NPC {npcA.Name} Guild does not match.");
                    Debug.Assert(npcA.GuildTrue == npcB.GuildTrue, $"NPC {npcA.Name} GuildTrue does not match.");
                }
            }

            Debug.Log("--------------------");
            Debug.Log("Compare far away NPCs (excluding Monster)");
            {

            }

            Debug.Log("--------------------");
            Debug.Log("Comparing oCItem VOBs");
            {
                var vobsA = world1.RootObjects.Where(i => i.Type == VirtualObjectType.oCItem).ToList();
                var vobsB = world2.RootObjects.Where(i => i.Type == VirtualObjectType.oCItem).ToList();

                Debug.Assert(vobsA.Count == vobsB.Count, $"oCItem VOBs inside RootObjects do not match - slotA={vobsA.Count}, slotB={vobsB.Count}.");

                for (var i = 0; i < vobsB.Count; i++)
                {
                    var vobA = vobsA[i];
                    var vobB = vobsB[i];

                    Debug.Assert(vobA.Position == vobB.Position, $"VOB {vobA.Name} position does not match.");
                    Debug.Assert(vobA.Rotation == vobB.Rotation, $"VOB {vobA.Name} rotation does not match.");
                    // Debug.Assert(vob.Scale == vob2.Scale, $"VOB {vob.Name} scale does not match.");
                }
            }

            Debug.Log("--------------------");
            Debug.Log("Comparing static VOBs (excluding NPCs+Monsters+Items)");
            {
                var vobsA = world1.RootObjects.Where(i => i.Type != VirtualObjectType.oCNpc && i.Type != VirtualObjectType.oCItem).ToList();
                var vobsB = world2.RootObjects.Where(i => i.Type != VirtualObjectType.oCNpc && i.Type != VirtualObjectType.oCItem).ToList();

                Debug.Assert(vobsA.Count == vobsB.Count, $"static VOBs inside RootObjects do not match - slotA={vobsA.Count}, slotB={vobsB.Count}.");

                for (var i = 0; i < vobsB.Count; i++)
                {
                    var vobA = vobsA[i];
                    var vobB = vobsB[i];

                    Debug.Assert(vobA.Position == vobB.Position, $"VOB {vobA.Name} position does not match.");
                    Debug.Assert(vobA.Rotation == vobB.Rotation, $"VOB {vobA.Name} rotation does not match.");
                }
            }
        }


        // Properties which we won't compare.
        private Dictionary<string, List<string>> _elementsToIgnore = new (){
            {
               typeof(VirtualObject).FullName!, new()
               {
                   nameof(VirtualObject.Id) // Includes the Index of current VM. Different for G1 and UnZENity.
               }
            }
        };

        private void ComparePropertiesByType(VirtualObjectType type, List<IVirtualObject> slotA, List<IVirtualObject> slotB)
        {
            for (var i = 0; i < slotA.Count(); i++)
            {
                // We loop through all the types from top to bottom.
                // We basically check all Properties except the ones we specifically exclude or handle manually.
                var properties = slotA[i].GetType().GetProperties();
                foreach (var property in properties)
                {
                    var declaringType = property.DeclaringType!.ToString();
                    var propertyName = property.Name;

                    if (_elementsToIgnore.TryGetValue(declaringType, out var typeToIgnore))
                    {
                        if (typeToIgnore.Contains(propertyName))
                        {
                            continue;
                        }
                    }

                    var valueA = property.GetValue(slotA[i]);
                    var valueB = property.GetValue(slotB[i]);

                    switch (property.GetMethod.ReturnParameter!.ToString().Trim())
                    {
                        case "ZenKit.Util.Matrix3x3":
                            var rotationA = (Matrix3x3)valueA;
                            var rotationB = (Matrix3x3)valueB;

                            // Identity comparison doesn't work. Therefore, we use a string comparison. (Please don't judge ;-D )
                            var vobAMatrix =
                                $"{rotationA.M11}, {rotationA.M12}, {rotationA.M13}, {rotationA.M21}, {rotationA.M22}, {rotationA.M23}, {rotationA.M31}, {rotationA.M32}, {rotationA.M33}";
                            var vobBMatrix =
                                $"{rotationB.M11}, {rotationB.M12}, {rotationB.M13}, {rotationB.M21}, {rotationB.M22}, {rotationB.M23}, {rotationB.M31}, {rotationB.M32}, {rotationB.M33}";
                            Debug.Assert(vobAMatrix == vobBMatrix, $"VOB property >{property.Name}< of type >{nameof(Matrix3x3)}< does not match: slotA={vobAMatrix}, slotB={vobBMatrix}");

                            continue;
                    }


                    Debug.Assert(valueA.Equals(valueB), $"{slotA[i].Name}: Property {property.DeclaringType}.{property.Name} does not match.");
                }

                continue;
                switch (type)
                {
                    case VirtualObjectType.oCMobDoor:
                        CompareDoor((IDoor)slotA[i], (IDoor)slotB[i]);
                        break;
                    case VirtualObjectType.oCMobSwitch:
                        CompareSwitch((ISwitch)slotA[i], (ISwitch)slotB[i]);
                        break;
                    case VirtualObjectType.zCVobStair:
                        CompareStair((IStair)slotA[i], (IStair)slotB[i]);
                        break;
                    case VirtualObjectType.zCZoneVobFarPlane:
                        CompareFarPlane((IZoneFarPlane)slotA[i], (IZoneFarPlane)slotB[i]);
                        break;
                    case VirtualObjectType.zCZoneVobFarPlaneDefault:
                        CompareFarPlaneDefault((IZoneFarPlaneDefault)slotA[i], (IZoneFarPlaneDefault)slotB[i]);
                        break;
                    case VirtualObjectType.oCTriggerScript:
                        CompareScriptTrigger((ITriggerScript)slotA[i], (ITriggerScript)slotB[i]);
                        break;
                    case VirtualObjectType.zCMover:
                        CompareMover((IMover)slotA[i], (IMover)slotB[i]);
                        break;
                    case VirtualObjectType.oCMobLadder:
                        CompareLadder((ILadder)slotA[i], (ILadder)slotB[i]);
                        break;
                    case VirtualObjectType.zCVobScreenFX:
                        CompareScreenFX((IVirtualObject)slotA[i], (IVirtualObject)slotB[i]);
                        break;
                    case VirtualObjectType.oCZoneMusic:
                        CompareMusic((IZoneMusic)slotA[i], (IZoneMusic)slotB[i]);
                        break;
                    case VirtualObjectType.oCZoneMusicDefault:
                        CompareMusicDefault((IZoneMusicDefault)slotA[i], (IZoneMusicDefault)slotB[i]);
                        break;
                    case VirtualObjectType.oCMobBed:
                        CompareBed((IBed)slotA[i], (IBed)slotB[i]);
                        break;
                    case VirtualObjectType.oCCSTrigger:
                        CompareCSTrigger((ITrigger)slotA[i], (ITrigger)slotB[i]);
                        break;
                    case VirtualObjectType.zCZoneZFog:
                        CompareFog((IZoneFog)slotA[i], (IZoneFog)slotB[i]);
                        break;
                    case VirtualObjectType.zCZoneZFogDefault:
                        CompareFogDefault((IZoneFogDefault)slotA[i], (IZoneFogDefault)slotB[i]);
                        break;
                    case VirtualObjectType.oCTriggerChangeLevel:
                        CompareLevelTrigger((ITriggerChangeLevel)slotA[i], (ITriggerChangeLevel)slotB[i]);
                        break;
                    case VirtualObjectType.zCVobStartpoint:
                        CompareStartpoint((IStartPoint)slotA[i], (IStartPoint)slotB[i]);
                        break;
                    case VirtualObjectType.oCMOB:
                        CompareMob((IMovableObject)slotA[i], (IMovableObject)slotB[i]);
                        break;
                    case VirtualObjectType.zCTriggerList:
                        CompareTriggerList((ITriggerList)slotA[i], (ITriggerList)slotB[i]);
                        break;
                    default:
                        Debug.LogError($"Comparison of properties for VOB type >{type}< not yet implemented.");
                        return; // Log error only once.
                }
            }
        }

        private void CompareDoor(IDoor vobA, IDoor vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareSwitch(ISwitch vobA, ISwitch vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareStair(IStair vobA, IStair vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareFarPlane(IZoneFarPlane vobA, IZoneFarPlane vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareFarPlaneDefault(IZoneFarPlaneDefault vobA, IZoneFarPlaneDefault vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareScriptTrigger(ITriggerScript vobA, ITriggerScript vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareMover(IMover vobA, IMover vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareLadder(ILadder vobA, ILadder vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareScreenFX(IVirtualObject vobA, IVirtualObject vobB)
        {
            CompareDefaultVob(vobA, vobB);

            // FIXME check proper type!
        }

        private void CompareMusic(IZoneMusic vobA, IZoneMusic vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareMusicDefault(IZoneMusicDefault vobA, IZoneMusicDefault vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareBed(IBed vobA, IBed vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareCSTrigger(ITrigger vobA, ITrigger vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareFog(IZoneFog vobA, IZoneFog vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareFogDefault(IZoneFogDefault vobA, IZoneFogDefault vobB)
        {
            CompareDefaultVob(vobA, vobB);

        }

        private void CompareLevelTrigger(IVirtualObject vobA, IVirtualObject vobB)
        {
            CompareDefaultVob(vobA, vobB);

            // FIXME check proper type!
        }

        private void CompareStartpoint(IStartPoint vobA, IStartPoint vobB)
        {
            CompareDefaultVob(vobA, vobB);

            // Nothing else to do
        }




        private void CompareMob(IMovableObject vobA, IMovableObject vobB)
        {


        }

        private void CompareTriggerList(ITriggerList vobA, ITriggerList vobB)
        {
            CompareDefaultVob(vobA, vobB);

            // FIXME - Compare TriggerList values
        }

        private void CompareDefaultVob(IVirtualObject vobA, IVirtualObject vobB)
        {
            Debug.Assert(vobA.Position == vobB.Position, $"VOB {vobA.Name} position of type >{vobA.Type}< does not match: slotA={vobA.Position}, slotB={vobB.Position}");

            // Identity comparison doesn't work. Therefore, we use a string comparison. (Please don't judge ;-D )
            var vobAMatrix =
                $"{vobA.Rotation.M11}, {vobA.Rotation.M12}, {vobA.Rotation.M13}, {vobA.Rotation.M21}, {vobA.Rotation.M22}, {vobA.Rotation.M23}, {vobA.Rotation.M31}, {vobA.Rotation.M32}, {vobA.Rotation.M33}";
            var vobBMatrix =
                $"{vobB.Rotation.M11}, {vobB.Rotation.M12}, {vobB.Rotation.M13}, {vobB.Rotation.M21}, {vobB.Rotation.M22}, {vobB.Rotation.M23}, {vobB.Rotation.M31}, {vobB.Rotation.M32}, {vobB.Rotation.M33}";
            Debug.Assert(vobAMatrix == vobBMatrix, $"VOB {vobA.Name} rotation of type >{vobA.Type}< does not match: slotA={vobAMatrix}, slotB={vobBMatrix}");

            // FIXME - add more
        }
    }
}
