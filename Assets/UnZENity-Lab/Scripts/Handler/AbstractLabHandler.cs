﻿using GUZ.Core;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public abstract class AbstractLabHandler : MonoBehaviour
    {
        public abstract void Bootstrap();
        
        
        protected void SpawnInteractable(string mdlName, PrefabType type, GameObject parentGo, Vector3 position = default, Quaternion rotation = default)
        {
            var prefab = ResourceLoader.TryGetPrefabObject(type);
            var mdl = ResourceLoader.TryGetModel(mdlName);

            if (mdl == null)
            {
                Debug.LogError("LabInteractableHandler: Element has no .mdl file: " + mdlName);
                return;
            }

            MeshFactory.CreateVob(mdlName, mdl, position, rotation,
                rootGo: prefab, parent: parentGo, useTextureArray: false);
        }
        
        protected GameObject SpawnItem(string itemName, GameObject parentGo, Vector3 position = default)
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
            var item = VmInstanceManager.TryGetItemData(itemName);
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            var itemGo = MeshFactory.CreateVob(item.Visual, mrm, position, default, true,
                rootGo: itemPrefab, parent: parentGo, useTextureArray: false);

            itemGo.GetComponent<VobItemProperties>().SetData(null, item);

            return gameObject;
        }
    }
}