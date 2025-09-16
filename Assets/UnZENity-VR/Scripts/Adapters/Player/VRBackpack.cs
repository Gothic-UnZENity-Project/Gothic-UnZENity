#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Models.Vob;
using GUZ.Core.Services.Player;
using GUZ.Core.Services.Vobs;
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
        [SerializeField] private TMP_Text _pagerText;
        [SerializeField] private HVRSocketContainer _socketContainer;

        private int _currentPage = 1;
        private int _totalPages;

        
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly PlayerService _playerService;
        [Inject] private readonly VobService _vobService;

        
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

            var inventory = _playerService.GetInventory();
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        public void OnPrevPageClick()
        {
            _currentPage--;
            if (_currentPage < 1)
                _currentPage = 1;

            var inventory = _playerService.GetInventory();
            UpdatePagerText(inventory);
            UpdateSockets(inventory);
        }

        public void OnNextPageClick()
        {
            _currentPage++;

            var inventory = _playerService.GetInventory();
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

                Destroy(socket.HeldObject.gameObject);
            }
        }
    }
}
#endif
