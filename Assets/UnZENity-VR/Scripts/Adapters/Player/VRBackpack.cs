#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Manager;
using GUZ.Core.Services.Audio;
using HurricaneVR.Framework.Core.Sockets;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        [Inject] private readonly AudioService _audioService;
        [Inject] private readonly SfxService _sfxService;

        
        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = _audioService.CreateAudioClip(_sfxService.InvOpen.File);
            socketable.SocketedClip = _audioService.CreateAudioClip(_sfxService.InvClose.File);
        }
    }
}
#endif
