using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core;
using GUZ.VR.Components;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core;
using ZenKit.Daedalus;
using ZenKit.Vobs;
using GUZ.Core.Extensions;
using System;
using System.Linq;
using GUZ.Core.Util;
using GUZ.Core.Creator.Meshes;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine.Events;

namespace GUZ
{
    public class BackPackManager : MonoBehaviour
    {
        [SerializeField] private float _syncInterval = 0.2f;
        [SerializeField] private int _maxBackpackSlots = 20;

        // Static lists for debugging
        public static List<string> Items = new List<string>();
        public static Dictionary<int, int> ItemsId = new Dictionary<int, int>();
        public List<HVRGrabbable> Grabbables = new List<HVRGrabbable>();

        public List<string> HeroInstanceItems = new List<string>();

        public GameObject MeshChild;
        public HVRSocketContainer SocketContainer;

        private float _lastSyncTime;
        private NpcInstance _heroInstance;
        private bool _isBackpackActive = false;

        private bool _wasBackpackPreviouslyInactive = true;

        void Start()
        {
            // Get hero instance reference once
            _heroInstance = GameManager.I.Npcs.GetHeroContainer().Instance;
        }

        public void OnEnable()
        {
            // Reset flag when object is enabled
            _isBackpackActive = true;
            gameObject.GetComponent<BackPackManager>().enabled = true;
        }

        public void OnDisable()
        {
            _isBackpackActive = false;
            gameObject.GetComponent<BackPackManager>().enabled = false;
        }

        void Update()
        {
            // Check if backpack mesh exists
            bool meshExists = CheckForBackpackMesh();

            if (meshExists && _isBackpackActive && _wasBackpackPreviouslyInactive)
            {
                _wasBackpackPreviouslyInactive = false;
                OnBackpackActivated();
            }
            else if (!meshExists && _isBackpackActive)
            {
                _isBackpackActive = false;
                _wasBackpackPreviouslyInactive = true;
            }

            // Only run periodic updates if backpack is active
            if (_isBackpackActive)
            {
                // Periodic sync between hero inventory and backpack
                if (Time.time - _lastSyncTime > _syncInterval)
                {
                    _lastSyncTime = Time.time;
                    
                    // Check if hero inventory has changed
                    bool heroInventoryChanged = CheckHeroInventoryChanged();
                    
                    // Track items before update
                    var previousItems = new Dictionary<int, int>(ItemsId);

                    // Update current contents
                    UpdateBackpackContents();

                    // Check for removed items
                    CheckForRemovedItems(previousItems);

                    // If hero inventory changed, sync from hero to backpack
                    if (heroInventoryChanged)
                    {
                        SyncHeroInventoryToBackpack();
                    }
                    else
                    {
                        // Otherwise sync from backpack to hero
                        SyncBackpackToHeroInventory();
                    }

                    var props = _heroInstance.GetUserData()?.Props;
                    if (props != null)
                    {
                        HeroInstanceItems = props.Items.Select(item => item.Key.ToString()).ToList();
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if hero inventory has changed since last update
        /// </summary>
        private bool CheckHeroInventoryChanged()
        {
            if (_heroInstance == null) return false;
            
            var props = _heroInstance.GetUserData()?.Props;
            if (props == null) return false;
            
            // Convert current hero inventory to a comparable format
            var currentHeroItems = new Dictionary<uint, int>();
            foreach (var item in props.Items)
            {
                currentHeroItems[item.Key] = item.Value;
            }
            
            // Compare with previous hero inventory
            if (HeroInstanceItems.Count != currentHeroItems.Count)
            {
                return true;
            }
            
            // Check if any item counts have changed
            foreach (var item in currentHeroItems)
            {
                if (!HeroInstanceItems.Contains(item.Key.ToString()))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Checks for items that were removed from the backpack and updates hero inventory
        /// </summary>
        private void CheckForRemovedItems(Dictionary<int, int> previousItems)
        {
            if (_heroInstance == null) return;

            var props = _heroInstance.GetUserData()?.Props;
            if (props == null) return;

            foreach (var prevItem in previousItems)
            {
                int itemId = prevItem.Key;
                int prevCount = prevItem.Value;

                // Check if item count decreased or item was removed completely
                if (!ItemsId.TryGetValue(itemId, out int currentCount) || currentCount < prevCount)
                {
                    int removedCount = currentCount < prevCount ? prevCount - currentCount : prevCount;

                    // Update hero inventory
                    if (props.Items.TryGetValue((uint)itemId, out int heroCount))
                    {
                        int newCount = heroCount - removedCount;
                        if (newCount <= 0)
                        {
                            // Remove item completely if count is zero or negative
                            props.Items.Remove((uint)itemId);
                            Debug.Log($"Removed item {itemId} from hero inventory");
                        }
                        else
                        {
                            // Update count
                            props.Items[(uint)itemId] = newCount;
                            Debug.Log($"Updated item {itemId} count to {newCount} in hero inventory");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the backpack mesh exists and updates references
        /// </summary>
        private bool CheckForBackpackMesh()
        {
            if (MeshChild == null)
            {
                MeshChild = transform.Find("Mesh")?.gameObject;
                if (MeshChild == null)
                {
                    MeshChild = transform.Find("BackPack.fbx")?.Find("Mesh")?.gameObject;
                }
            }

            if (SocketContainer == null && MeshChild != null)
            {
                SocketContainer = MeshChild.GetComponent<HVRSocketContainer>();
            }

            return MeshChild != null && SocketContainer != null;
        }

        /// <summary>
        /// Called when backpack is activated (taken out)
        /// </summary>
        private void OnBackpackActivated()
        {
            Debug.Log("Backpack activated!");

            SyncHeroInventoryToBackpack();
            UpdateBackpackContents();
        }

        public bool HasItem(int itemID, int amount = 1)
        {
            if (ItemsId.ContainsKey(itemID))
            {
                return ItemsId[itemID] >= amount;
            }
            return false;
        }

        void PopulateItemIds()
        {
            ItemsId.Clear();
            foreach (var item in Grabbables)
            {
                var itemProperties = item.GetComponent<VobItemProperties>();
                if (itemProperties == null) continue;

                var instanceName = itemProperties.Item.Index;
                var itemData = VmInstanceManager.TryGetItemData(instanceName);
                var itemID = itemData.Index;
                // var itemAmount = itemProperties.Amount == 0 ? 1 : itemProperties.Amount;
                // we need to somehow store, set and get the amount of the item in the backpack slot

                if (ItemsId.ContainsKey(itemID))
                {
                    ItemsId[itemID] += 1;
                }
                else
                {
                    ItemsId[itemID] = 1;
                }
            }
        }

        private void UpdateBackpackContents()
        {
            Items.Clear();

            if (SocketContainer == null) return;

            List<HVRGrabbable> Listgrabbables = new List<HVRGrabbable>();

            // go through the sockets and see if there is an item in it
            foreach (var socket in SocketContainer.Sockets)
            {
                if (socket.HeldObject != null)
                {
                    Listgrabbables.Add(socket.HeldObject);
                    var item = socket.HeldObject;
                    var itemID = item.name;
                    if (!Items.Contains(itemID))
                    {
                        Items.Add(itemID);
                    }
                }
            }

            Grabbables = Listgrabbables;
            PopulateItemIds();
        }

        /// <summary>
        /// Synchronizes the hero's inventory to the backpack by adding missing items and removing items that are no longer in the inventory
        /// </summary>
        public void SyncHeroInventoryToBackpack()
        {
            if (SocketContainer == null) return;

            _heroInstance ??= GameManager.I.Npcs.GetHeroContainer().Instance;

            var props = _heroInstance.GetUserData()?.Props;
            if (props == null) return;

            // Get current backpack items
            UpdateBackpackContents();
            
            // First, remove items from backpack that are not in hero inventory
            RemoveItemsNotInHeroInventory(props);

            // Temporarily disable audio for sockets
            PreSpawnContent(out AudioClip grabAudio, out AudioClip releaseAudio);

            // Check hero inventory for items not in backpack
            foreach (var inventoryItem in props.Items)
            {
                int itemID = (int)inventoryItem.Key;
                int amount = inventoryItem.Value;

                // Skip if item is already in backpack
                if (ItemsId.ContainsKey(itemID) && ItemsId[itemID] >= amount) continue;

                // Calculate how many to add
                int amountToAdd = amount;
                if (ItemsId.ContainsKey(itemID))
                {
                    amountToAdd = amount - ItemsId[itemID];
                }

                // Add items to backpack
                for (int i = 0; i < amountToAdd; i++)
                {
                    if (!AddItemToBackpack(itemID))
                    {
                        Debug.LogWarning($"Failed to add item {itemID} to backpack - no available slots");
                        break;
                    }
                }
            }
            
            // Wait a moment and restore audio
            StartCoroutine(PostSpawnContent(grabAudio, releaseAudio));
        }
        
        /// <summary>
        /// Removes items from backpack that are not in hero inventory or have too many copies
        /// </summary>
        private void RemoveItemsNotInHeroInventory(NpcProperties props)
        {
            // Create a copy of ItemsId to avoid modification during iteration
            var backpackItems = new Dictionary<int, int>(ItemsId);
            
            foreach (var backpackItem in backpackItems)
            {
                int itemID = backpackItem.Key;
                int backpackAmount = backpackItem.Value;
                
                // Check if item exists in hero inventory
                if (!props.Items.TryGetValue((uint)itemID, out int heroAmount))
                {
                    // Item not in hero inventory, remove all from backpack
                    RemoveItemFromBackpack(itemID, backpackAmount);
                    Debug.Log($"Removed item {itemID} from backpack (not in hero inventory)");
                }
                else if (backpackAmount > heroAmount)
                {
                    // Too many copies in backpack, remove excess
                    int excessAmount = backpackAmount - heroAmount;
                    RemoveItemFromBackpack(itemID, excessAmount);
                    Debug.Log($"Removed {excessAmount} excess copies of item {itemID} from backpack");
                }
            }
        }

        /// <summary>
        /// Temporarily disables audio for sockets during item spawning
        /// </summary>
        private void PreSpawnContent(out AudioClip grabAudio, out AudioClip releaseAudio)
        {
            // We assume that all Slots have the same sound. Therefore fetching first only.
            var firstSocket = SocketContainer.Sockets.FirstOrDefault();
            grabAudio = firstSocket != null ? firstSocket.AudioGrabbedFallback : null;
            releaseAudio = firstSocket != null ? firstSocket.AudioReleasedFallback : null;

            foreach (var socket in SocketContainer.Sockets)
            {
                socket.AudioGrabbedFallback = null;
                socket.AudioReleasedFallback = null;
            }
        }

        /// <summary>
        /// Restores audio for sockets after items have been spawned
        /// </summary>
        private IEnumerator PostSpawnContent(AudioClip grabAudio, AudioClip releaseAudio)
        {
            // We wait for some time to ensure the objects are automatically snapped into place.
            yield return new WaitForSeconds(1f);

            foreach (var socket in SocketContainer.Sockets)
            {
                socket.AudioGrabbedFallback = grabAudio;
                socket.AudioReleasedFallback = releaseAudio;
            }
        }

        /// <summary>
        /// Synchronizes the backpack contents to the hero's inventory
        /// </summary>
        public void SyncBackpackToHeroInventory()
        {
            _heroInstance ??= GameManager.I.Npcs.GetHeroContainer().Instance;

            var props = _heroInstance.GetUserData()?.Props;
            if (props == null) return;

            // Update current backpack contents
            UpdateBackpackContents();

            // Update hero inventory based on backpack contents
            // IMPORTANT: Don't clear props.Items as it's the source of truth
            foreach (var item in ItemsId)
            {
                // Update item count in hero inventory
                props.Items[(uint)item.Key] = item.Value;
            }
            
            // Check for items in hero inventory that are not in backpack
            // and should be removed from hero inventory
            List<uint> itemsToRemove = new List<uint>();
            
            foreach (var heroItem in props.Items)
            {
                uint itemID = heroItem.Key;
                
                // If item is not in backpack, mark for removal
                if (!ItemsId.ContainsKey((int)itemID))
                {
                    itemsToRemove.Add(itemID);
                }
            }
            
            // Remove items from hero inventory
            foreach (var itemID in itemsToRemove)
            {
                props.Items.Remove(itemID);
                Debug.Log($"Removed item {itemID} from hero inventory (not in backpack)");
            }
        }

        /// <summary>
        /// Adds an item to the backpack if there's an available socket
        /// </summary>
        /// <param name="itemID">The item ID to add</param>
        /// <returns>True if item was added successfully</returns>
        public bool AddItemToBackpack(int itemID)
        {
            if (SocketContainer == null) return false;

            // Find an available socket
            var availableSocket = SocketContainer.Sockets.FirstOrDefault(s => s.HeldObject == null);
            if (availableSocket == null) return false;

            // Create the item
            var itemInstance = VmInstanceManager.TryGetItemData(itemID);
            if (itemInstance == null) return false;

            // Create item GameObject
            var itemGO = CreateItemGameObject(itemInstance);
            if (itemGO == null) return false;

            // Place in socket
            PlaceObjectIntoContainer(itemGO);
            return true;
        }

        /// <summary>
        /// Creates a GameObject for an item instance
        /// </summary>
        private GameObject CreateItemGameObject(ItemInstance itemInstance)
        {
            try
            {
                // Get prefab object first
                var go = ResourceLoader.TryGetPrefabObject(PrefabType.VobItem);

                // Set item data
                var itemProps = go.GetComponent<VobItemProperties>();
                if (itemProps != null)
                {
                    itemProps.SetData(null, itemInstance);
                }

                // Get mesh for the item
                var mrm = ResourceLoader.TryGetMultiResolutionMesh(itemInstance.Visual);

                // Create the visual representation with proper configuration
                var itemGO = MeshFactory.CreateVob(
                    itemInstance.Name,
                    mrm,
                    default,
                    default,
                    true,
                    rootGo: go,
                    useTextureArray: false
                );

                // Make sure the item has a grabbable component
                if (!itemGO.GetComponentInChildren<HVRGrabbable>(true))
                {
                    Debug.LogWarning($"Item {itemInstance.Name} doesn't have HVRGrabbable component, adding one");
                    var grabbable = itemGO.AddComponent<HVRGrabbable>();
                    // Configure basic grabbable settings
                    grabbable.ForceGrabbable = true;
                }

                return itemGO;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create item GameObject: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Places an object into the backpack container
        /// </summary>
        private void PlaceObjectIntoContainer(GameObject itemGo)
        {
            var grabbable = itemGo.GetComponentInChildren<HVRGrabbable>(true);

            if (grabbable == null)
            {
                Debug.LogError($"No HVRGrabbable found on {itemGo.name}, cannot place in container");
                return;
            }

            // Wait 1 frame to ensure mesh bounds can be calculated by HVR Socket
            StartCoroutine(PlaceObjectWithDelay(grabbable));
        }

        private IEnumerator PlaceObjectWithDelay(HVRGrabbable grabbable)
        {
            // Wait one frame for HVR to initialize properly
            yield return null;

            if (SocketContainer.TryFindAvailableSocket(grabbable, out var socket))
            {
                socket.TryGrab(grabbable, true, true);
                grabbable.transform.localPosition = Vector3.zero; // Reset position
            }
            else
            {
                Debug.LogWarning($"No available socket found for {grabbable.name}");
            }
        }

        /// <summary>
        /// Removes an item from the backpack
        /// </summary>
        public bool RemoveItemFromBackpack(int itemID, int amount = 1)
        {
            if (!HasItem(itemID, amount)) return false;

            int removed = 0;
            List<HVRGrabbable> itemsToRemove = new List<HVRGrabbable>();

            // Find items to remove
            foreach (var grabbable in Grabbables)
            {
                if (removed >= amount) break;

                var itemProperties = grabbable.GetComponent<VobItemProperties>();
                if (itemProperties == null) continue;

                var instanceName = itemProperties.Item.Index;
                var itemData = VmInstanceManager.TryGetItemData(instanceName);

                if (itemData.Index == itemID)
                {
                    itemsToRemove.Add(grabbable);
                    removed++;
                }
            }

            // Remove items
            foreach (var item in itemsToRemove)
            {
                var socket = SocketContainer.Sockets.FirstOrDefault(s => s.HeldObject == item);
                if (socket != null)
                {
                    socket.ForceRelease();
                    Destroy(item.gameObject);
                }
            }

            // Update backpack contents
            UpdateBackpackContents();
            return true;
        }
    }
}