using System;
using System.Collections.Generic;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Vm;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Core.Demo
{
    public class DemoContainerLoot : MonoBehaviour
    {
        [FormerlySerializedAs("debugSpawnContentNow")]
        public bool DebugSpawnContentNow;

        private readonly char[] _itemNameSeparators = { ';', ',' };
        private readonly char[] _itemCountSeparators = { ':', '.' };


        [Serializable]
        public struct ContentItem
        {
            [FormerlySerializedAs("name")] public string Name;
            [FormerlySerializedAs("amount")] public int Amount;
        }

        [FormerlySerializedAs("content")] public List<ContentItem> Content = new();

        private void Update()
        {
            if (DebugSpawnContentNow)
            {
                DebugSpawnContentNow = false;

                SpawnContent();
            }
        }


        public void SetContent(string contents)
        {
            if (contents == string.Empty)
            {
                return;
            }

            var items = contents.Split(_itemNameSeparators);

            foreach (var item in items)
            {
                var count = 1;
                var nameCountSplit = item.Split(_itemCountSeparators);

                if (nameCountSplit.Length != 1)
                {
                    count = int.Parse(nameCountSplit[1]);
                }

                Content.Add(new ContentItem
                {
                    Name = nameCountSplit[0],
                    Amount = count
                });
            }
        }

        private void SpawnContent()
        {
            var itemsObj = new GameObject("Items");
            itemsObj.SetParent(gameObject, true);

            foreach (var item in Content)
            {
                var itemInstance = VmInstanceManager.TryGetItemData(item.Name);

                var mrm = ResourceLoader.TryGetMultiResolutionMesh(itemInstance.Visual);
                var itemObj = MeshFactory.CreateVob(item.Name, mrm, default, default, true, itemsObj);
            }
        }
    }
}
