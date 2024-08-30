#if GUZ_HVR_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Sockets;
using MyBox;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.HVR.Components.VobContainer
{
    public class HVRVobContainerPhysicsChest : HVRPhysicsDoor
    {
        private readonly char[] _itemNameSeparators = { ';', ',' };
        private readonly char[] _itemCountSeparators = { ':', '.' };

        // Need to be updated if ocMobContainer.prefab is changed.
        private const int _socketRows = 3;
        private const int _socketsPerRow = 4;
        private const float _socketRadius = 0.1f;
        private const float _socketCollectionMargin = _socketRadius / 2;

        [Separator("GUZ - Settings")]
        [SerializeField] private GameObject _rootGo;
        [SerializeField] private VobContainerProperties _containerProperties;
        [SerializeField] private HVRSocketContainer _socketContainer;
        [SerializeField] private BoxCollider _collectorCollider;

        // Flags to ensure we always call On*() once, everytime open/close status changes.
        private bool _openingForTheFirstTime = true;
        private bool _onOpenedHandled;
        private bool _onClosedHandled;
        
        
        [Serializable]
        public struct ContentItem
        {
            public string Name;
            public int Amount;
        }
        
        
        public override void Start()
        {
            base.Start();
            
            _socketContainer.gameObject.SetActive(false);
        }
        
        protected override void Update()
        {
            base.Update();

            if (Opened && !_onOpenedHandled)
            {
                OnOpened();
            }

            if (Closed && !_onClosedHandled)
            {
                OnClosed();
            }
        }
        
        private void OnOpened()
        {
            if (_openingForTheFirstTime)
            {
                AlignSockets();
                InitializeContent();
                _openingForTheFirstTime = false;
            }
            
            // Render items if opened for the first time.
            _socketContainer.gameObject.SetActive(true);
            
            _onOpenedHandled = true;
            _onClosedHandled = false;
        }
        
        private void OnClosed()
        {
            _socketContainer.gameObject.SetActive(false);
            
            _onOpenedHandled = false;
            _onClosedHandled = true;
        }

        /// <summary>
        /// Containers have different sizes. Re-align the prefab's HVR sockets to match their actual sizes.
        /// </summary>
        private void AlignSockets()
        {
            var sockets = _socketContainer.Sockets;

            // We need to dynamically fetch the bounding box of main mesh as some chests have the correct one inside BIP01 "CHEST_*_0" and others inside "ZM_CHEST*"
            var meshFilters = _rootGo
                .GetComponentsInChildren<MeshFilter>() // Find all meshes
                .Where(i => !i.TryGetComponent(typeof(HVRGrabbable), out _)) // Filter out meshe, which is the lid
                .Where(i => i.gameObject.name.StartsWithIgnoreCase("ZM_") || i.gameObject.name.StartsWithIgnoreCase("BIP01")) // Use Gothic elements only. Ignore elements like HVR sockets.
                .Where(i => !i.gameObject.name.ContainsIgnoreCase("LOCK")) // Ignore lock element
                .ToArray();

            MeshFilter usedMesh;
            if (meshFilters.IsEmpty())
            {
                Debug.LogError($"No suitable mesh found for HVR Socket size calculation in {_rootGo.name}. Skipping...", _rootGo);
                return;
            }
            else if (meshFilters.Length >= 2)
            {
                Debug.LogWarning($"More than one feasible MeshFilter for HVR Socket size calculation found in {_rootGo.name}. Leveraging first (1)ZM_* or (2)BIP01 one.", _rootGo);

                // Based on mesh check of all containers in G1, the highest priority for the right mesh is a ZM_* GO if existing.
                var zmMesh = meshFilters.FirstOrDefault(i => i.gameObject.name.StartsWithIgnoreCase("ZM_"));
                usedMesh = zmMesh ?? meshFilters.First();
            }
            else
            {
                usedMesh = meshFilters.First();
            }
            
            var mainMeshBounds = usedMesh!.sharedMesh.bounds;
            
            // Set collectorCollider's size to perfectly align with container.
            _collectorCollider.size = new Vector3(mainMeshBounds.size.x, _collectorCollider.size.y, mainMeshBounds.size.z);
            
            // Move all Slots nearly towards height of container (*1,5 == 75% close to top).
            _socketContainer.transform.localPosition = new (0, mainMeshBounds.center.y * 1.5f, 0);

            var centeredMainBound = new Bounds(Vector3.zero, mainMeshBounds.size);
            
            // Align all sockets in row+column based on actual sizes.
            var rowWidth = (centeredMainBound.size.x - 2*_socketCollectionMargin) / _socketsPerRow;
            var rowDepth = (centeredMainBound.size.z - 2*_socketCollectionMargin) / _socketRows;
            
            // Calculate center of a grid-element for a socket ring entry.
            var middleAlignmentX = (rowWidth - _socketRadius * 2) / 2;
            var middleAlignmentZ = (rowDepth - _socketRadius * 2) / 2;
            
            // Loop variables
            var currentRow = 0;
            var currentColumn = 0;
            
            foreach (var socket in sockets)
            {
                // Calculation
                // _socketCollectionMargin - Left aligned margin for the whole collection
                // _socketRadius - Location is based on a 50% shift into the desired area as the drawing of a circle starts centered
                // middleAlignment* - Centered location within a grid for an element
                // min.x - Start point to calculate from left top
                socket.transform.localPosition = new (
                    _socketCollectionMargin + _socketRadius + middleAlignmentX + centeredMainBound.min.x + rowWidth * currentColumn,
                    0,
                    _socketCollectionMargin + _socketRadius + middleAlignmentZ + centeredMainBound.min.z + rowDepth * currentRow
                );

                currentColumn++;
                if (currentColumn >= _socketsPerRow)
                {
                    currentColumn = 0;
                    currentRow++;
                }
            }
        }
        
        private void InitializeContent()
        {
            var contents = _containerProperties.ContainerProperties?.Contents;

            if (string.IsNullOrEmpty(contents))
            {
                return;
            }

            var contentItems = ParseContent(contents);

            PreSpawnContent(out AudioClip grabAudio, out AudioClip releaseAudio);
            SpawnContent(contentItems);
            StartCoroutine(PostSpawnContent(grabAudio, releaseAudio));
        }
        
        private List<ContentItem> ParseContent(string contents)
        {
            List<ContentItem> result = new();
            
            var items = contents.Split(_itemNameSeparators);
        
            foreach (var item in items)
            {
                var count = 1;
                var nameCountSplit = item.Split(_itemCountSeparators);
        
                if (nameCountSplit.Length != 1)
                {
                    count = int.Parse(nameCountSplit[1]);
                }
        
                result.Add(new ContentItem
                {
                    Name = nameCountSplit[0],
                    Amount = count
                });
            }

            return result;
        }


        /// <summary>
        /// When we spawn items later, they will be automatically collected by the HVR Collector. It will put their meshes
        /// into place. But it will also play an audio file. We will temporarily remove this file until we're done adding the items.
        /// </summary>
        private void PreSpawnContent(out AudioClip grabAudio, out AudioClip releaseAudio)
        {
            // We assume that all Slots have the same sound. Therefore fetching first only.
            var firstSocket = _socketContainer.Sockets.First();
            grabAudio = firstSocket.AudioGrabbedFallback;
            releaseAudio = firstSocket.AudioReleasedFallback;
            
            foreach (var socket in _socketContainer.Sockets)
            {
                socket.AudioGrabbedFallback = null;
                socket.AudioReleasedFallback = null;
            }
        }

        private void SpawnContent(List<ContentItem> contentItems)
        {
            var sockets = _socketContainer.Sockets;

            if (contentItems.Count > sockets.Count)
            {
                Debug.LogError("There are more items in a container than HVRSockets available. Please implement pagination to ensure everything is visible and can be grabbed!");
            }

            for (var i = 0; i < Math.Min(contentItems.Count, sockets.Count); i++)
            {
                var currentItem = contentItems[i];
                var currentSocket = sockets[i];
                var itemInstance = VmInstanceManager.TryGetItemData(currentItem.Name);
                Debug.Log(itemInstance.Name);
                
                var go = TempCreateItem(itemInstance);

                // We need to parent our item to the normal Vob tree. Because putting it into a slot changes the parent to the slot-GO.
                // And when grabbed out of the chest, HVR will re-parent the object to it's original parent.
                // If the chest or sth. else ist the parent, a culling of the chest will hide the item (e.g. in our hand).
                var parentGo = VobCreator.GetRootGameObjectOfType(VirtualObjectType.oCItem);
                go.SetParent(parentGo);

                // With this, we move the object into the chest and the "auto-collector" will trigger and move the object to the first free Socket.
                // TODO - The order of items might be wrong if e.g. a huge sword with order-ID 0 is colliding with the chest and therefore
                //        sliding down too slow (due to collisions inside the chest) it might get another slot-ID.
                //        It's up to us if this is an issue or even worth a word.
                go.transform.position = currentSocket.transform.position;
            }
        }
        
        private IEnumerator PostSpawnContent(AudioClip grabAudio, AudioClip releaseAudio)
        {
            // We wait for some time to ensure the objects are automatically snapped into place.
            yield return new WaitForSeconds(1f);
            
            foreach (var socket in _socketContainer.Sockets)
            {
                socket.AudioGrabbedFallback = grabAudio;
                socket.AudioReleasedFallback = releaseAudio;
            }
        }
        
        /// <summary>
        /// FIXME - Only temporary function. In the future we need to create a new Item to use it (e.g.) for saving later.
        /// FIXME - For now we stick with the mesh without properties only.
        /// 1. Create a new ZenKit.Vob.Item object
        /// 2. Call VobCreator.CreateItem(vob, ...)
        /// </summary>
        private GameObject TempCreateItem(ItemInstance itemInstance)
        {
            var go = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);
            go.GetComponent<VobItemProperties>().SetData(null, itemInstance);
            
            var mrm = ResourceLoader.TryGetMultiResolutionMesh(itemInstance.Visual);
            return MeshFactory.CreateVob(itemInstance.Name, mrm, default, default, true, rootGo: go, useTextureArray: false);
        }
    }
}
#endif
