#if GUZ_HVR_INSTALLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.HurricaneVR.Framework.Shared.Utilities;
using GUZ.Core;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Manager;
using GUZ.Core.Models.Vm;
using GUZ.Core.Models.Vob;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.Vm;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using GUZ.VR.Services;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core.Utils;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.VR.Adapters.Player
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        [SerializeField] private TMP_Text _categoryText;
        [SerializeField] private TMP_Text _pagerText;
        [SerializeField] private HVRSocketContainer _socketContainer;

        private int _currentPage = 1;
        private int _totalPages;
        private VmGothicEnums.InvCats _selectedCategory =  VmGothicEnums.InvCats.InvWeapon;
        private bool _tempIgnoreSocketing;
        
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly PlayerService _playerService;
        [Inject] private readonly VRPlayerService _vrPlayerService;
        [Inject] private readonly VobService _vobService;
        [Inject] private readonly VobMeshCullingService _vobMeshCullingService;
        [Inject] private readonly SaveGameService _saveGameService;
        [Inject] private readonly VmService _vmService;

        
        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = _audioService.CreateAudioClip(_audioService.InvOpen.File);
            socketable.SocketedClip = _audioService.CreateAudioClip(_audioService.InvClose.File);
        }

        public void OnShoulderGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabber is not HVRShoulderSocket)
                return;

            _currentPage = 1;
            ClearSockets();
        }
        
        public void OnShoulderReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (grabber is not HVRShoulderSocket)
                return;

            UpdateInventoryView();
        }

        public void OnItemPutIntoBackpack(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_tempIgnoreSocketing)
                return;
            
            var vobLoader = grabbable.GetComponentInParent<VobLoader>(true);
            var vobContainer = vobLoader.Container;

            _vobMeshCullingService.RemoveCullingEntry(vobContainer);
            _saveGameService.CurrentWorldData.Vobs.Remove(vobContainer.Vob);

            _playerService.AddItem(vobContainer.Vob.Name, vobContainer.VobAs<IItem>().Amount);
            
            UpdateInventoryView();
        }

        public void OnItemPutOutOfBackpack(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (_tempIgnoreSocketing)
                return;
            
            var vobLoader = grabbable.GetComponentInParent<VobLoader>();
            var vobContainer = vobLoader.Container;

            _vobMeshCullingService.AddCullingEntry(vobContainer);
            _saveGameService.CurrentWorldData.Vobs.Add(vobContainer.Vob);

            // Keep commented out: Item will be put "out of inventory" when we release it from hand later.
            // _playerService.RemoveItem(vobContainer.Vob.Name, vobContainer.VobAs<IItem>().Amount);

            UpdateInventoryView();
        }
        
        public void OnPrevPageClick()
        {
            _currentPage--;
            if (_currentPage < 1)
                _currentPage = 1;

            UpdateInventoryView();
        }

        public void OnNextPageClick()
        {
            _currentPage++;

            UpdateInventoryView();
        }

        public void OnNextCategoryClick()
        {
            if (_selectedCategory >= VmGothicEnums.InvCats.InvMisc)
                return;

            _selectedCategory++;

            OnCategoryClick();
        }

        public void OnPrevCategoryClick()
        {
            if (_selectedCategory <= VmGothicEnums.InvCats.InvWeapon)
                return;

            _selectedCategory--;

            OnCategoryClick();
        }

        private void OnCategoryClick()
        {
            _currentPage = 1;

            UpdateInventoryView();
        }

        private void UpdateCategoryText()
        {
            _categoryText.text = _vmService.InventoryCategories[(int)_selectedCategory];
        }

        private void UpdatePagerText(List<ContentItem> inventory)
        {
            _totalPages = Mathf.CeilToInt((float)inventory.Count / 9) + 1;
            if (_currentPage > _totalPages)
                _currentPage = _totalPages;

            _pagerText.text = $"{_currentPage}/{_totalPages}";
        }

        private IEnumerator UpdateSockets(List<ContentItem> inventory)
        {
            // While we re-stack items into slots, we need to ignore their events. Otherwise we create a loop.
            _tempIgnoreSocketing = true;
            
            ClearSockets();
            yield return null; // Releasing and destroying objects takes until next frame.

            var startIndex = _currentPage * 9 - 9;
            var count = Mathf.Min(9, inventory.Count - startIndex);
            var items = inventory.GetRange(startIndex, count);

            foreach (var item in items)
            {
                var vobContainer = _vobService.CreateItem(new Item
                {
                    Name = item.Name,
                    Visual = new VisualMesh(),
                    Instance = item.Name,
                    Amount = item.Amount
                });

                _socketContainer.TryAddGrabbable(vobContainer.Go.GetComponentInChildren<HVRGrabbable>());
            }
            
            _tempIgnoreSocketing = false;
        }

        private void ClearSockets()
        {
            foreach (var socket in _socketContainer.Sockets)
            {
                // Nothing inside socket
                if (!socket.IsGrabbing)
                    continue;

                // Destroy VobLoader GO after releasing it from slot (proper event handling)
                var heldRoot = socket.HeldObject.transform.parent.gameObject;
                socket.ForceRelease();
                heldRoot.SetActive(false); // Disable it, as it would be with scale 1 in front of our camera for 1 second.
                // We need to wait until next frame to ensure HVR has removed the item from socket.
                this.ExecuteNextUpdate(() => Destroy(heldRoot));
            }
        }
        
        private void UpdateInventoryView()
        {
            var inventory = _playerService.GetInventory(_selectedCategory);

            // Subtract amount of held items from inventory
            SubtractItemFromHand(inventory, _vrPlayerService.GrabbedItemLeft);
            SubtractItemFromHand(inventory, _vrPlayerService.GrabbedItemRight);
            
            UpdateCategoryText();
            UpdatePagerText(inventory);
            StartCoroutine(UpdateSockets(inventory));
        }

        private void SubtractItemFromHand(List<ContentItem> inventory, GameObject handItem)
        {
            var item = handItem?.GetComponentInParent<VobLoader>().Container.VobAs<IItem>();

            if (item == null)
                return;
            
            var matchingItem = inventory.FirstOrDefault(x => x.Name == item.Instance);

            // == nothing found
            if (matchingItem == null)
                return;
            
            matchingItem.Amount -= item.Amount;
            if (matchingItem.Amount <= 0)
            {
                inventory.Remove(matchingItem);
            }
        }
    }
}
#endif
