using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using GUZ.Core.Vob.WayNet;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Util.Logger;
using Object = UnityEngine.Object;
using WayPoint = GUZ.Core.Vob.WayNet.WayPoint;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Wrapper for Initialization topics from NpcManager
    /// </summary>
    public class NpcInitializer2
    {
        public GameObject RootGo;
        private readonly List<(NpcContainer2 npc, string spawnPoint)> _tmpWldInsertNpcData = new();

        private static DaedalusVm Vm => GameData.GothicVm;

        public async Task InitNpcsNewGame(LoadingManager loading)
        {
            NewRunDaedalus();
            await NewAddLazyLoading(loading);
        }

        public async Task InitNpcsSaveGame(LoadingManager loading)
        {
            var saveGameNpcs = GameGlobals.SaveGame.CurrentWorldData.Npcs;

            foreach (var vobNpc in saveGameNpcs)
            {
                // Update the progress bar and check if we need to wait for the next frame now (As some conditions skip -continue- end of loop and would skip check)
                loading.AddProgress();
                await FrameSkipper.TrySkipToNextFrame();

                var npcContainer = AllocZkInstance(vobNpc);
                SaveGameAddLazyLoadingAnywhere(npcContainer, vobNpc.ScriptWaypoint);
            }
        }

        public void InitNpcVobSaveGame(ZenKit.Vobs.Npc vobNpc)
        {
            var npcContainer = AllocZkInstance(vobNpc);
            SaveGameAddLazyLoadingNearby(npcContainer, vobNpc);
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
            var userDataObject = AllocZkInstance(npcInstanceIndex);

            // For mesh creation later, we need to store that there is a new NPC or a duplicate Monster to be spawned.
            _tmpWldInsertNpcData.Add((userDataObject, spawnPoint));
        }

        private NpcContainer2 AllocZkInstance(ZenKit.Vobs.Npc vobNpc)
        {
            var symbol = GameData.GothicVm.GetSymbolByName(vobNpc.Name);
            var userDataObject = AllocZkInstance(symbol.Index);
            userDataObject.Vob = vobNpc;

            return userDataObject;
        }

        private NpcContainer2 AllocZkInstance(int npcInstanceIndex)
        {
            var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex)!;
            var npcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);

            var userDataObject = new NpcContainer2
            {
                Instance = npcInstance,
                Props = new(),
                Vob = new()
            };

            // We reference our object as user data to retrieve it whenever a Daedalus External provides an NpcInstance as input.
            // With this, we can always switch between our UnZENity data and ZenKit data.
            npcInstance.UserData = userDataObject;

            // IMPORTANT!: NpcInstance.UserData stores a weak pointer. i.e. if we do not store the local variable it would get removed.
            MultiTypeCache.NpcCache2.Add(userDataObject);

            return userDataObject;
        }

        /// <summary>
        /// Daedalus will walk through the whole Wld_InsertNpc() calls once.
        /// </summary>
        private void NewRunDaedalus()
        {
            // We need to set self=... --> Otherwise we get an NPE C_NPC.id/.name is not in NULL object
            Vm.GlobalSelf = Vm.GlobalHero;

            // Inside Startup.d, it's always STARTUP_{MAPNAME} and INIT_{MAPNAME}
            // FIXME - Inside Startup.d some Startup_*() functions also call Init_*() some not. How to handle properly? (Force calling it here? Even if done twice?)
            GameData.GothicVm.Call($"STARTUP_{GameGlobals.SaveGame.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}");
        }

        /// <summary>
        /// Now we will create the NPCs step-by-step to ensure smooth loading screen fps.
        /// </summary>
        private async Task NewAddLazyLoading(LoadingManager loading)
        {
            loading.SetPhase(LoadingManager.LoadingProgressType.Npc, _tmpWldInsertNpcData.Count);

            foreach ((NpcContainer2 npc, string spawnPoint) element in _tmpWldInsertNpcData)
            {
                // Update the progress bar and check if we need to wait for the next frame now (As some conditions skip -continue- end of loop and would skip check)
                loading.AddProgress();
                await FrameSkipper.TrySkipToNextFrame();

                var go = InitLazyLoadNpc(element.npc);

                var spawnPoint = GetSpawnPoint(element.npc, element.spawnPoint);
                if (spawnPoint == null)
                {
                    Logger.LogWarning($"Cannot spawn NPC as waypoint ${element.spawnPoint} does not exist.", LogCat.Npc);

                    // FIXME - Destroy GO and NPCInstance (Do not save the instance inside SaveGame as G1 is also removing it?)
                    continue;
                }

                if (spawnPoint.IsFreePoint())
                {
                    element.npc.Props.CurrentFreePoint = (FreePoint)spawnPoint;
                }
                else
                {
                    element.npc.Props.CurrentWayPoint = (WayPoint)spawnPoint;
                }

                go.transform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);
                GameGlobals.NpcMeshCulling.AddCullingEntry(go);
            }

            // Full loading of NPCs is done.
            loading.AddProgress(LoadingManager.LoadingProgressType.VOB, 1f);
        }

        /// <summary>
        /// Initialize an NPC which is close to our hero in a save game.
        /// </summary>
        private void SaveGameAddLazyLoadingNearby(NpcContainer2 npc, ZenKit.Vobs.Npc npcVob)
        {
            var go = InitLazyLoadNpc(npc);

            go.transform.SetPositionAndRotation(npcVob.Position.ToUnityVector(), npcVob.Rotation.ToUnityQuaternion());
            GameGlobals.NpcMeshCulling.AddCullingEntry(go);
        }

        /// <summary>
        /// Basically the same logic as SaveGameAddLazyLoadingNearby() but we use the Routine's WP to get the position from.
        /// </summary>
        private void SaveGameAddLazyLoadingAnywhere(NpcContainer2 npc, string fallbackWayPoint)
        {
            var go = InitLazyLoadNpc(npc);

            var spawnPoint = GetSpawnPoint(npc, fallbackWayPoint);

            // When loading a save game from G1, e.g. TPL_1401_GorNaKosh's WP is wrong. His WP only exists in Old Mine,
            // but he is also named inside STARTUP_SUB_PSICAMP() which is on world.zen. Simply removing them for now.
            if (spawnPoint == null)
            {
                Logger.LogWarning($"Cannot spawn NPC {npc.Instance.GetName(NpcNameSlot.Slot0)} as waypoint " +
                                  $"{npc.Props.RoutineCurrent?.Waypoint}/{fallbackWayPoint} does not exist.", LogCat.Npc);
                Object.Destroy(go);
                return;
            }
            if (spawnPoint.IsFreePoint())
                npc.Props.CurrentFreePoint = (FreePoint)spawnPoint;
            else
                npc.Props.CurrentWayPoint = (WayPoint)spawnPoint;
            go.transform.SetPositionAndRotation(spawnPoint.Position, spawnPoint.Rotation);

            GameGlobals.NpcMeshCulling.AddCullingEntry(go);
        }

        /// <summary>
        /// InitZkInstance and create a GameObject for the NPC to be loaded later.
        /// </summary>
        public GameObject InitLazyLoadNpc(NpcContainer2 npc)
        {
            InitZkInstance(npc);
            var go = new GameObject($"{npc.Instance.GetName(NpcNameSlot.Slot0)} ({npc.Instance.Id})");
            go.SetParent(RootGo);

            var loader = go.AddComponent<NpcLoader2>();
            loader.Npc = npc.Instance;

            return go;
        }

        private void InitZkInstance(NpcContainer2 npc)
        {
            // As we have our back reference between NpcInstance and NpcData, we can now initialize the object on ZenKit side.
            // Lookups like Npc_SetTalentValue() will work now as NpcInstance.UserData() points to our object which stores the information.
            Vm.InitInstance(npc.Instance);

            // We need to load routines to set the SpawnPoint correctly later.
            GameGlobals.Npcs.ExchangeRoutine(npc.Instance, npc.Instance.DailyRoutine);
        }

        public void InitNpc(NpcInstance npcInstance, GameObject lazyLoadGo)
        {
            var npcData = npcInstance.GetUserData2();
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc, parent: lazyLoadGo)!;
            var props = npcData.Props;

            npcData.Props.Dialogs = GameData.Dialogs.Instances
                .Where(dialog => dialog.Npc == npcInstance.Index)
                .OrderByDescending(dialog => dialog.Important)
                .ToList();

            // We set the root of Prefab as the new Root object. LazyLoading Root-GO isn't needed for anything, but it's name anymore.
            newNpc.name = "Root";
            npcData.Go = newNpc;

            lazyLoadGo.transform.GetPositionAndRotation(out var lazyPos, out var lazyRot);

            var finalSpawnPos = GetFreeAreaAtSpawnPoint(lazyPos);

            var mdhName = string.IsNullOrEmpty(props.MdhNameOverlay)
                ? props.MdhNameBase
                : props.MdhNameOverlay;
            MeshFactory.CreateNpc(newNpc.name, props.MdmName, mdhName, props.BodyData,
                finalSpawnPos, lazyRot, lazyLoadGo, newNpc);

            // We don't need specific locations of initial LazyLoading GO anymore.
            lazyLoadGo.transform.SetPositionAndRotation(default, default);

            foreach (var equippedItem in props.EquippedItems)
            {
                MeshFactory.CreateNpcWeapon(newNpc, equippedItem, (VmGothicEnums.ItemFlags)equippedItem.MainFlag,
                    (VmGothicEnums.ItemFlags)equippedItem.Flags);
            }
        }

        [CanBeNull]
        private WayNetPoint GetSpawnPoint(NpcContainer2 npc, string fallbackSpawnPoint)
        {
            // Find the right spawn point based on the currently active routine.
            if (npc.Props.RoutineCurrent != null)
            {
                var routineSpawnPointName = npc.Props.RoutineCurrent.Waypoint;
                var wp = WayNetHelper.GetWayNetPoint(routineSpawnPointName);

                // Some Routines have a misspelled WP name. (e.g. Graham at 8am [..]OUSIDE[...] - >T< missing)
                // We will therefore do a fallback to the previous routine.
                if (wp == null)
                    return WayNetHelper.GetWayNetPoint(npc.Props.RoutinePrevious.Waypoint);
                else
                    return wp;
            }
            // Fallback: If no routine exists, spawn at the spot which is named inside Wld_insertNpc()
            else
            {
                return WayNetHelper.GetWayNetPoint(fallbackSpawnPoint);
            }
        }

        /// <summary>
        /// Check if NPC/Monster will spawn inside another and do a circulated free V3 check around the area.
        /// </summary>
        private Vector3 GetFreeAreaAtSpawnPoint(Vector3 positionToScan)
        {
            var isPositionFound = false;
            var testRadius = 1f; // ~2x size of normal bounding box of an NPC.
            // Some FP/WP are on a hill. The spawn check will therefore lift the location for a little to not interfere with world mesh collision check.
            var groundControlDifference = new Vector3(0, 1f, 0);
            var initialSpawnPointGroundControl = positionToScan + groundControlDifference;

            // Check if the spawn point is free.
            if (!Physics.CheckSphere(initialSpawnPointGroundControl, testRadius / 2))
            {
                return positionToScan;
            }

            // Alternatively let's circle around the spawn point if multiple NPCs spawn onto the same one.
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
                        return positionToScan + offsetPoint;
                    }
                }
            }

            Logger.LogError(
                $"No suitable spawn point found for NPC at >{positionToScan}<. Circle search didn't find anything!", LogCat.Npc);
            return default;
        }
    }
}
