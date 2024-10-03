using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GUZ.Core.Manager.Vobs
{
    public class VobManager : AbstractVobManager
    {
        public VobManager(GameConfiguration config)
        {
            FeatureEnableSounds = config.EnableGameSounds;
            FeatureEnableMusic = config.EnableGameMusic;
            FeatureShowFreePoints = config.ShowFreePoints;
            FeatureEnableNpcs = config.EnableNpcs;
            FeatureShowParticleEffects = config.EnableParticleEffects;
            FeatureShowDecals = config.EnableDecalVisuals;

            GlobalEventDispatcher.WorldSceneLoaded.AddListener(PostWorldLoaded);
        }

        public Dictionary<VirtualObjectType, Queue<GameObject>> CachedGOs = new();


        private void PostWorldLoaded()
        {
            GameContext.InteractionAdapter.SetTeleportationArea(TeleportParentGo);
        }

        public async Task CreateAsync(LoadingManager loading, List<IVirtualObject> vobs, GameObject rootGo)
        {
            var stopwatch = Stopwatch.StartNew();

            PreCreateVobs(vobs, rootGo);

            /*
             * VOBs are created in three flavours:
             * 1. If an object is from cache, we load its vobType prefab and apply the (already loaded) mesh on it.
             *   HINT: We need to apply VobData on the cached mesh vobs one-by-one.
             *         e.g. a door might be locked or unlocked. We need to apply this data on the correct items.
             * 2. If the object is new, we create it with Prefab and create a new GameObject
             */
            await CreateVobs(loading, vobs);
            PostCreateVobs();

            stopwatch.Log("Vobs created");
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
            // The elements are already created by static cache.
            TeleportParentGo = rootGo.FindChildRecursively("Teleport").gameObject;
            NonTeleportParentGo = rootGo.FindChildRecursively("NonTeleport").gameObject;

            PreFillCachedGoList();
            TotalVObs = GetTotalVobCount(vobs);

            CreatedCount = 0;
            CullingVobObjects.Clear();
        }

        /// <summary>
        /// As a result we get something like:
        ///   _cachedGOs = {
        ///     zCVob => [GameObject, GameObject, ...]
        ///     zCVobAnimate => [GameObject]
        ///      ...
        ///   }
        /// </summary>
        private void PreFillCachedGoList()
        {
            // We fill list of static cache GameObjects to apply prefab data onto it when looped.
            foreach (var rootGo in new[]{TeleportParentGo, NonTeleportParentGo})
            {
                foreach (var vobTypeGOs in rootGo.GetAllDirectChildren())
                {
                    Enum.TryParse(vobTypeGOs.name, out VirtualObjectType type); // Name of GameObject == a VobType enum label
                    CachedGOs.Add(type, new Queue<GameObject>()); // Init

                    foreach (var cachedGo in vobTypeGOs.GetAllDirectChildren())
                    {
                        CachedGOs[type].Enqueue(cachedGo);
                    }
                }
            }
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

        /// <summary>
        /// 90% of all VOBs with meshes are loaded already (except Items).
        /// Let's not filter anything out as it is super fast to load from here on.
        /// </summary>
        protected override bool SpawnObjectType(VirtualObjectType type)
        {
            return true;
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

        [CanBeNull]
        protected override GameObject CreateItem(Item vob, GameObject parent = null)
        {
            string itemName;

            if (!string.IsNullOrEmpty(vob.Instance))
            {
                itemName = vob.Instance;
            }
            else if (!string.IsNullOrEmpty(vob.Name))
            {
                itemName = vob.Name;
            }
            else
            {
                throw new Exception("Vob Item -> no usable name found.");
            }

            var item = VmInstanceManager.TryGetItemData(itemName);

            if (item == null)
            {
                return null;
            }

            var prefabInstance = GetPrefab(vob);
            var vobObj = CreateItemMesh(vob, item, prefabInstance, parent);

            if (vobObj == null)
            {
                Object.Destroy(prefabInstance); // No mesh created. Delete the prefab instance again.
                Debug.LogError(
                    $"There should be no! object which can't be found n:{vob.Name} i:{vob.Instance}. We need to use >PxVobItem.instance< to do it right!");
                return null;
            }

            vobObj.GetComponent<VobItemProperties>().SetData(vob, item);

            return vobObj;
        }

        public void CreateItemMesh(int itemId, string spawnPoint, GameObject go)
        {
            var item = VmInstanceManager.TryGetItemData(itemId);

            var position = WayNetHelper.GetWayNetPoint(spawnPoint).Position;

            CreateItemMesh(item, go, position);
        }

        /// <summary>
        /// Create item with mesh only. No special handling like grabbing etc.
        /// e.g. used for NPCs drinking beer mesh in their hand.
        /// </summary>
        public void CreateItemMesh(int itemId, GameObject go)
        {
            var item = VmInstanceManager.TryGetItemData(itemId);

            CreateItemMesh(item, go);
        }

        /// <summary>
        /// Create item with mesh only. No special handling like grabbing etc.
        /// e.g. used for NPCs drinking beer mesh in their hand.
        /// </summary>
        public void CreateItemMesh(string itemName, GameObject go)
        {
            if (itemName == "")
            {
                return;
            }

            var item = VmInstanceManager.TryGetItemData(itemName);

            CreateItemMesh(item, go);
        }

        private GameObject CreateItemMesh(Item vob, ItemInstance item, GameObject go, GameObject parent = null)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);

            if (mrm != null)
            {
                return MeshFactory.CreateVob(item.Visual, mrm, vob.Position.ToUnityVector(),
                    vob.Rotation.ToUnityQuaternion(),
                    true, parent ?? ParentGosNonTeleport[vob.Type], go, false);
            }

            // shortbow (itrw_bow_l_01) has no mrm, but has mmb
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);

            return MeshFactory.CreateVob(item.Visual, mmb, vob.Position.ToUnityVector(),
                vob.Rotation.ToUnityQuaternion(), parent ?? ParentGosNonTeleport[vob.Type], go);
        }

        private GameObject CreateItemMesh(ItemInstance item, GameObject parentGo, Vector3 position = default)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            if( mrm != null )
                return MeshFactory.CreateVob(item.Visual, mrm, position, default, false, parentGo, useTextureArray: false);

            // shortbow (itrw_bow_l_01) has no mrm, but has mmb
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);

            return MeshFactory.CreateVob(item.Visual, mmb, position,
                default, parentGo);
        }
    }
}
