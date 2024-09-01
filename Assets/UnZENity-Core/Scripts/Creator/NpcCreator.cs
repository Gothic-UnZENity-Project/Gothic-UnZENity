using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core.Vob.WayNet;
using JetBrains.Annotations;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Object = UnityEngine.Object;
using WayNet_WayPoint = GUZ.Core.Vob.WayNet.WayPoint;

namespace GUZ.Core.Creator
{
    public static class NpcCreator
    {
        private static GameObject _npcRootGo;
        private static DaedalusVm Vm => GameData.GothicVm;

        // Hint - If this scale ratio isn't looking well, feel free to change it.
        private const float _fatnessScale = 0.1f;

        private static readonly List<(int npcInstance, string spawnPoint)> _tmpWldInsertNpcData = new();

        static NpcCreator()
        {
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(PostWorldLoaded);
        }

        /// <summary>
        /// If the current world is visited for the first time, we call Wld_InsertNpc() to "spawn" them for the first time.
        /// </summary>
        public static async Task CreateAsync(GameConfiguration config, LoadingManager loading, int npcsPerFrame)
        {
            // We load NPCs only! if we enter the world for the first time (e.g. when having a fresh game start).
            // If we loaded the data from a save game or previous visit in this game session, we have our NPCs already loaded via Vobs + SaveGame state.
            if (!SaveGameManager.IsWorldLoadedForTheFirstTime)
            {
                return;
            }

            // Final debug check if we really want to load NPCs.
            if (!config.EnableNpcs)
            {
                return;
            }

            // Inside Startup.d, it's always STARTUP_{MAPNAME} and INIT_{MAPNAME}
            // FIXME - Inside Startup.d some Startup_*() functions also call Init_*() some not. How to handle properly? (Force calling it here? Even if done twice?)
            GameData.GothicVm.Call($"STARTUP_{SaveGameManager.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}");

            // Daedalus will walk through the whole Wld_InsertNpc() calls once.
            // Afterwards we will crate the NPCs step-by-step to ensure smooth loading screen fps.
            await InitializeNpcs(loading, npcsPerFrame);
        }

        private static GameObject GetRootGo()
        {
            // GO need to be created after world is loaded. Otherwise we will spawn NPCs inside Bootstrap.unity
            if (_npcRootGo != null)
            {
                return _npcRootGo;
            }

            _npcRootGo = new GameObject("NPCs");

            return _npcRootGo;
        }

        private static NpcProperties GetProperties(NpcInstance npc)
        {
            return LookupCache.NpcCache[npc.Index].properties;
        }

        private static GameObject GetNpcGo(NpcInstance npcInstance)
        {
            return GetProperties(npcInstance).Go;
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
        public static void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            // We allocate memory for the NpcInstance only once per symbolIndex (via AllocInstance<> in ZenKit).
            // Monsters can be spawned multiple times, but they will be ignored the second time.
            if (!LookupCache.NpcCache.ContainsKey(npcInstanceIndex))
            {
                var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex);
                var npcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);

                LookupCache.NpcCache.Add(npcInstanceIndex, (instance: npcInstance, properties: null));
            }

            // Nevertheless, for mesh creation later, we need to store, that there is a new NPC or a duplicate Monster to be spawned.
            _tmpWldInsertNpcData.Add((npcInstanceIndex, spawnPoint));
        }

        private static async Task InitializeNpcs(LoadingManager loading, int npcsPerFrame)
        {
            var createdCount = 0;
            var totalNpcs = _tmpWldInsertNpcData.Count;

            foreach (var npcData in _tmpWldInsertNpcData)
            {
                // Update progress bar and check if we need to wait for next frame.
                loading.AddProgress(LoadingManager.LoadingProgressType.VOb, 1f / totalNpcs);
                if (++createdCount % npcsPerFrame == 0)
                {
                    await Task.Yield(); // Wait for the next frame
                }

                if (WayNetHelper.GetWayNetPoint(npcData.spawnPoint) is null)
                {
                    Debug.LogWarning($"Cannot spawn NPC as waypoint ${npcData.spawnPoint} does not exist.");
                    continue;
                }

                var newNpc = InitializeNpc(npcData.npcInstance);
                if (newNpc == null)
                {
                    continue;
                }

                SetSpawnPoint(newNpc, npcData.spawnPoint);
                GameGlobals.NpcMeshCulling.AddCullingEntry(newNpc);
            }

            // Full loading of NPCs is done.
            loading.AddProgress(LoadingManager.LoadingProgressType.VOb, 1f);
        }

        /// <summary>
        /// Initialize NPCs and spawn them on the scene.
        /// We also need to ensure, that the NpcInstance from ZenKit has called AllocInstance<> and InitInstance<> once per instanceIndex!
        ///
        /// There are three possibilities to call this method:
        /// 1. No NpcCache entry is set
        ///     - Then we need to AllocInstance<> and InitInstance<>
        ///     - Called whenever NPCs are fetched from a save game
        /// 2. NPCCache entry is set, but no Properties component
        ///     - Then we need to InitInstance<> only
        ///     - Called whenever NPCs are pre-allocated via Wld_InsertNpc()
        /// 3. Both NPCCache entry and Properties component are set
        ///     - Then Alloc+Init was called already. We now need to copy the Ext*() data into this object
        ///       (e.g. mdmName from Mdl_SetVisualBody() call from the first monster's InitInstance<> call)
        ///     - Called whenever monsters are spawned multiple times (won't affect NPCs, they're always singletons inside Daedalus usage)
        ///
        /// Once one of these options is executed, we will go on with creating the meshes itself.
        /// </summary>
        [CanBeNull]
        public static GameObject InitializeNpc(int npcInstanceIndex)
        {
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);

            // 1. NPCs/Monsters which are loaded from a save game (not from Wld_InsertNpc)
            // There is no pre-allocated NpcInstance inside cache
            // Therefore call AllocInstance<> now for the first time
            if (!LookupCache.NpcCache.TryGetValue(npcInstanceIndex, out var cachedValue))
            {
                var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex);
                var npcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);

                cachedValue = (instance: npcInstance, properties: null);
                LookupCache.NpcCache.Add(npcInstanceIndex, cachedValue);
            }

            // 2. NPCs/Monsters which are spawned the first time
            if (cachedValue.properties == null)
            {
                // IMPORTANT: When calling InitInstance(), we will trigger Daedalus to call us via Externals and fill up data.
                // At that point we need to have our properties component set inside our lookup to fill the data properly.
                cachedValue.properties = newNpc.GetComponent<NpcProperties>();
                cachedValue.properties.NpcInstance = cachedValue.instance;
                LookupCache.NpcCache[npcInstanceIndex] =
                    cachedValue; // Tuples are structs. We therefore need to update the whole struct instead of a single property only.
                Vm.InitInstance(cachedValue.instance);

                cachedValue.properties.Dialogs = GameData.Dialogs.Instances
                    .Where(dialog => dialog.Npc == cachedValue.instance.Index)
                    .OrderByDescending(dialog => dialog.Important)
                    .ToList();
            }
            // 3. Monsters which are spawned more than once
            else
            {
                var origNpc = LookupCache.NpcCache[npcInstanceIndex];
                var origProps = origNpc.properties.GetComponent<NpcProperties>();
                // Clone Properties as they're required from the first instance and fetched via e.g. Mdl_SetVisualBody().
                // As we won't call it multiple times, we will only copy the data but not reinvoke it on ZenKit.
                cachedValue.properties.Copy(origProps);
            }

            // Hint: If we filter out NPCs to spawn, we will never get any Monster as they have no Ids set. Except default: 0.
            if (GameGlobals.Config.SpawnNpcInstances.Value.Any() &&
                !GameGlobals.Config.SpawnNpcInstances.Value.Contains(cachedValue.instance.Id))
            {
                LookupCache.NpcCache.Remove(cachedValue.instance.Index);
                Object.Destroy(newNpc);
                return null;
            }

            newNpc.name = $"{cachedValue.instance.GetName(NpcNameSlot.Slot0)} ({cachedValue.instance.Id})";

            var mdhName = string.IsNullOrEmpty(cachedValue.properties.OverlayMdhName)
                ? cachedValue.properties.BaseMdhName
                : cachedValue.properties.OverlayMdhName;
            MeshFactory.CreateNpc(newNpc.name, cachedValue.properties.MdmName, mdhName, cachedValue.properties.BodyData,
                newNpc, GetRootGo());

            foreach (var equippedItem in cachedValue.properties.EquippedItems)
            {
                MeshFactory.CreateNpcWeapon(newNpc, equippedItem, (VmGothicEnums.ItemFlags)equippedItem.MainFlag,
                    (VmGothicEnums.ItemFlags)equippedItem.Flags);
            }

            var npcRoutine = cachedValue.instance.DailyRoutine;
            NpcHelper.ExchangeRoutine(newNpc, cachedValue.instance, npcRoutine);


            newNpc.TryGetComponent<Routine>(out var routine);

            if (routine.CurrentRoutine != null)

                // As per the original game we don't spawn the NPC if the WayNet point doesn't exist.
                if (WayNetHelper.GetWayNetPoint(routine.CurrentRoutine.Waypoint) == null)
                {
                    LookupCache.NpcCache.Remove(cachedValue.instance.Index);
                    Object.Destroy(newNpc);
                    return null;
                }


            // As they're loaded asynchronously, we need to disable every NPC/Monster during loading.
            // We will enable them via Culling once everything is loaded.
            // Otherwise, they'll fall through the world as the world mesh is loaded last. ;-)
            newNpc.SetActive(false);

            return newNpc;
        }

        /// <summary>
        /// The startpoint to create NPCs isn't necessarily the spawnpoint mentioned here.
        /// It can also be the currently active routine point to walk to.
        /// We therefore execute the daily routines to collect current location and use this as spawn location.
        /// </summary>
        private static void SetSpawnPoint(GameObject npcGo, string spawnPoint)
        {
            WayNetPoint initialSpawnPoint;

            // Find the right spawn point based on currently active routine.
            if (npcGo.GetComponent<Routine>().Routines.Any())
            {
                var routineSpawnPointName = npcGo.GetComponent<Routine>().CurrentRoutine.Waypoint;
                initialSpawnPoint = WayNetHelper.GetWayNetPoint(routineSpawnPointName);

                // Fallback: No WP found? Try one more time with the previous (most likely "earlier") routine waypoint.
                if (initialSpawnPoint == null)
                {
                    routineSpawnPointName = npcGo.GetComponent<Routine>().GetPreviousRoutine().Waypoint;
                    initialSpawnPoint = WayNetHelper.GetWayNetPoint(routineSpawnPointName);
                }
            }
            // Fallback: If no routine exists, spawn at the spot which is named inside Wld_insertNpc()
            else
            {
                initialSpawnPoint = WayNetHelper.GetWayNetPoint(spawnPoint);
            }

            if (initialSpawnPoint == null)
            {
                Debug.LogWarning($"spawnPoint={spawnPoint} couldn't be found.");
                return;
            }

            var isPositionFound = false;
            var testRadius = 1f; // ~2x size of normal bounding box of an NPC.
            // Some FP/WP are on a hill. The spawn check will therefore lift the location for a little to not interfere with world mesh collision check.
            var groundControlDifference = new Vector3(0, 1f, 0);
            var initialSpawnPointGroundControl = initialSpawnPoint.Position + groundControlDifference;

            // Check if the spawn point is free.
            if (!Physics.CheckSphere(initialSpawnPointGroundControl, testRadius / 2))
            {
                npcGo.transform.position = initialSpawnPoint.Position;
                // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                npcGo.GetComponentInChildren<Rigidbody>().position = initialSpawnPoint.Position;
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
                            npcGo.transform.position = initialSpawnPoint.Position + offsetPoint;
                            // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                            npcGo.GetComponentInChildren<Rigidbody>().position =
                                initialSpawnPoint.Position + offsetPoint;
                            isPositionFound = true;
                            break;
                        }
                    }
                }
            }

            if (!isPositionFound)
            {
                Debug.LogError(
                    $"No suitable spawn point found for NPC >{npcGo.name}<. Circle search didn't find anything!");
                return;
            }

            // Some data to be used for later.
            if (initialSpawnPoint.IsFreePoint())
            {
                npcGo.GetComponent<NpcProperties>().CurrentFreePoint = (FreePoint)initialSpawnPoint;
            }
            else
            {
                npcGo.GetComponent<NpcProperties>().CurrentWayPoint = (WayNet_WayPoint)initialSpawnPoint;
            }
        }

        public static void SetSpawnPoint(GameObject npcGo, Vector3 position, Quaternion rotation)
        {
            npcGo.transform.SetPositionAndRotation(position, rotation);
        }

        private static void PostWorldLoaded(GameObject playerGo)
        {
            // FIXME - We need to activate physics (kinetic=false) and routines now. (After world mesh is loaded and player sees game for the first frame)

            _tmpWldInsertNpcData.ClearAndReleaseMemory();
        }

        public static void ExtTaMin(NpcInstance npcInstance, int startH, int startM, int stopH, int stopM, int action,
            string waypoint)
        {
            var npc = GetNpcGo(npcInstance);

            RoutineData routine = new()
            {
                StartH = startH,
                StartM = startM,
                NormalizedStart = startH % 24 * 60 + startM,
                StopH = stopH,
                StopM = stopM,
                NormalizedEnd = stopH % 24 * 60 + stopM,
                Action = action,
                Waypoint = waypoint
            };

            npc.GetComponent<Routine>().Routines.Add(routine);

            // Add element if key not yet exists.
            GameData.NpcRoutines.TryAdd(npcInstance.Index, new List<RoutineData>());
            GameData.NpcRoutines[npcInstance.Index].Add(routine);
        }

        public static void ExtMdlSetVisual(NpcInstance npc, string visual)
        {
            var props = GetProperties(npc);
            props.BaseMdsName = visual;
        }

        public static void ExtApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            var props = GetProperties(npc);
            props.OverlayMdsName = overlayName;
        }

        public static void ExtNpcSetTalentSkill(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            // FIXME - TBD.
            // FIXME - In OpenGothic it adds MDS overlays based on skill level.
        }

        public static void ExtSetVisualBody(VmGothicExternals.ExtSetVisualBodyData data)
        {
            var props = GetProperties(data.Npc);

            props.BodyData = data;

            if (data.Armor >= 0)
            {
                var armorData = VmInstanceManager.TryGetItemData(data.Armor);
                props.EquippedItems.Add(VmInstanceManager.TryGetItemData(data.Armor));
                props.MdmName = armorData.VisualChange;
            }
            else
            {
                props.MdmName = data.Body;
            }
        }

        public static void ExtMdlSetModelScale(NpcInstance npc, Vector3 scale)
        {
            var npcGo = GetNpcGo(npc);

            // FIXME - If fatness is applied before, we reset it here. We need to do proper Vector multiplication here.
            npcGo.transform.localScale = scale;
        }

        public static void ExtSetModelFatness(NpcInstance npc, float fatness)
        {
            var npcGo = GetNpcGo(npc);
            var oldScale = npcGo.transform.localScale;
            var bonusFat = fatness * _fatnessScale;

            npcGo.transform.localScale = new Vector3(oldScale.x + bonusFat, oldScale.y, oldScale.z + bonusFat);
        }

        public static NpcInstance ExtHlpGetNpc(int instanceId)
        {
            if (!LookupCache.NpcCache.TryGetValue(instanceId, out var npcData))
            {
                var instanceName = GameData.GothicVm.GetSymbolByIndex(instanceId).Name;
                Debug.LogError(
                    $"Couldn't find NPC {instanceId} inside cache. Please ensure {instanceName}'s NPC.id is added inside GameConfiguration.");
                return null;
            }


            return npcData.instance;
        }

        public static int ExtHlpGetInstanceId(DaedalusInstance instance)
        {
            if (instance == null)
            {
                return -1;
            }

            return instance.Index;
        }

        public static void ExtNpcPerceptionEnable(NpcInstance npc, VmGothicEnums.PerceptionType perception,
            int function)
        {
            var props = GetProperties(npc);
            props.Perceptions[perception] = function;
        }

        public static void ExtNpcPerceptionDisable(NpcInstance npc, VmGothicEnums.PerceptionType perception)
        {
            var props = GetProperties(npc);
            props.Perceptions[perception] = -1;
        }

        public static void ExtNpcSetPerceptionTime(NpcInstance npc, float time)
        {
            var props = GetProperties(npc);
            props.PerceptionTime = time;
        }

        public static void ExtNpcSetTalentValue(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            var props = GetProperties(npc);
            props.Talents[talent] = level;
        }

        public static void ExtCreateInvItems(NpcInstance npc, uint itemId, int amount)
        {
            var props = GetProperties(npc);

            props.Items.TryAdd(itemId, amount);
            props.Items[itemId] += amount;
        }

        public static void ExtEquipItem(NpcInstance npc, int itemId)
        {
            var props = GetProperties(npc);
            var itemData = VmInstanceManager.TryGetItemData(itemId);

            props.EquippedItems.Add(itemData);
        }
    }
}
