#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using HurricaneVR.Framework.Core.Sockets;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.VR.Adapters.Player
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        [Inject] private readonly AudioService _audioService;

        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = _audioService.CreateAudioClip(SfxConst.InvOpen.File);
            socketable.SocketedClip = _audioService.CreateAudioClip(SfxConst.InvClose.File);
        }
    }
}
#endif
