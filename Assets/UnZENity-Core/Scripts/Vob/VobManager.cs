using System;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using Object = UnityEngine.Object;

namespace GUZ.Core.Vob
{
    public class VobManager
    {
        /// <summary>
        /// First time a VOB is made visible: Create it.
        /// </summary>
        public void InitVob(GameObject go)
        {
            go.TryGetComponent(out VobLoader loaderComp);

            if (loaderComp == null || loaderComp.IsLoaded)
            {
                return;
            }

            loaderComp.IsLoaded = true;

            var vob = loaderComp.Vob;
            switch (loaderComp.Vob.Type)
            {
                case VirtualObjectType.oCItem:
                    CreateItem((Item)vob, go);
                    break;
            }
        }

        /// <summary>
        /// To save memory, we can also Destroy Vobs and their Mesh+GO structure.
        /// </summary>
        public void DestroyVob(GameObject go)
        {
            throw new NotImplementedException();
        }


        private void CreateItem(Item vob, GameObject parent = null)
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
                return;
            }

            var prefabInstance = GetPrefab(vob);
            var vobObj = CreateItemMesh(item, prefabInstance, parent);

            if (vobObj == null)
            {
                Object.Destroy(prefabInstance); // No mesh created. Delete the prefab instance again.
                Debug.LogError(
                    $"There should be no! object which can't be found n:{vob.Name} i:{vob.Instance}. We need to use >PxVobItem.instance< to do it right!");
                return;
            }

            vobObj.GetComponent<VobItemProperties>().SetData(vob, item);
        }

        private GameObject GetPrefab(IVirtualObject vob)
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
                case VirtualObjectType.oCZoneMusicDefault:
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
                    go = ResourceLoader.TryGetPrefabObject(PrefabType.Vob);
                    break;
            }

            go!.name = name;

            // Fill Property data into prefab here
            // Can also be outsourced to a proper method if it becomes a lot.
            go.GetComponent<VobProperties>().SetData(vob);

            return go;
        }

        private GameObject CreateItemMesh(ItemInstance item, GameObject go, GameObject parent)
        {
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);

            if (mrm != null)
            {
                return MeshFactory.CreateVob(item.Visual, mrm, parent: parent, rootGo: go, useTextureArray: false);
            }

            // shortbow (itrw_bow_l_01) has no mrm, but has mmb
            var mmb = ResourceLoader.TryGetMorphMesh(item.Visual);

            return MeshFactory.CreateVob(item.Visual, mmb, parent: parent, rootGo: go);
        }
    }
}
