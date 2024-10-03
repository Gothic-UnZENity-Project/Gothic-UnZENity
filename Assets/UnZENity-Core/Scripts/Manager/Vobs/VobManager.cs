using System;
using System.Collections.Generic;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
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

        protected override void PreCreateVobs(List<IVirtualObject> vobs, GameObject rootGo)
        {
            // The elements are already created by static cache.
            TeleportParentGo = rootGo.FindChildRecursively("Teleport").gameObject;
            NonTeleportParentGo = rootGo.FindChildRecursively("NonTeleport").gameObject;

            CreateParentVobStructure();

            TotalVObs = GetTotalVobCount(vobs);

            CreatedCount = 0;
            CullingVobObjects.Clear();
        }

        private void PostWorldLoaded()
        {
            GameContext.InteractionAdapter.SetTeleportationArea(TeleportParentGo);
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

        private void CreateParentVobStructure()
        {
            foreach (var child in TeleportParentGo.GetAllDirectChildren())
            {
                Enum.TryParse(child.name, out VirtualObjectType type);

                ParentGosTeleport.Add(type, child);
            }

            foreach (var child in NonTeleportParentGo.GetAllDirectChildren())
            {
                Enum.TryParse(child.name, out VirtualObjectType type);

                ParentGosNonTeleport.Add(type, child);
            }
        }

        protected override void PostCreateVobs()
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
