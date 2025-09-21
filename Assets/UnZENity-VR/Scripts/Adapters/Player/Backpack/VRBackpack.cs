#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using Assets.HurricaneVR.Framework.Shared.Utilities;
using GUZ.Core;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Manager;
using GUZ.Core.Models.Vm;
using GUZ.Core.Models.Vob;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core.Utils;
using MyBox;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenKit.Vobs;

namespace GUZ.VR.Adapters.Player.Backpack
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        [SerializeField] private TMP_Text _pagerText;
        [SerializeField] private HVRSocketContainer _socketContainer;
        [SerializeField] private RawImage[] _categoryImages;

        private int _currentPage = 1;
        private int _totalPages;
        private VmGothicEnums.ItemFlags _selectedCategory =  VmGothicEnums.ItemFlags.ItemKatNf;

        
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly PlayerService _playerService;
        [Inject] private readonly VobService _vobService;
        [Inject] private readonly VobMeshCullingService _vobMeshCullingService;
        [Inject] private readonly SaveGameService _saveGameService;

        
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

            var inventory = _playerService.GetInventory(_selectedCategory);
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        public void OnItemPutIntoBackpack(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // Put out means a hand is grabbing (not the Socket of backpack itself.
            if (grabber is not HVRHandGrabber)
                return;

            var vobLoader = grabbable.GetComponentInParent<VobLoader>();
            var vobContainer = vobLoader.Container;

            _vobMeshCullingService.RemoveCullingEntry(vobContainer);
            _saveGameService.CurrentWorldData.Vobs.Remove(vobContainer.Vob);
        }

        public void OnItemPutOutOfBackpack(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // Put out means a hand is grabbing (not the Socket of backpack itself.
            if (grabber is not HVRHandGrabber)
                return;
            
            var vobLoader = grabbable.GetComponentInParent<VobLoader>();
            var vobContainer = vobLoader.Container;

            _vobMeshCullingService.AddCullingEntry(vobContainer);
            _saveGameService.CurrentWorldData.Vobs.Add(vobContainer.Vob);
        }

        public void OnPrevPageClick()
        {
            _currentPage--;
            if (_currentPage < 1)
                _currentPage = 1;

            var inventory = _playerService.GetInventory(_selectedCategory);
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        public void OnNextPageClick()
        {
            _currentPage++;

            var inventory = _playerService.GetInventory(_selectedCategory);
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        public void OnCategoryClicked(VmGothicEnums.ItemFlags category)
        {
            if (category == _selectedCategory)
                return;
            
            _selectedCategory = category;

            _categoryImages.ForEach(i => i.color = Color.black);

            var inventory = _playerService.GetInventory(_selectedCategory);
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        private void UpdatePagerText(List<ContentItem> inventory)
        {
            _totalPages = Mathf.CeilToInt((float)inventory.Count / 9);
            if (_currentPage > _totalPages)
                _currentPage = _totalPages;

            _pagerText.text = $"{_currentPage}/{_totalPages}";
        }

        private void UpdateSockets(List<ContentItem> inventory)
        {
            ClearSockets();

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

                // vobContainer.Go.SetParent(_itemsRootBucket);
                _socketContainer.TryAddGrabbable(vobContainer.Go.GetComponentInChildren<HVRGrabbable>());
            }
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
                this.ExecuteNextUpdate(() => Destroy(heldRoot));
            }
        }
    }
}
#endif
