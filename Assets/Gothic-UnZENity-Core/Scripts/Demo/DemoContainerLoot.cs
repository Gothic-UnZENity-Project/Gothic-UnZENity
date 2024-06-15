using System;
using System.Collections.Generic;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GVR.Core;
using UnityEngine;

namespace GUZ.Core.Demo
{
	public class DemoContainerLoot : MonoBehaviour
	{
		public bool debugSpawnContentNow = false;

		private readonly char[] itemNameSeparators = { ';', ',' };
		private readonly char[] itemCountSeparators = { ':', '.' };
		
		
        [Serializable]
        public struct Content
        {
            public string name;
            public int amount;
        }
        public List<Content> content = new();

		private void Update()
		{
			if (debugSpawnContentNow)
			{
				debugSpawnContentNow = false;

				SpawnContent();
			}
		}


		public void SetContent(string contents)
		{
			if (contents == string.Empty)
				return;

			var items = contents.Split(itemNameSeparators);

			foreach (var item in items)
			{
				var count = 1;
				var nameCountSplit = item.Split(itemCountSeparators);

				if (nameCountSplit.Length != 1)
				{
					count = int.Parse(nameCountSplit[1]);
				}

				content.Add(new() {
					name = nameCountSplit[0],
					amount = count
				});
			}
		}
		
		private void SpawnContent()
		{
			var itemsObj = new GameObject("Items");
			itemsObj.SetParent(gameObject, true);
			
			foreach (var item in content)
			{
				var itemInstance = AssetCache.TryGetItemData(item.name);

				var mrm = ResourceLoader.TryGetMultiResolutionMesh(itemInstance.Visual);
				var itemObj = MeshFactory.CreateVob(item.name, mrm, default, default, true, itemsObj);
			}
		}
	}
}
