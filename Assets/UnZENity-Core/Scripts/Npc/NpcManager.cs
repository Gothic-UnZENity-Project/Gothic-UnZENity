using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Caches;
using GUZ.Core.Config;
using GUZ.Core.Data;
using GUZ.Core.Data.Container;
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
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Npc
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcManager
    {
        // Supporter class where the whole Init() logic is outsourced for better readability.
        private NpcInitializer _initializer = new ();
        private Queue<NpcLoader> _objectsToInitQueue = new();
        private Queue<NpcContainer> _objectToReEnableQueue = new();

        private static DaedalusVm _vm => GameData.GothicVm;

        private const float _fpLookupDistance = 7f; // meter

        
        public void Init(ICoroutineManager coroutineManager)
        {
            coroutineManager.StartCoroutine(InitNpcCoroutine());
            coroutineManager.StartCoroutine(ReEnableNpcCoroutine());
            
            GlobalEventDispatcher.LoadGameStart.AddListener(() =>
            {
                _objectsToInitQueue.Clear();
                _objectToReEnableQueue.Clear();
            });
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
                        !GameGlobals.Config.Dev.SpawnMonsterInstances.Value.Contains(
                            (DeveloperConfigEnums.MonsterId)monsterId))
                    {
                        continue;
                    }

                    _initializer.InitNpc(npcElement.Npc, npcElement.gameObject);
                }

                yield return FrameSkipper.TrySkipToNextFrameCoroutine();
            }
        }

        /// <summary>
        /// Reset one-by-one. Otherwise NPCs will spawn over another if starting from the same WayPoint.
        /// </summary>
        private IEnumerator ReEnableNpcCoroutine()
        {
            while (true)
            {
                if (_objectToReEnableQueue.IsEmpty())
                {
                    yield return null;
                }
                else
                {
                    var npcData = _objectToReEnableQueue.Dequeue();
                    // If we walked to an NPC in our game, the NPC will be re-enabled and Routines get reset.
                    npcData.PrefabProps?.AiHandler?.ReEnableNpc();
                    yield return FrameSkipper.TrySkipToNextFrameCoroutine();
                }
            }
        }

        public void SetRootGo(GameObject rootGo)
        {
            _initializer.RootGo = rootGo;
        }

        public Vector3 GetFreeAreaAtSpawnPoint(Vector3 positionToScan)
        {
            return _initializer.GetFreeAreaAtSpawnPoint(positionToScan);
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

        // FIXME - I think they are overwritten when an NPC is loaded from a SaveGame, as we Initialize them again...
        public void ExtNpcSetTalentValue(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
            InitTalents(npc);
            var vob = npc.GetUserData()!.Vob;

            vob.SetTalent((int)talent, new Talent
            {
                Type =  (int)talent,
                Skill = 0,
                Value = level
            });
        }
        
        // FIXME - In OpenGothic it adds MDS overlays based on skill level.
        public void ExtNpcSetTalentSkill(NpcInstance npc, VmGothicEnums.Talent talent, int skillValue)
        {
            InitTalents(npc);
            var vob = npc.GetUserData()!.Vob;

            vob.SetTalent((int)talent, new Talent
            {
                Type =  (int)talent,
                Skill = skillValue,
                Value = 0
            });
        }

        /// <summary>
        /// Initialize for the first time if not yet done.
        /// </summary>
        private void InitTalents(NpcInstance npc)
        {
            if (npc.GetUserData()!.Vob.TalentCount != 0)
                return;

            var vob = npc.GetUserData()!.Vob;
            for (var i = 0; i < Constants.Daedalus.TalentsMax; i++)
            {
                vob.AddTalent(new Talent()
                {
                    Type = i,
                    Value = 0,
                    Skill = 0
                });
            }
        }

        public void ExtMdlSetVisual(NpcInstance npc, string visual)
        {
            var props = npc.GetUserData().Props;
            props.MdsNameBase = visual;
        }

        public void ExtSetVisualBody(VmGothicExternals.ExtSetVisualBodyData data)
        {
            var props = data.Npc.GetUserData().Props;

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
            return MultiTypeCache.NpcCache
                .FirstOrDefault(i => i.Instance.Index == instanceId)?
                .Instance;
        }

        public void ExtNpcChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            var vob = npc.GetUserData().Vob;

            vob.Attributes[attributeId] = value;
        }

        public void ExtCreateInvItems(NpcInstance npc, int itemId, int amount)
        {
            // We also initialize NPCs inside Daedalus when we load a save game. It's needed as some data isn't stored on save games.
            // But e.g. inventory items will be skipped as they are stored inside save game VOBs.
            if (!GameGlobals.SaveGame.IsWorldLoadedForTheFirstTime)
            {
                return;
            }

            if (npc.GetUserData() == null)
            {
                Logger.LogError($"NPC is not set for {nameof(ExtCreateInvItems)}. Is it an error on Daedalus or our end?", LogCat.Npc);
                return;
            }

            var itemInstanceName = GameData.GothicVm.GetSymbolByIndex(itemId)!.Name;
            var vob = npc.GetUserData()!.Vob;

            IItem item = null;
            for (var i = 0; i < vob.ItemCount; i++)
            {
                if (vob.GetItem(i).Instance == itemInstanceName)
                {
                    item = vob.GetItem(i);
                    break;
                }
            }

            if (item == null)
            {
                vob.AddItem(new Item()
                {
                    Instance = itemInstanceName,
                    Amount = amount
                });
            }
            else
            {
                item.Amount += amount;
            }
        }

        public NpcContainer GetHeroContainer()
        {
            return ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData();
        }

        public GameObject GetHeroGameObject()
        {
            return ((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData().Go;
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
                // We assume that this call is only made when the cache got cleared before as we loaded another world.
                // Therefore, we re-add it now.
                MultiTypeCache.NpcCache.Add(((NpcInstance)GameData.GothicVm.GlobalHero).GetUserData());

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

            var vobNpc = new NpcVob(-1)
            {
                Name = GameGlobals.Config.Gothic.PlayerInstanceName,
                Player = true
            };

            var npcData = new NpcContainer
            {
                Instance = heroInstance,
                Vob = vobNpc,
                Go = playerGo,
                Props = new(),
                // We need to set it now, as the normal "init" logic of the Awake function in this Comp won't work.
                PrefabProps = playerGo.GetComponentInChildren<NpcPrefabProperties>()
            };

            npcData.PrefabProps.Head = Camera.main!.transform;
            npcData.PrefabProps.NpcSubtitles = playerGo.GetComponentInChildren<INpcSubtitles>(true);

            heroInstance.UserData = npcData;

            MultiTypeCache.NpcCache.Add(npcData);
            _vm.InitInstance(heroInstance);

            _vm.GlobalHero = heroInstance;
        }

        public void ExtMdlSetModelScale(NpcInstance npc, System.Numerics.Vector3 scale)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData().Vob.ModelScale = scale;
        }

        public void ExtSetModelFatness(NpcInstance npc, float fatness)
        {
            // FIXME - Set this value on actual GameObject later.
            npc.GetUserData().Vob.ModelFatness = fatness;
        }

        public void ExtEquipItem(NpcInstance npc, int itemId)
        {
            var props = npc.GetUserData().Props;
            var itemData = VmInstanceManager.TryGetItemData(itemId);

            props.EquippedItems.Add(itemData);
        }

        public void ExtApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            npc.GetUserData().Props.MdsNameOverlay = overlayName;
        }

        public void ExtNpcSetToFistMode(NpcInstance npc)
        {
            var npcProperties = npc.GetUserData().Props;

            npcProperties.WeaponState = VmGothicEnums.WeaponState.Fist;

            // if npc has item in hand remove it and set weapon to fist
            // Some animations need to force remove items, some not.
            if (npcProperties.UsedItemSlot.IsNullOrEmpty())
            {
                return;
            }

            var slotGo = npc.GetUserData().Go.FindChildRecursively(npcProperties.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }

        public void ExtNpcExchangeRoutine(NpcInstance npcInstance, string routineName)
        {
            var formattedRoutineName = $"Rtn_{routineName}_{npcInstance.Id}";
            var newRoutine = _vm.GetSymbolByName(formattedRoutineName);

            if (newRoutine == null)
            {
                Logger.LogError($"Routine {formattedRoutineName} couldn't be found.", LogCat.Npc);
                return;
            }

            ExchangeRoutine(npcInstance, newRoutine.Index);
        }

        public void ExtTaMin(NpcInstance npc, int startH, int startM, int stopH, int stopM, int action, string waypoint)
        {
            var props = npc.GetUserData().Props;

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
        
        public void ExchangeRoutine(NpcInstance npc, string routineName)
        {
            var routine = GameData.GothicVm.GetSymbolByName(routineName);

            ExchangeRoutine(npc, routine == null ? 0: routine.Index);
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

            npc.GetUserData().Props.Routines.Clear();

            // We always need to set "self" before executing any Daedalus function.
            GameData.GothicVm.GlobalSelf = npc;
            GameData.GothicVm.Call(routineIndex);

            npc.GetUserData().Vob.HasRoutine = npc.GetUserData().Props.Routines.NotNullOrEmpty();
            
            CalculateCurrentRoutine(npc);
        }

        /// <summary>
        /// Based on time of the day, we need to calculate routine.
        /// </summary>
        private bool CalculateCurrentRoutine(NpcInstance npc)
        {
            var npcProps = npc.GetUserData().Props;
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
            go.TryGetComponent(out NpcLoader loaderComp);

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
        
        public void ReEnableNpc(NpcContainer npcData)
        {
            _objectToReEnableQueue.Enqueue(npcData);
        }

        public string ExtNpcGetNextWp(NpcInstance npc)
        {
            var pos = npc.GetUserData().Go.transform.position;

            return WayNetHelper.FindNearestWayPoint(pos, true).Name;
        }

        public bool ExtWldIsFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData().Props;
            var npcGo = npc.GetUserData().Go;
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
            var pos = npc.GetUserData().Go.transform.position;

            return WayNetHelper.FindNearestWayPoint(pos).Name;
        }

        public bool ExtIsNextFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData().Props;
            var pos = npc.GetUserData().Go.transform.position;
            var fp = WayNetHelper.FindNearestFreePoint(pos, fpNamePart, null);

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

        public void SetDialogs(NpcContainer npcContainer)
        {
            var npcIndex = npcContainer.Instance.Index;
            npcContainer.Props.Dialogs = GameData.Dialogs.Instances
                .Where(dialog => dialog.Npc == npcIndex)
                .OrderByDescending(dialog => dialog.Important)
                .ToList();
        }

        public int ExtNpcHasItems(NpcInstance npc, int itemId)
        {
            var npcVob = npc.GetUserData()!.Vob;
            var itemInstanceName = GameData.GothicVm.GetSymbolByIndex(itemId)!.Name;
            
            for (var i = 0; i < npcVob.ItemCount; i++)
            {
                if (npcVob.GetItem(i).Name == itemInstanceName)
                    return npcVob.GetItem(i).Amount;
            }
            
            return 0;
        }
        
        public void ExtNpcClearInventory(NpcInstance npc)
        {
            npc.GetUserData()!.Vob.ClearItems();
        }
    }
}
