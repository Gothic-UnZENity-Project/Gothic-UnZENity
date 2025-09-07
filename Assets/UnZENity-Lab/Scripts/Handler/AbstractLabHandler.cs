using GUZ.Core;
using GUZ.Core.Core.Logging;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Meshes;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Util;
using Reflex.Attributes;
using UnityEngine;
using Logger = GUZ.Core.Core.Logging.Logger;

namespace GUZ.Lab.Handler
{
    public abstract class AbstractLabHandler : MonoBehaviour
    {
        public abstract void Bootstrap();


        [Inject] protected readonly VmCacheService VmCacheService;
        [Inject] protected readonly MeshService MeshService;
        [Inject] protected readonly VobService VobService;
        [Inject] protected readonly GameStateService GameStateService;


        protected GameObject SpawnInteractable(string mdlName, PrefabType type, GameObject parentGo, Vector3 position = default, Quaternion rotation = default)
        {
            var prefab = ResourceLoader.TryGetPrefabObject(type);
            var mdl = ResourceLoader.TryGetModel(mdlName);

            if (mdl == null)
            {
                Logger.LogError("LabInteractableHandler: Element has no .mdl file: " + mdlName, LogCat.Debug);
                return null;
            }

            return MeshService.CreateVob(mdlName, mdl, position, rotation,
                rootGo: prefab, parent: parentGo, useTextureArray: false);
        }
        
        protected GameObject SpawnItem(string itemName, GameObject parentGo, Vector3 position = default, PrefabType type = PrefabType.VobItem)
        {
            var itemPrefab = ResourceLoader.TryGetPrefabObject(type);
            var item = VmCacheService.TryGetItemData(itemName);
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            var itemGo = MeshService.CreateVob(item.Visual, mrm, position, default, true,
                rootGo: itemPrefab, parent: parentGo, useTextureArray: false);

            return itemGo;
        }
    }
}
