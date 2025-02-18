using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using GUZ.Core.Vob.WayNet;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Wrapper for Initialization topics from NpcManager
    /// </summary>
    public class NpcInitializer2
    {
        private readonly List<(NpcContainer2 npc, string spawnPoint)> _tmpWldInsertNpcData = new();

        private static DaedalusVm Vm => GameData.GothicVm;

        public async Task InitNPCsNewGame(LoadingManager loading)
        {
            NewRunDaedalus();

            loading.SetPhase(LoadingManager.LoadingProgressType.Npc, _tmpWldInsertNpcData.Count);

            await NewAddLazyLoading(loading);
        }

        /// <summary>
        /// Original Gothic uses this function to spawn an NPC instance into the world.
        /// We collect this data only and create NPCs/Monsters in chunks afterward.
        ///
        /// Nevertheless, we need to fill the NpcCache already, as there are the following statements inside Startup.d:
        ///     Wld_InsertNpc(GRD_282_Nek,"");
        ///     var C_NPC nek;
        ///     nek = Hlp_GetNpc(GRD_282_Nek);
        /// --> We need to provide the NpcInstance for Hlp_GetNpc() already during fill up time. Even if we don't have a working mesh etc.
        /// --> Otherwise we get a NPE.
        /// --> We will fill the NpcCache with proper values later.
        /// </summary>
        public void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex)!;
            var npcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);

            var userDataObject = new NpcContainer2
            {
                Instance = npcInstance
            };

            // We reference our object as user data to retrieve it whenever a Daedalus External provides an NpcInstance as input.
            // With this, we can always switch between our UnZENity data and ZenKit data.
            npcInstance.UserData = userDataObject;

            // IMPORTANT!: NpcInstance.UserData stores a weak pointer. i.e. if we do not store the local variable it would get removed.
            MultiTypeCache.NpcCache2.Add(userDataObject);

            // For mesh creation later, we need to store, that there is a new NPC or a duplicate Monster to be spawned.
            _tmpWldInsertNpcData.Add((userDataObject, spawnPoint));
        }

        /// <summary>
        /// Daedalus will walk through the whole Wld_InsertNpc() calls once.
        /// </summary>
        private void NewRunDaedalus()
        {
            // Inside Startup.d, it's always STARTUP_{MAPNAME} and INIT_{MAPNAME}
            // FIXME - Inside Startup.d some Startup_*() functions also call Init_*() some not. How to handle properly? (Force calling it here? Even if done twice?)
            GameData.GothicVm.Call($"STARTUP_{GameGlobals.SaveGame.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}");
        }

        /// <summary>
        /// Now we will crate the NPCs step-by-step to ensure smooth loading screen fps.
        /// </summary>
        private async Task NewAddLazyLoading(LoadingManager loading)
        {
            loading.SetPhase(LoadingManager.LoadingProgressType.Npc, _tmpWldInsertNpcData.Count);

            foreach ((NpcContainer2 npc, string spawnPoint) element in _tmpWldInsertNpcData)
            {
                // Update progress bar and check if we need to wait for next frame now (As some conditions skip -continue- end of loop and would skip check)
                loading.AddProgress();
                await FrameSkipper.TrySkipToNextFrame();

                InitZkInstance(element.npc);
                var go = new GameObject(element.npc.Instance.GetName(NpcNameSlot.Slot0));
                var spawnPoint = GetSpawnPoint(element.npc, element.spawnPoint);

                if (spawnPoint == null)
                {
                    Debug.LogWarning($"Cannot spawn NPC as waypoint ${element.spawnPoint} does not exist.");

                    // FIXME - Destroy GO and NPCInstance (Do not save the instance inside SaveGame as G1 is also removing it?)
                    continue;
                }

                go.transform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);
                GameGlobals.NpcMeshCulling.AddCullingEntry(go);
            }

            // Full loading of NPCs is done.
            loading.AddProgress(LoadingManager.LoadingProgressType.VOB, 1f);
        }

        private void InitZkInstance(NpcContainer2 npc)
        {
            // As we have our back reference between NpcInstance and NpcData, we can now initialize the object on ZenKit side.
            // Lookups like Npc_SetTalentValue() will work now as NpcInstance.UserData() points to our object which stores the information.
            Vm.InitInstance(npc.Instance);
        }

        [CanBeNull]
        private WayNetPoint GetSpawnPoint(NpcContainer2 npc, string spawnPoint)
        {
            // Find the right spawn point based on currently active routine.
            if (npc.Properties.Routines.Any())
            {
                var routineSpawnPointName = npc.Properties.RoutineCurrent.Waypoint;
                return WayNetHelper.GetWayNetPoint(routineSpawnPointName);
            }
            // Fallback: If no routine exists, spawn at the spot which is named inside Wld_insertNpc()
            else
            {
                return WayNetHelper.GetWayNetPoint(spawnPoint);
            }
        }

        /// <summary>
        /// Check if NPC/Monster will spawn inside another and do a circulated free V3 check around the area.
        /// </summary>
        public bool GetFreeAreaAtSpawnPoint(GameObject go, Vector3 spawnPointPos)
        {
            var isPositionFound = false;
            var testRadius = 1f; // ~2x size of normal bounding box of an NPC.
            // Some FP/WP are on a hill. The spawn check will therefore lift the location for a little to not interfere with world mesh collision check.
            var groundControlDifference = new Vector3(0, 1f, 0);
            var initialSpawnPointGroundControl = spawnPointPos + groundControlDifference;

            // Check if the spawn point is free.
            if (!Physics.CheckSphere(initialSpawnPointGroundControl, testRadius / 2))
            {
                go.transform.position = spawnPointPos;
                // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                go.GetComponentInChildren<Rigidbody>().position = spawnPointPos;
                isPositionFound = true;
            }
            // Alternatively let's circle around the spawn point if multiple NPCs spawn onto the same one.
            else
            {
                // Circle around at least x-times.
                // G1: Orc-dogs are in a big crowd. We therefore need to draw a location circle multiple times to spawn them all.
                for (var currentRadius = testRadius; currentRadius <= testRadius * 2; currentRadius += testRadius)
                {
                    for (var angle = 0f; angle < 360f; angle += 36f)
                    {
                        var angleInRadians = angle * Mathf.Deg2Rad;
                        var offsetPoint = new Vector3(Mathf.Cos(angleInRadians) * currentRadius, 0,
                            Mathf.Sin(angleInRadians) * currentRadius);
                        var checkPointGroundControl = initialSpawnPointGroundControl + offsetPoint;

                        // Check if the point is clear (no obstacles)
                        if (!Physics.CheckSphere(checkPointGroundControl, testRadius / 2))
                        {
                            go.transform.position = spawnPointPos + offsetPoint;
                            // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                            go.GetComponentInChildren<Rigidbody>().position =
                                spawnPointPos + offsetPoint;
                            isPositionFound = true;
                            break;
                        }
                    }
                }
            }

            if (!isPositionFound)
            {
                Debug.LogError(
                    $"No suitable spawn point found for NPC >{go.name}<. Circle search didn't find anything!");
                return default;
            }

            // FIXME - Needed?
            // Some data to be used for later.
            // if (initialSpawnPoint.IsFreePoint())
            // {
            //     npc.Go.GetComponent<NpcProperties>().CurrentFreePoint = (FreePoint)initialSpawnPoint;
            // }
            // else
            // {
            //     npc.Go.GetComponent<NpcProperties>().CurrentWayPoint = (WayNet_WayPoint)initialSpawnPoint;
            // }

            return true;
        }
    }
}
