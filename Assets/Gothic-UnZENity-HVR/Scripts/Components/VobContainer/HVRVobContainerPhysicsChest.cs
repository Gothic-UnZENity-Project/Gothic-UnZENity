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
        
        [Separator("GUZ - Settings")]
        [SerializeField] private VobContainerProperties _containerProperties;
        [SerializeField] private HVRSocketContainer _socketContainer;
        
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
