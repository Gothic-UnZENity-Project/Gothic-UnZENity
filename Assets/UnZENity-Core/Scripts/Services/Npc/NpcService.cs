﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Adapters.Npc;
using GUZ.Core.Const;
using GUZ.Core.Logging;
using GUZ.Core.Creator;
using GUZ.Core.Domain.Npc;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Models.Adapter.Vobs;
using GUZ.Core.Models.Config;
using GUZ.Core.Models.Container;
using GUZ.Core.Models.Npc;
using GUZ.Core.Models.Vm;
using GUZ.Core.Models.Vob;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.Vm;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using MyBox;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Logger = GUZ.Core.Logging.Logger;

namespace GUZ.Core.Services.Npc
{
    /// <summary>
    /// Manage all NPC related calls a(Ext* engine calls and e.g. load Npcs at WorldSceneManager time)
    /// </summary>
    public class NpcService
    {
        public Dictionary<string, List<(int hour, int minute, int status)>> MobRoutines = new();

        [Inject] private readonly GameStateService _gameStateService;
        [Inject] private readonly VmService _vmService;
        [Inject] private readonly ConfigService _configService;
        [Inject] private readonly UnityMonoService _unityMonoService;
        [Inject] private readonly FrameSkipperService _frameSkipperService;
        [Inject] private readonly MultiTypeCacheService _multiTypeCacheService;
        [Inject] private readonly VmCacheService _vmCacheService;
        [Inject] private readonly SaveGameService _saveGameService;
        [Inject] private readonly PlayerService _playerService;
        [Inject] private readonly WayNetService _wayNetService;

        // Supporter class where the whole Init() logic is outsourced for better readability.
        private readonly NpcInitializerDomain _initializerDomain = new NpcInitializerDomain().Inject();


        private Queue<NpcLoader> _objectsToInitQueue = new();
        private Queue<NpcContainer> _objectToReEnableQueue = new();

        private DaedalusVm _vm => _gameStateService.GothicVm;

        private const float _fpLookupDistance = 7f; // meter

        
        public void Init()
        {
            _unityMonoService.StartCoroutine(InitNpcCoroutine());
            _unityMonoService.StartCoroutine(ReEnableNpcCoroutine());
            
            GlobalEventDispatcher.LoadGameStart.AddListener(() =>
            {
                _objectsToInitQueue.Clear();
                _objectToReEnableQueue.Clear();
            });
            
            GlobalEventDispatcher.NpcMeshCullingChanged.AddListener(EventNpcMeshCullingChanged);
            GlobalEventDispatcher.CreateNpc.AddListener(CreateVobNpc);
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
                    var monsterId = npcElement.Npc.GetAiVar(_vmService.AIVMMRealId);

                    // Do not load NPCs we don't want to have via Debug flags.
                    if (npcId != 0 && _configService.Dev.SpawnNpcInstances.Value.Any() &&
                        !_configService.Dev.SpawnNpcInstances.Value.Contains(npcElement.Npc.Id))
                    {
                        continue;
                    }

                    // Do not load Monsters we don't want to have via Debug flags.
                    if (npcId == 0 && monsterId != 0 && _configService.Dev.SpawnMonsterInstances.Value.Any() &&
                        !_configService.Dev.SpawnMonsterInstances.Value.Contains(
                            (DeveloperConfigEnums.MonsterId)monsterId))
                    {
                        continue;
                    }

                    _initializerDomain.InitNpc(npcElement.Npc, npcElement.gameObject);
                }

                yield return _frameSkipperService.TrySkipToNextFrameCoroutine();
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
                    yield return _frameSkipperService.TrySkipToNextFrameCoroutine();
                }
            }
        }

        public void SetRootGo(GameObject rootGo)
        {
            _initializerDomain.RootGo = rootGo;
        }

        public Vector3 GetFreeAreaAtSpawnPoint(Vector3 positionToScan)
        {
            return _initializerDomain.GetFreeAreaAtSpawnPoint(positionToScan);
        }

        public async Task CreateWorldNpcs(LoadingService loading)
        {
            MobRoutines.ClearAndReleaseMemory();
            MobRoutines = new();
            
            if (_saveGameService.IsNewGame)
                await _initializerDomain.InitNpcsNewGame(loading);
            else
                await _initializerDomain.InitNpcsSaveGame(loading);
        }

        /// <summary>
        /// World Vobs from a SaveGame contains NPCs if they're close to our hero during save time.
        /// We will create them here as a "normal" lazy loaded NPC.
        /// </summary>
        public void CreateVobNpc(INpc vobNpc)
        {
            if (vobNpc.Name.EqualsIgnoreCase(Constants.DaedalusHeroInstanceName))
            {
                _playerService.HeroSpawnPosition = vobNpc.Position.ToUnityVector();
                _playerService.HeroSpawnRotation = vobNpc.Rotation.ToUnityQuaternion();
                return;
            }

            // Initialize NPC and set its data from SaveGame (VOB entry).
            _initializerDomain.InitNpcVobSaveGame(vobNpc);
        }

        public void ExtWldInsertNpc(int npcInstanceIndex, string spawnPoint)
        {
            _initializerDomain.ExtWldInsertNpc(npcInstanceIndex, spawnPoint);
        }

        // FIXME - I think they are overwritten when an NPC is loaded from a SaveGame, as we Initialize them again...
        public void ExtNpcSetTalentValue(NpcInstance npc, VmGothicEnums.Talent talent, int level)
        {
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
            var vob = npc.GetUserData()!.Vob;

            vob.SetTalent((int)talent, new Talent
            {
                Type =  (int)talent,
                Skill = skillValue,
                Value = 0
            });
        }

        public void ExtMdlSetVisual(NpcInstance npc, string visual)
        {
            var props = npc.GetUserData().Props;
            props.MdsNameBase = visual;
        }

        public void ExtSetVisualBody(ExtSetVisualBodyData data)
        {
            var props = data.Npc.GetUserData().Props;

            props.BodyData = data;

            if (data.Armor >= 0)
            {
                var armorData = _vmCacheService.TryGetItemData(data.Armor);
                props.EquippedItems.Add(_vmCacheService.TryGetItemData(data.Armor));
                props.MdmName = armorData.VisualChange;
            }
            else
            {
                props.MdmName = data.Body;
            }
        }

        public NpcInstance ExtHlpGetNpc(int instanceId)
        {
            return _multiTypeCacheService.NpcCache
                .FirstOrDefault(i => i.Instance.Index == instanceId)?
                .Instance;
        }

        public void ExtNpcChangeAttribute(NpcInstance npc, int attributeId, int value)
        {
            var vob = npc.GetUserData().Vob;

            vob.Attributes[attributeId] = value;
        }

        public NpcContainer GetHeroContainer()
        {
            return ((NpcInstance)_gameStateService.GothicVm.GlobalHero).GetUserData();
        }

        public GameObject GetHeroGameObject()
        {
            return ((NpcInstance)_gameStateService.GothicVm.GlobalHero).GetUserData().Go;
        }

        /// <summary>
        /// We need to first Alloc() hero data space and put the instance to the cache.
        /// Then we initialize it. (During Init, PC_HERO:Npc_Default->Prototype:Npc_Default will call SetTalentValue where we need the lookup to fetch the NpcInstance).
        ///
        /// This method will get called every time we spawn into another world. We therefore need to check if initialize the first time or we only need to set the lookup cache.
        /// </summary>
        public void CacheHero()
        {
            if (_gameStateService.GothicVm.GlobalHero != null)
            {
                // We assume that this call is only made when the cache got cleared before as we loaded another world.
                // Therefore, we re-add it now.
                var heroContainer = ((NpcInstance)_gameStateService.GothicVm.GlobalHero).GetUserData();
                _multiTypeCacheService.NpcCache.Add(heroContainer);

                return;
            }

            // Initial setup
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);

            // Flat player
            if (playerGo == null)
            {
                playerGo = GameObject.FindWithTag(Constants.MainCameraTag);
            }

            var heroInstance = _gameStateService.GothicVm.AllocInstance<NpcInstance>(_configService.GothicGame.Player);
            var heroDaedalusInstance = _gameStateService.GothicVm.GetSymbolByName(_configService.GothicGame.Player)!;

            var vobNpc = new NpcAdapter(heroDaedalusInstance.Index)
            {
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

            _playerService.SetHero(npcData);
            _multiTypeCacheService.NpcCache.Add(npcData);
            _vm.InitInstance(heroInstance);
            vobNpc.CopyFromInstanceData(heroInstance);

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

        public void ExtApplyOverlayMds(NpcInstance npc, string overlayName)
        {
            npc.GetUserData().Props.MdsNameOverlay = overlayName;
        }

        public void ExtNpcSetToFistMode(NpcInstance npc)
        {
            var npcProperties = npc.GetUserData().Props;

            npc.GetUserData()!.Vob.FightMode = (int)VmGothicEnums.WeaponState.Fist;

            // if npc has item in hand remove it and set weapon to fist
            // Some animations need to force remove items, some not.
            if (npcProperties.UsedItemSlot.IsNullOrEmpty())
                return;

            var slotGo = npc.GetUserData().Go.FindChildRecursively(npcProperties.UsedItemSlot);
            var item = slotGo!.transform.GetChild(0);

            Object.Destroy(item.gameObject);
        }
        
        public void ExtNpcSetToFightMode(NpcInstance npc, int itemIndex)
        {
            npc.GetUserData()!.Vob.FightMode = (int)VmGothicEnums.WeaponState.W1H;

            // FIXME - Spawn Item as well!
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
                _initializerDomain.InitNpc(loaderComp.Npc, loaderComp.gameObject);
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

            return _wayNetService.FindNearestWayPoint(pos, true).Name;
        }

        public bool ExtWldIsFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData().Props;
            var npcGo = npc.GetUserData().Go;
            var freePoints =
                _wayNetService.FindFreePointsWithName(npcGo.transform.position, fpNamePart, _fpLookupDistance);

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

            return _wayNetService.FindNearestWayPoint(pos).Name;
        }

        public bool ExtIsNextFpAvailable(NpcInstance npc, string fpNamePart)
        {
            var props = npc.GetUserData().Props;
            var pos = npc.GetUserData().Go.transform.position;
            var fp = _wayNetService.FindNearestFreePoint(pos, fpNamePart, null);

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
            npcContainer.Props.Dialogs = _gameStateService.Dialogs.Instances
                .Where(dialog => dialog.Npc == npcIndex)
                .OrderByDescending(dialog => dialog.Important)
                .ToList();
        }

        private void EventNpcMeshCullingChanged(NpcContainer npcContainer, NpcLoader npcLoader, bool isInVisibleRange, bool wasOutOfDistance)
        {
            // Alter position tracking of NPC
            if (isInVisibleRange)
            {
                var initializedNow = InitNpc(npcLoader.gameObject);

                // If the NPC !wasOutOfDistance (==wasInDistanceAlready), then we spawned our VRPlayer next to the NPC
                // (e.g. from a save game) and we need to go on with the current routine instead of "resetting" the routine.
                // (Which would respawn NPC at a waypoint, which is wrong.)
                if (wasOutOfDistance && !initializedNow)
                {
                    ReEnableNpc(npcContainer);
                }
            }
        }
    }
}
