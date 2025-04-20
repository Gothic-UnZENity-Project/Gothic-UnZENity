using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc.Routines;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using MyBox;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using Vector3 = System.Numerics.Vector3;

namespace GUZ.Core._Npc2
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcManager2
    {
        // Supporter class where the whole Init() logic is outsourced for better readability.
        private NpcInitializer2 _initializer = new ();
        private Queue<NpcLoader2> _objectsToInitQueue = new();

        private static DaedalusVm _vm => GameData.GothicVm;

        private const float _fpLookupDistance = 7f; // meter

        
        public void Init(ICoroutineManager coroutineManager)
        {
            coroutineManager.StartCoroutine(InitNpcCoroutine());
        }

        private IEnumerator InitNpcCoroutine()
        {
            while (true)
            {
                if (_objectsToInitQueue.IsEmpty())
                {
                    yield return null;
                }
                else
                {
                    var npcElement = _objectsToInitQueue.Dequeue();

                    var npcId = npcElement.Npc.Id;
                    var monsterId = npcElement.Npc.GetAiVar(Constants.DaedalusConst.AIVMMRealId);

                    // Do not load NPCs we don't want to have via Debug flags.
                    if (npcId != 0 && GameGlobals.Config.Dev.SpawnNpcInstances.Value.Any() &&
                        !GameGlobals.Config.Dev.SpawnNpcInstances.Value.Contains(npcElement.Npc.Id))
                    {
                        continue;
                    }

                    // Do not load Monsters we don't want to have via Debug flags.
                    if (npcId == 0 && monsterId != 0 && GameGlobals.Config.Dev.SpawnMonsterInstances.Value.Any() &&
                        !GameGlobals.Config.Dev.SpawnMonsterInstances.Value.Contains((DeveloperConfigEnums.MonsterId)monsterId))
                    {
                        continue;
                    }

                    _initializer.InitNpc(npcElement.Npc, npcElement.gameObject);
                    if (npcId != 0 && GameGlobals.Config.Dev.SpawnNpcInstances.Value.Any() &&
                        !GameGlobals.Config.Dev.SpawnNpcInstances.Value.Contains(npcElement.Npc.Id))
                    {
                        continue;
                    }
                    yield return FrameSkipper.TrySkipToNextFrameCoroutine();
                }
            }
        }

        public void SetRootGo(GameObject rootGo)
        {
            _initializer.RootGo = rootGo;
        }

        public async Task CreateWorldNpcs(LoadingManager loading)
        {
            if (GameGlobals.SaveGame.IsNewGame)
                await _initializer.InitNpcsNewGame(loading);
             else
                await _initializer.InitNpcsSaveGame(loading);
        }

        /// <summary>
        /// World Vobs from a SaveGame contains NPCs if they're close to our hero during save time.
        /// We will create them here as a "normal" lazy loaded NPC.
        /// </summary>
        public void CreateVobNpc(ZenKit.Vobs.Npc vobNpc)
        {
            if (vobNpc.Name.EqualsIgnoreCase(Constants.DaedalusHeroInstanceName))
            {
                GameGlobals.Player.HeroSpawnPosition = vobNpc.Position.ToUnityVector();
                GameGlobals.Player.HeroSpawnRotation = vobNpc.Rotation.ToUnityQuaternion();
                return;
            }

            // Initialize NPC and set its data from SaveGame (VOB entry).
            _initializer.InitNpcVobSaveGame(vobNpc);
        }

        public void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            _initializer.ExtWldInsertNpc(npcInstanceIndex, spawnPoint);
        }

        public void ExtNpcSetTalentValue(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            var props = npc.GetUserData2().Props;
            props.Talents[talent] = level;
        }

        public void ExtMdlSetVisual(NpcInstance npc, string visual)
        {
            var props = npc.GetUserData2().Props;
            props.MdsNameBase = visual;
        }

        public void ExtSetVisualBody(VmGothicExternals.ExtSetVisualBodyData data)
        {
            var props = data.Npc.GetUserData2().Props;

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

        public NpcInstance ExtHlpGetNpc(int instanceId)
        {
            return MultiTypeCache.NpcCache2
                .FirstOrDefault(i => i.Instance.Index == instanceId)?
                .Instance;
        }

        public void ExtNpcChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            var vob = npc.GetUserData2().Vob;

            vob.Attributes[attributeId] = value;
        }

        public void ExtCreateInvItems(NpcInstance npc, uint itemId, int amount)
        {
            // We also initialize NPCs inside Daedalus when we load a save game. It's needed as some data isn't stored on save games.
            // But e.g. inventory items will be skipped as they are stored inside save game VOBs.
            if (!GameGlobals.SaveGame.IsWorldLoadedForTheFirstTime)
            {
                return;
            }

            if (npc.GetUserData2() == null)
            {
                Debug.LogError($"NPC is not set for {nameof(ExtCreateInvItems)}. Is it an error on Daedalus or our end?");
                return;
            }

            var props = npc.GetUserData2().Props;
            if (props == null)
            {
                Debug.LogError($"NPC not found with index {npc.Index}");
                return;
            }
            props.Items.TryAdd(itemId, amount);
            props.Items[itemId] += amount;
        }

        public NpcContainer2 GetHeroContainer()
        {
            return ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData2();
        }

        public GameObject GetHeroGameObject()
        {
            return ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData2().Go;
        }

        /// <summary>
        /// We need to first Alloc() hero data space and put the instance to the cache.
        /// Then we initialize it. (During Init, PC_HERO:Npc_Default->Prototype:Npc_Default will call SetTalentValue where we need the lookup to fetch the NpcInstance).
        ///
        /// This method will get called every time we spawn into another world. We therefore need to check if initialize the first time or we only need to set the lookup cache.
        /// </summary>
        public void CacheHero()
        {
            if (GameData.GothicVm.GlobalHero != null)
            {
                // We assume, that this call is only made when the cache got cleared before as we loaded another world.
                // Therefore, we re-add it now.
                MultiTypeCache.NpcCache2.Add(((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData2());

                return;
            }


            // Initial setup
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);

            // Flat player
            if (playerGo == null)
            {
                playerGo = GameObject.FindWithTag(Constants.MainCameraTag);
            }

            var heroInstance = GameData.GothicVm.AllocInstance<NpcInstance>(GameGlobals.Config.Gothic.PlayerInstanceName);

            var vobNpc = new ZenKit.Vobs.Npc();
            vobNpc.Name = GameGlobals.Config.Gothic.PlayerInstanceName;
            vobNpc.Player = true;

            var npcData = new NpcContainer2
            {
                Instance = heroInstance,
                Vob = vobNpc,
                Go = playerGo,
                Props = new(),
                // We need to set it now, as the normal "init" logic of the Awake function in this Comp won't work.
                PrefabProps = playerGo.GetComponentInChildren<NpcPrefabProperties2>()
            };

            npcData.PrefabProps.Head = Camera.main!.transform;
            npcData.PrefabProps.NpcSubtitles = playerGo.GetComponentInChildren<INpcSubtitles>(true);

            heroInstance.UserData = npcData;

            MultiTypeCache.NpcCache2.Add(npcData);
            _vm.InitInstance(heroInstance);

            _vm.GlobalHero = heroInstance;
        }

        public void ExtMdlSetModelScale(NpcInstance npc, Vector3 scale)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData2().Vob.ModelScale = scale;
        }

        public void ExtSetModelFatness(NpcInstance npc, float fatness)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData2().Vob.ModelFatness = fatness;
        }

        public void ExtEquipItem(NpcInstance npc, int itemId)
        {
            var props = npc.GetUserData2().Props;
            var itemData = VmInstanceManager.TryGetItemData(itemId);

            props.EquippedItems.Add(itemData);
        }

        public void ExtApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            npc.GetUserData2().Props.MdsNameOverlay = overlayName;
        }

        public void ExtNpcSetToFistMode(NpcInstance npc)
        {
            var npcProperties = npc.GetUserData2().Props;

            npcProperties.WeaponState = VmGothicEnums.WeaponState.Fist;

            // if npc has item in hand remove it and set weapon to fist
            // Some animations need to force remove items, some not.
            if (npcProperties.UsedItemSlot.IsNullOrEmpty())
            {
                return;
            }

            var slotGo = npc.GetUserData2().Go.FindChildRecursively(npcProperties.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }

        public void ExtNpcExchangeRoutine(NpcInstance npcInstance, string routineName)
        {
            var formattedRoutineName = $"Rtn_{routineName}_{npcInstance.Id}";
            var newRoutine = _vm.GetSymbolByName(formattedRoutineName);

            if (newRoutine == null)
            {
                Debug.LogError($"Routine {formattedRoutineName} couldn't be found.");
                return;
            }

            ExchangeRoutine(npcInstance, newRoutine.Index);
        }

        public void ExtTaMin(NpcInstance npc, int startH, int startM, int stopH, int stopM, int action, string waypoint)
        {
            var props = npc.GetUserData2().Props;

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

            props.Routines.Add(routine);
        }

        public void ExchangeRoutine(NpcInstance npc, int routineIndex)
        {
            // Monsters
            // e.g. Monsters have no routine, and we just need to send StartAiState function.
            if (routineIndex == 0)
            {
                // FIXME - Call StartRoutine somehow again.
                // We always need to set "self" before executing any Daedalus function.
                // GameData.GothicVm.GlobalSelf = npcInstance;
                // go.GetComponent<AiHandler>().StartRoutine(npcInstance.StartAiState);
                return;
            }

            npc.GetUserData2().Props.Routines.Clear();

            // We always need to set "self" before executing any Daedalus function.
            GameData.GothicVm.GlobalSelf = npc;
            GameData.GothicVm.Call(routineIndex);

            CalculateCurrentRoutine(npc);
        }

        /// <summary>
        /// Based on time of the day, we need to calculate routine.
        /// </summary>
        private bool CalculateCurrentRoutine(NpcInstance npc)
        {
            var npcProps = npc.GetUserData2().Props;
            var currentTime = GameGlobals.Time.GetCurrentDateTime();
            var normalizedNow = currentTime.Hour % 24 * 60 + currentTime.Minute;
            RoutineData newRoutine = null;

            // There are routines where stop is lower than start. (e.g. now:8:00, routine:22:00-9:00), therefore the second check.
            foreach (var routine in npcProps.Routines)
            {
                if (routine.NormalizedStart <= normalizedNow && normalizedNow < routine.NormalizedEnd)
                {
                    newRoutine = routine;
                    break;
                }
                // Handling the case where the time range spans across midnight

                if (routine.NormalizedStart > routine.NormalizedEnd)
                {
                    if (routine.NormalizedStart <= normalizedNow || normalizedNow < routine.NormalizedEnd)
                    {
                        newRoutine = routine;
                        break;
                    }
                }
            }

            // e.g. Mud has a bug as there is no routine covering 8am. We therefore pick the last one as seen in original G1. (sit)
            if (newRoutine == null)
            {
                newRoutine = npcProps.Routines.Last();
            }

            var changed = npcProps.RoutineCurrent != newRoutine;

            if (changed)
            {
                var routineIndex = npcProps.Routines.IndexOf(newRoutine);
                var prevRoutineIndex = routineIndex == 0 ? npcProps.Routines.Count - 1 : routineIndex - 1;;
                npcProps.RoutinePrevious = npcProps.Routines[prevRoutineIndex];
            }
            npcProps.RoutineCurrent = newRoutine;

            return changed;
        }

        public bool InitNpc(GameObject go, bool initImmediately = false)
        {
            go.TryGetComponent(out NpcLoader2 loaderComp);

            if (loaderComp == null || loaderComp.IsLoaded)
            {
                return false;
            }

            // Do not put element into queue a second time.
            loaderComp.IsLoaded = true;

            if (initImmediately)
            {
                _initializer.InitNpc(loaderComp.Npc, loaderComp.gameObject);
            }
            else
            {
                _objectsToInitQueue.Enqueue(loaderComp);
            }

            return true;
        }

        public string ExtNpcGetNextWp(NpcInstance npc)
        {
            var pos = npc.GetUserData2().Go.transform.position;

            return WayNetHelper.FindNearestWayPoint(pos, true).Name;
        }

        public bool ExtWldIsFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData2().Props;
            var npcGo = npc.GetUserData2().Go;
            var freePoints =
                WayNetHelper.FindFreePointsWithName(npcGo.transform.position, fpNamePart, _fpLookupDistance);

            foreach (var fp in freePoints)
            {
                // Kind of: If we're already standing on a FreePoint, then there is one available.
                if (props.CurrentFreePoint == fp)
                {
                    return true;
                }

                // Alternatively, we found a free one within range.
                if (!fp.IsLocked)
                {
                    return true;
                }
            }

            return false;
        }

        public string ExtGetNearestWayPoint(NpcInstance npc)
        {
            var pos = npc.GetUserData2().Go.transform.position;

            return WayNetHelper.FindNearestWayPoint(pos).Name;
        }

        public bool ExtIsNextFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData2().Props;
            var pos = npc.GetUserData2().Go.transform.position;
            var fp = WayNetHelper.FindNearestFreePoint(pos, fpNamePart);

            if (fp == null)
            {
                return false;
            }
            // Ignore if we're already on this FP.

            if (fp == props.CurrentFreePoint)
            {
                return false;
            }

            if (fp.IsLocked)
            {
                return false;
            }

            return true;
        }
    }
}
