using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Components;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.VR.Components
{
    public class VRVobDoor : MonoBehaviour
    {
        private static Dictionary<string, AudioClip> _doorOpenedClips = new();
        private static Dictionary<string, AudioClip> _doorClosedClips= new();
        
        private void Start()
        {
            var props = GetComponent<VobDoorProperties>().DoorProperties;
            PrepareSounds(props);
        }
        
        /// <summary>
        /// Add opening and closing sound to the containers HVR settings.
        /// The logic about when to play them is provided by HVR itself.
        /// </summary>
        private void PrepareSounds(Door props)
        {
            if (props == null)
            {
                return;
            }

            var mdsName = props.Visual?.Name;

            if (string.IsNullOrEmpty(mdsName))
            {
                return;
            }

            // Check if the sound is already loaded and cached.
            
            // If the sound isn't already loaded and cached: Do it now.
            if (!_doorClosedClips.ContainsKey(mdsName.ToLower()))
            {
                var mds = ResourceLoader.TryGetModelScript(mdsName);

                if (mds == null)
                {
                    return;
                }

                var openSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S0_2_S1")).SoundEffects.First().Name;
                var closeSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S1_2_S0")).SoundEffects.First().Name;

                _doorOpenedClips.Add(mdsName.ToLower(), VobHelper.GetSoundClip(openSoundEffect));
                _doorClosedClips.Add(mdsName.ToLower(), VobHelper.GetSoundClip(closeSoundEffect));
            }

            var hvrDoor = GetComponentInChildren<HVRPhysicsDoor>();
            hvrDoor.SFXOpened = _doorOpenedClips[mdsName.ToLower()];
            hvrDoor.SFXClosed = _doorClosedClips[mdsName.ToLower()];
        }
    }
}
