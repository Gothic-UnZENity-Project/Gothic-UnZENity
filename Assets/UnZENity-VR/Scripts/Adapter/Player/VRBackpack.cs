#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Globals;
using HurricaneVR.Framework.Core.Sockets;
using UnityEngine;

namespace GUZ.VR.Adapter.Player
{
    [RequireComponent(typeof(HVRSocketable))]
    public class VRBackpack : MonoBehaviour
    {
        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = SoundCreator.ToAudioClip(SfxConst.InvOpen.File);
            socketable.SocketedClip = SoundCreator.ToAudioClip(SfxConst.InvClose.File);
        }
    }
}
#endif
