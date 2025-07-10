#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Globals;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Utils;
using UnityEngine;

namespace GUZ.VR.Components
{
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
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            SFXPlayer.Instance?.PlaySFX(grabbable.HandGrabbedClip, grabbable.transform.position);
        }

        public void OnReleased(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            SFXPlayer.Instance?.PlaySFX(grabbable.HandGrabbedClip, grabbable.transform.position);
        }
    }
}
#endif
