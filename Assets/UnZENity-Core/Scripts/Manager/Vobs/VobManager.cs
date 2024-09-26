using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Vobs;
using Debug = UnityEngine.Debug;

namespace GUZ.Core.Manager.Vobs
{
    public class VobManager : AbstractVobManager
    {
        private List<VirtualObjectType> _featureTypesToSpawn;


        public VobManager(GameConfiguration config)
        {
            FeatureEnableSounds = config.EnableGameSounds;
            FeatureEnableMusic = config.EnableGameMusic;
            FeatureShowFreePoints = config.ShowFreePoints;
            FeatureEnableNpcs = config.EnableNpcs;
            FeatureShowParticleEffects = config.EnableParticleEffects;
            FeatureShowDecals = config.EnableDecalVisuals;

            _featureTypesToSpawn = config.SpawnVOBTypes.Value.ToList();

            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldLoaded);
        }

        private void PostWorldLoaded()
        {
            GameContext.InteractionAdapter.SetTeleportationArea(TeleportParentGo);
        }

        public async Task CreateAsync(LoadingManager loading, List<IVirtualObject> vobs, GameObject rootGo)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            PreCreateVobs(vobs, rootGo);
            await CreateVobs(loading, vobs);
            PostCreateVobs();
            stopwatch.Stop();
            Debug.Log($"Created vobs in {stopwatch.Elapsed.TotalSeconds} s");
        }

        public GameObject GetRootGameObjectOfType(VirtualObjectType type)
        {
            GameObject retVal;

            if (ParentGosTeleport.TryGetValue(type, out retVal))
            {
                return retVal;
            }
            else if (ParentGosNonTeleport.TryGetValue(type, out retVal))
            {
                return ParentGosNonTeleport[type];
            }
            else
            {
                Debug.LogError($"No suitable root GO found for type >{type}<");
                return null;
            }
        }

        private void PreCreateVobs(List<IVirtualObject> vobs, GameObject rootGo)
        {
            // The elements are already created by glTF cache.
            TeleportParentGo = rootGo.transform.Find("Teleport").gameObject;
            NonTeleportParentGo = rootGo.transform.Find("NonTeleport").gameObject;

            TotalVObs = GetTotalVobCount(vobs);

            CreatedCount = 0;
            CullingVobObjects.Clear();
        }

        private void PostCreateVobs()
        {
            GameGlobals.VobMeshCulling.PrepareVobCulling(CullingVobObjects);

            FireTreeCache.ClearAndReleaseMemory();

            // TODO - warnings about "not implemented" - print them once only.
            foreach (var var in new[]
                     {
                         VirtualObjectType.zCVobScreenFX,
                         VirtualObjectType.zCVobAnimate,
                         VirtualObjectType.zCTriggerWorldStart,
                         VirtualObjectType.zCTriggerList,
                         VirtualObjectType.oCCSTrigger,
                         VirtualObjectType.oCTriggerScript,
                         VirtualObjectType.zCVobLensFlare,
                         VirtualObjectType.zCMoverController,
                         VirtualObjectType.zCPFXController
                     })
            {
                Debug.LogWarning($"{var} not yet implemented.");
            }
        }

        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return _featureTypesToSpawn.IsEmpty() || _featureTypesToSpawn.Contains(type);
        }

         protected override GameObject GetPrefab(IVirtualObject vob)
        {
            GameObject go;
            var name = vob.Name;

            switch (vob.Type)
            {
                case VirtualObjectType.oCItem:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
                    break;
                case VirtualObjectType.zCVobSpot:
                case VirtualObjectType.zCVobStartpoint:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSpot);
                    break;
                case VirtualObjectType.zCVobSound:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSound);
                    break;
                case VirtualObjectType.zCVobSoundDaytime:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSoundDaytime);
                    break;
                case VirtualObjectType.oCZoneMusic:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobMusic);
                    break;
                case VirtualObjectType.oCMOB:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.Vob);
                    break;
                case VirtualObjectType.oCMobFire:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobFire);
                    break;
                case VirtualObjectType.oCMobInter:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobInteractable);
                    break;
                case VirtualObjectType.oCMobBed:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobBed);
                    break;
                case VirtualObjectType.oCMobWheel:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobWheel);
                    break;
                case VirtualObjectType.oCMobSwitch:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobSwitch);
                    break;
                case VirtualObjectType.oCMobDoor:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobDoor);
                    break;
                case VirtualObjectType.oCMobContainer:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobContainer);
                    break;
                case VirtualObjectType.oCMobLadder:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobLadder);
                    break;
                case VirtualObjectType.zCVobAnimate:
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.VobAnimate);
                    break;
                default:
                    return new GameObject(name);
            }

            go.name = name;

            // Fill Property data into prefab here
            // Can also be outsourced to a proper method if it becomes a lot.
            go.GetComponent<VobProperties>().SetData(vob);

            return go;
        }

        protected override void AddToMobInteractableList(IVirtualObject vob, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            switch (vob.Type)
            {
                case VirtualObjectType.oCMOB:
                case VirtualObjectType.oCMobFire:
                case VirtualObjectType.oCMobInter:
                case VirtualObjectType.oCMobBed:
                case VirtualObjectType.oCMobDoor:
                case VirtualObjectType.oCMobContainer:
                case VirtualObjectType.oCMobSwitch:
                case VirtualObjectType.oCMobWheel:
                    var propertiesComponent = go.GetComponent<VobProperties>();

                    if (propertiesComponent == null)
                    {
                        Debug.LogError($"VobProperties component missing on {go.name} ({vob.Type})");
                    }

                    GameData.VobsInteractable.Add(go.GetComponent<VobProperties>());
                    break;
            }
        }

    }
}
