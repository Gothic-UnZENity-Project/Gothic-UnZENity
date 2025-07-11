#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Globals;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core.Utils;
using UnityEngine;

namespace GUZ.VR.Components
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
