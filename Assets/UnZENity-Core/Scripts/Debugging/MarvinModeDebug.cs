using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Debugging
{
    public class MarvinModeDebug : MonoBehaviour
    {
        [Tooltip("Marvin: insert ...")]
        public string itemName;
        public bool spawnItem;


        private void OnValidate()
        {
            if (spawnItem)
            {
                spawnItem = false;

                SpawnItem();
            }
        }

        private void SpawnItem()
        {
            var item = VmInstanceManager.TryGetItemData(itemName);

            if (item == null)
            {
                Debug.LogError($"Item >{itemName}< not found.");
                return;
            }

            var cameraGo = Camera.main!.gameObject;
            var itemPrefab = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(item.Visual);
            var itemGo = MeshFactory.CreateVob(item.Visual, mrm, default, default, true,
                rootGo: itemPrefab, useTextureArray: false);

            itemGo.transform.position = cameraGo.transform.position;

            // Move ItemGO 1m to the front based on camera GO's rotation
            itemGo.transform.localPosition += cameraGo.transform.forward * 1f;
            itemGo.transform.localPosition += cameraGo.transform.up * -1f;

            itemGo.GetComponent<VobItemProperties>().SetData(null, item);
        }
    }
}
