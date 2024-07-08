using System.Collections.Generic;
using System.Linq;
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
            return LookupCache.NpcCache[npc.Index];
        }

        private static GameObject GetNpcGo(NpcInstance npcInstance)
        {
            return GetProperties(npcInstance).Go;
        }

        /// <summary>
        /// Original Gothic uses this function to spawn an NPC instance into the world.
        /// 
        /// The startpoint to walk isn't neccessarily the spawnpoint mentioned here.
        /// It can also be the currently active routine point to walk to.
        /// We therefore execute the daily routines to collect current location and use this as spawn location.
        /// </summary>
        public static void ExtWldInsertNpc(int npcInstance, string spawnPoint)
        {
            var newNpc = InitializeNpc(npcInstance);

            if (newNpc == null)
            {
                return;
            }

            SetSpawnPoint(newNpc, spawnPoint);
        }

        [CanBeNull]
        public static GameObject InitializeNpc(int npcInstanceIndex)
        {
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);
            var props = newNpc.GetComponent<NpcProperties>();
            var npcSymbol = Vm.GetSymbolByIndex(npcInstanceIndex);

            if (npcSymbol == null)
            {
                Debug.LogError($"Npc with ID {npcInstanceIndex} not found.");
                return null;
            }

            // Humans are singletons.
            if (LookupCache.NpcCache.TryAdd(npcInstanceIndex, newNpc.GetComponent<NpcProperties>()))
            {
                props.NpcInstance = Vm.AllocInstance<NpcInstance>(npcSymbol);
                Vm.InitInstance(props.NpcInstance);

                props.Dialogs = GameData.Dialogs.Instances
                    .Where(dialog => dialog.Npc == props.NpcInstance.Index)
                    .OrderByDescending(dialog => dialog.Important)
                    .ToList();
            }
            // Monsters are used multiple times.
            else
            {
                var origNpc = LookupCache.NpcCache[npcInstanceIndex];
                var origProps = origNpc.GetComponent<NpcProperties>();
                // Clone Properties as they're required from the first instance.
                props.Copy(origProps);
            }

            if (GameGlobals.Config.SpawnNPCInstances.Value.Any() &&
                !GameGlobals.Config.SpawnNPCInstances.Value.Contains(props.NpcInstance.Id))
            {
                LookupCache.NpcCache.Remove(props.NpcInstance.Index);
                Object.Destroy(newNpc);
                return null;
            }

            newNpc.name = $"{props.NpcInstance.GetName(NpcNameSlot.Slot0)} ({props.NpcInstance.Id})";

            var mdhName = string.IsNullOrEmpty(props.OverlayMdhName) ? props.BaseMdhName : props.OverlayMdhName;
            MeshFactory.CreateNpc(newNpc.name, props.MdmName, mdhName, props.BodyData, newNpc);
            newNpc.SetParent(GetRootGo());

            foreach (var equippedItem in props.EquippedItems)
            {
                MeshFactory.CreateNpcWeapon(newNpc, equippedItem, (VmGothicEnums.ItemFlags)equippedItem.MainFlag,
                    (VmGothicEnums.ItemFlags)equippedItem.Flags);
            }

            var npcRoutine = props.NpcInstance.DailyRoutine;
            NpcHelper.ExchangeRoutine(newNpc, props.NpcInstance, npcRoutine);

            return newNpc;
        }

        private static void SetSpawnPoint(GameObject npcGo, string spawnPoint)
        {
            WayNetPoint initialSpawnPoint;

            // Find the right spawn point based on currently active routine.
            if (npcGo.GetComponent<Routine>().Routines.Any() && GameGlobals.Config.EnableNPCRoutines)
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
                            npcGo.GetComponentInChildren<Rigidbody>().position = initialSpawnPoint.Position + offsetPoint;
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
            if (!LookupCache.NpcCache.TryGetValue(instanceId, out var properties))
            {
                Debug.LogError($"Couldn't find NPC {instanceId} inside cache.");
                return null;
            }


            return properties.NpcInstance;
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
