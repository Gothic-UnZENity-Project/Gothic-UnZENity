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
        private AudioClip _openClip;
        private AudioClip _closeClip;

        private void Start()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Init);
        }

        private void Init()
        {
            _openClip = SoundCreator.ToAudioClip(SfxConst.InvOpen.File);
            _closeClip = SoundCreator.ToAudioClip(SfxConst.InvClose.File);
            
            var socketable = GetComponent<HVRSocketable>();

            socketable.UnsocketedClip = _openClip;
            socketable.SocketedClip = _closeClip;
        }
    }
}
#endif
