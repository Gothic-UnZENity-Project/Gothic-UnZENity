using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core._Npc2;
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
                // Handled within NpcManager now.
            }
            else
            {
                await InitializeNpcsFromSaveGame(loading);
            }
        }

        /// <summary>
        /// If we loaded the data from a save game or previous visit in this game session,
        /// we have our NPCs nearby already loaded via Vobs and load remaining (far) NPCs now.
        /// </summary>
        private static async Task InitializeNpcsFromSaveGame(LoadingManager loading)
        {
            // loading.SetPhase(LoadingManager.LoadingProgressType.Npc,  GameGlobals.SaveGame.CurrentWorldData.Npcs.Count);
            //
            // foreach (var npcVob in GameGlobals.SaveGame.CurrentWorldData.Npcs)
            // {
            //     loading.AddProgress();
            //     await FrameSkipper.TrySkipToNextFrame();
            //
            //     var instance = Vm.AllocInstance<NpcInstance>(npcVob.Name);
            //     var npcData = new NpcContainer()
            //     {
            //         Instance = instance,
            //         Vob = npcVob
            //     };
            //     instance.UserData = npcData;
            //
            //     MultiTypeCache.NpcCache.Add(npcData);
            //
            //     var newNpc = InitializeNpc(instance, true, npcVob);
            //
            //     if (newNpc == null)
            //     {
            //         continue;
            //     }
            //
            //     var isSpawned = SetSpawnPoint(npcData, npcVob.ScriptWaypoint);
            //
            //     if (isSpawned)
            //     {
            //         GameGlobals.NpcMeshCulling.AddCullingEntry(newNpc);
            //     }
            // }
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

        private static NpcProperties2 GetProperties(NpcInstance npc)
        {
            return npc.GetUserData2().Properties;
        }

        private static GameObject GetNpcGo(NpcInstance npcInstance)
        {
            return npcInstance.GetUserData2().Go;
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

        public static void ExtNpcSetTalentSkill(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            // FIXME - TBD.
            // FIXME - In OpenGothic it adds MDS overlays based on skill level.
        }
    }
}
