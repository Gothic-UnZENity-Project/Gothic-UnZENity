using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Properties;
using GUZ.Core.Util;
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

        private static readonly List<(NpcContainer npcData, string spawnPoint)> _tmpWldInsertNpcData = new();

        static NpcCreator()
        {
            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldLoaded);
        }

        /// <summary>
        /// If the current world is visited for the first time, we call Wld_InsertNpc() to "spawn" them for the first time.
        /// </summary>
        public static async Task CreateAsync(DeveloperConfig config, LoadingManager loading)
        {
            // Final debug check if we really want to load NPCs.
            if (!config.EnableNpcs)
            {
                return;
            }

            if (!GameGlobals.SaveGame.IsLoadedGame)
            {
                await InitializeNpcsFirstTime(loading);
            }
            else
            {
                await InitializeNpcsFromSaveGame(loading);
            }
        }

        /// <summary>
        /// We load NPCs via Daedalus Init_*() only! if we enter the world for the first time
        /// when reaching a world for the first time.
        /// </summary>
        private static async Task InitializeNpcsFirstTime(LoadingManager loading)
        {
            // Inside Startup.d, it's always STARTUP_{MAPNAME} and INIT_{MAPNAME}
            // FIXME - Inside Startup.d some Startup_*() functions also call Init_*() some not. How to handle properly? (Force calling it here? Even if done twice?)
            GameData.GothicVm.Call($"STARTUP_{GameGlobals.SaveGame.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}");

            // Daedalus will walk through the whole Wld_InsertNpc() calls once.
            // Afterwards we will crate the NPCs step-by-step to ensure smooth loading screen fps.
            await InitializeNpcs(loading);
        }

        /// <summary>
        /// If we loaded the data from a save game or previous visit in this game session,
        /// we have our NPCs nearby already loaded via Vobs and load remaining (far) NPCs now.
        /// </summary>
        private static async Task InitializeNpcsFromSaveGame(LoadingManager loading)
        {
            loading.SetProgressStep(LoadingManager.LoadingProgressType.Npc,  GameGlobals.SaveGame.CurrentWorldData.Npcs.Count);

            foreach (var npcVob in GameGlobals.SaveGame.CurrentWorldData.Npcs)
            {
                loading.AddProgress();
                await FrameSkipper.TrySkipToNextFrame();

                var instance = Vm.AllocInstance<NpcInstance>(npcVob.Name);
                var npcData = new NpcContainer()
                {
                    Instance = instance,
                    Vob = npcVob
                };
                instance.UserData = npcData;

                MultiTypeCache.NpcCache.Add(npcData);

                var newNpc = InitializeNpc(instance, true, npcVob);

                if (newNpc == null)
                {
                    continue;
                }

                var isSpawned = SetSpawnPoint(npcData, npcVob.ScriptWaypoint);

                if (isSpawned)
                {
                    GameGlobals.NpcMeshCulling.AddCullingEntry(newNpc);
                }
            }
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
            return npc.GetUserData().Properties;
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
            var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex)!;
            var npcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);

            var userDataObject = new NpcContainer
            {
                Instance = npcInstance
            };

            // We reference our object as user data to retrieve it whenever a Daedalus External provides an NpcInstance as input.
            // With this, we can always switch between our UnZENity data and ZenKit data.
            npcInstance.UserData = userDataObject;

            // IMPORTANT!: NpcInstance.UserData stores a weak pointer. i.e. if we do not store the local variable it would get removed.
            MultiTypeCache.NpcCache.Add(userDataObject);

            // For mesh creation later, we need to store, that there is a new NPC or a duplicate Monster to be spawned.
            _tmpWldInsertNpcData.Add((userDataObject, spawnPoint));
        }

        private static async Task InitializeNpcs(LoadingManager loading)
        {
            loading.SetProgressStep(LoadingManager.LoadingProgressType.Npc, _tmpWldInsertNpcData.Count);

            foreach (var npc in _tmpWldInsertNpcData)
            {
                // Update progress bar and check if we need to wait for next frame now (As some conditions skip -continue- end of loop and would skip check)
                loading.AddProgress();
                await FrameSkipper.TrySkipToNextFrame();

                if (WayNetHelper.GetWayNetPoint(npc.spawnPoint) is null)
                {
                    Debug.LogWarning($"Cannot spawn NPC as waypoint ${npc.spawnPoint} does not exist.");
                    continue;
                }

                var newNpc = InitializeNpc(npc.npcData.Instance, false, new ZenKit.Vobs.Npc());
                if (newNpc == null)
                {
                    continue;
                }

                var isSpawned = SetSpawnPoint(npc.npcData, npc.spawnPoint);

                if (isSpawned)
                {
                    GameGlobals.NpcMeshCulling.AddCullingEntry(newNpc);
                }
            }

            // Full loading of NPCs is done.
            loading.AddProgress(LoadingManager.LoadingProgressType.VOB, 1f);
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
        /// <param name="isFromSaveGame">We will alter certain aspects of initializing if NPC is created from a saved VOB.</param>
        [CanBeNull]
        public static GameObject InitializeNpc(NpcInstance npcInstance, bool isFromSaveGame, ZenKit.Vobs.Npc npcVob)
        {
            var npcData = npcInstance.GetUserData();
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);
            var propertiesComp = newNpc.GetComponent<NpcProperties>();

            npcData.Vob = npcVob;
            npcData.Properties = propertiesComp;
            npcData.Properties.NpcData = npcData;

            propertiesComp.SetData(npcVob);

            // As we have our back reference between NpcInstance and NpcData, we can now initialize the object on ZenKit side.
            // Lookups like Npc_SetTalentValue() will work now as NpcInstance.UserData() points to our object which stores the information.
            Vm.InitInstance(npcInstance);

            OverwriteInitDataWithSaveGameData(isFromSaveGame, npcData);

            npcData.Properties.Dialogs = GameData.Dialogs.Instances
                .Where(dialog => dialog.Npc == npcInstance.Index)
                .OrderByDescending(dialog => dialog.Important)
                .ToList();

            // Hint: If we filter out NPCs to spawn, we will never get any Monster as they have no Ids set. Except default: 0.
            if (GameGlobals.Config.Dev.SpawnNpcInstances.Value.Any() &&
                !GameGlobals.Config.Dev.SpawnNpcInstances.Value.Contains(npcInstance.Id))
            {
                Object.Destroy(newNpc);                                                                         // 1
                MultiTypeCache.NpcCache.Remove(MultiTypeCache.NpcCache.First(i => i.Instance == npcInstance));  // 2
                // Hint: We don't destroy NpcInstance object. As this is a debug IF statement only, it's fine.  // 3

                return null;
            }

            newNpc.name = $"{npcInstance.GetName(NpcNameSlot.Slot0)} ({npcInstance.Id})";

            var mdhName = string.IsNullOrEmpty(propertiesComp.OverlayMdhName)
                ? propertiesComp.BaseMdhName
                : propertiesComp.OverlayMdhName;
            MeshFactory.CreateNpc(newNpc.name, propertiesComp.MdmName, mdhName, propertiesComp.BodyData,
                newNpc, GetRootGo());

            foreach (var equippedItem in propertiesComp.EquippedItems)
            {
                MeshFactory.CreateNpcWeapon(newNpc, equippedItem, (VmGothicEnums.ItemFlags)equippedItem.MainFlag,
                    (VmGothicEnums.ItemFlags)equippedItem.Flags);
            }

            var npcRoutine = npcInstance.DailyRoutine;
            NpcHelper.ExchangeRoutine(npcInstance, npcRoutine);

            newNpc.TryGetComponent<Routine>(out var routine);

            // As they're loaded asynchronously, we need to disable every NPC/Monster during loading.
            // We will enable them via Culling once everything is loaded.
            // Otherwise, they'll fall through the world as the world mesh is loaded last. ;-)
            newNpc.SetActive(false);

            return newNpc;
        }

        /// <summary>
        /// If we load a game from SaveGame (and the world is not visited for the first time), we need to exchange some
        /// values between NpcInstance and Vobs.Npc
        /// e.g. DailyRoutine is updated in VOB
        /// </summary>
        private static void OverwriteInitDataWithSaveGameData(bool isFromSaveGame, NpcContainer data)
        {
            if (!isFromSaveGame)
            {
                return;
            }

            var instance = data.Instance;
            var vob = data.Vob;

            if (vob.HasRoutine)
            {
                instance.DailyRoutine = Vm.GetSymbolByName(vob.CurrentRoutine)!.Index;
            }

            if (vob.StartAiState.NotNullOrEmpty())
            {
                instance.StartAiState = Vm.GetSymbolByName(vob.StartAiState)!.Index;
            }

            instance.Guild = vob.Guild;
            instance.Level = vob.Level;
            instance.FightTactic = vob.FightTactic;
            instance.SpawnDelay = vob.RespawnTime;
            instance.Exp = vob.Xp;
            instance.ExpNext = vob.XpNextLevel;
            instance.Lp = vob.Lp;
            instance.BodyStateInterruptableOverride = vob.BsInterruptableOverride;

            var vobMissions = vob.Missions;
            for (var i = 0; i < vobMissions.Count; i++)
            {
                instance.SetMission((NpcMissionSlot)i, vobMissions[i]);
            }

            var vobAttributes = vob.Attributes;
            for (var i = 0; i < vobAttributes.Count; i++)
            {
                instance.SetAttribute((NpcAttribute)i, vobAttributes[i]);
            }

            var vobHitChances = vob.HitChance;
            for (var i = 0; i < vobHitChances.Count; i++)
            {
                instance.SetHitChance((NpcTalent)i, vobHitChances[i]);
            }

            var vobProtections = vob.Protection;
            for (var i = 0; i < vobProtections.Count; i++)
            {
                instance.SetProtection((DamageType)i, vobProtections[i]);
            }

            var vobAiVars = vob.AiVars;
            for (var i = 0; i < vobAiVars.Length; i++)
            {
                instance.SetAiVar(i, vobAiVars[i]);
            }
        }

        /// <summary>
        /// The startpoint to create NPCs isn't necessarily the spawnpoint mentioned here.
        /// It can also be the currently active routine point to walk to.
        /// We therefore execute the daily routines to collect current location and use this as spawn location.
        /// </summary>
        private static bool SetSpawnPoint(NpcContainer npc, string spawnPoint)
        {
            WayNetPoint initialSpawnPoint;

            // Find the right spawn point based on currently active routine.
            if (npc.Go.GetComponent<Routine>().Routines.Any())
            {
                var routineSpawnPointName = npc.Go.GetComponent<Routine>().CurrentRoutine.Waypoint;
                initialSpawnPoint = WayNetHelper.GetWayNetPoint(routineSpawnPointName);

                // As per the original game we don't spawn the NPC if the WayNet point doesn't exist.
                if (WayNetHelper.GetWayNetPoint(routineSpawnPointName) == null)
                {
                    Object.Destroy(npc.Go);                                                                         // 1
                    MultiTypeCache.NpcCache.Remove(MultiTypeCache.NpcCache.First(i => i.Instance == npc.Instance)); // 2
                    // Hint: We don't destroy NpcInstance object. But in G1 there's only one NPC with this issue.   // 3
                    return false;
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
                return false;
            }

            var isPositionFound = false;
            var testRadius = 1f; // ~2x size of normal bounding box of an NPC.
            // Some FP/WP are on a hill. The spawn check will therefore lift the location for a little to not interfere with world mesh collision check.
            var groundControlDifference = new Vector3(0, 1f, 0);
            var initialSpawnPointGroundControl = initialSpawnPoint.Position + groundControlDifference;

            // Check if the spawn point is free.
            if (!Physics.CheckSphere(initialSpawnPointGroundControl, testRadius / 2))
            {
                npc.Go.transform.position = initialSpawnPoint.Position;
                // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                npc.Go.GetComponentInChildren<Rigidbody>().position = initialSpawnPoint.Position;
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
                            npc.Go.transform.position = initialSpawnPoint.Position + offsetPoint;
                            // There are three options to sync the Physics information for collision check. This is the most performant one as it only alters the single V3.
                            npc.Go.GetComponentInChildren<Rigidbody>().position =
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
                    $"No suitable spawn point found for NPC >{npc.Go.name}<. Circle search didn't find anything!");
                return false;
            }

            // Some data to be used for later.
            if (initialSpawnPoint.IsFreePoint())
            {
                npc.Go.GetComponent<NpcProperties>().CurrentFreePoint = (FreePoint)initialSpawnPoint;
            }
            else
            {
                npc.Go.GetComponent<NpcProperties>().CurrentWayPoint = (WayNet_WayPoint)initialSpawnPoint;
            }

            return true;
        }

        private static void PostWorldLoaded()
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
            npc.GetUserData().Vob.ModelFatness = fatness;

            var npcGo = GetNpcGo(npc);
            var oldScale = npcGo.transform.localScale;
            var bonusFat = fatness * _fatnessScale;

            npcGo.transform.localScale = new Vector3(oldScale.x + bonusFat, oldScale.y, oldScale.z + bonusFat);
        }

        /// <summary>
        /// Hint: This lookup is exclusively used for Npcs as elements for Monsters will be stored multiple times with the same index.
        /// (An NpcInstance.index always correlates to a specific Daedalus C_Class instance)
        /// </summary>
        public static NpcInstance ExtHlpGetNpc(int instanceId)
        {
            return MultiTypeCache.NpcCache
                .FirstOrDefault(i => i.Instance.Index == instanceId)?
                .Instance;
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
            // We also initialize NPCs inside Daedalus when we load a save game. It's needed as some data isn't stored on save games.
            // But e.g. inventory items will be skipped as they are stored inside save game VOBs.
            if (!GameGlobals.SaveGame.IsWorldLoadedForTheFirstTime)
            {
                return;
            }

            var props = GetProperties(npc);
            if (props == null)
            {
                Debug.LogError($"NPC not found with index {npc.Index}");
                return;
            }
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
