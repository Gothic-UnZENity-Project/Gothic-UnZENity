#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Manager;
using GUZ.Core.Services.Player;
using HurricaneVR.Framework.Core.Sockets;
using Reflex.Attributes;
using TMPro;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        [SerializeField] private TMP_Text _pagerText;
        private int _currentPage = 1;
        private int _totalPages = 5;

        
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly PlayerService _playerService;

        
        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = _audioService.CreateAudioClip(_audioService.InvOpen.File);
            socketable.SocketedClip = _audioService.CreateAudioClip(_audioService.InvClose.File);

            UpdatePagerText();
        }

        public void OnPrevPageClick()
        {
            _currentPage--;
            if (_currentPage < 1)
                _currentPage = 1;

            UpdatePagerText();
        }

        public void OnNextPageClick()
        {
            _currentPage++;

            UpdatePagerText();
        }

        private void UpdatePagerText()
        {
            var inventory = _playerService.GetInventory();
            _totalPages = Mathf.CeilToInt((float)inventory.Count / 9);
            if (_currentPage > _totalPages)
                _currentPage = _totalPages;

            _pagerText.text = $"{_currentPage}/{_totalPages}";
        }

        
    }
}
#endif
