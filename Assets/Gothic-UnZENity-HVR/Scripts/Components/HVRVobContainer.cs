#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Vobs;

namespace GUZ.HVR.Components
{
    [RequireComponent(typeof(VobContainerProperties))]
    public class HVRVobContainer : MonoBehaviour
    {
        private static Dictionary<string, AudioClip> _containerOpenedClips = new();
        private static Dictionary<string, AudioClip> _containerClosedClips= new();
        
        
        private void Start()
        {
            var props = GetComponent<VobContainerProperties>().ContainerProperties;

            if (props == null)
            {
                if (SceneManager.GetActiveScene().name != Constants.SceneLab)
                {
                    Debug.LogError("oCMobContainer properties not set for GameObject.");
                    return;
                }
            }

            PrepareSounds(props);
        }

        /// <summary>
        /// Add opening and closing sound to the containers HVR settings.
        /// The logic about when to play them is provided by HVR itself.
        /// </summary>
        private void PrepareSounds(Container props)
        {
            if (props == null)
            {
                return;
            }

            var mdsName = props.Visual!.Name;

            // If the sound isn't already loaded and cached: Do it now.
            if (!_containerClosedClips.ContainsKey(mdsName.ToLower()))
            {
                var mds = ResourceLoader.TryGetModelScript(mdsName);

                if (mds == null)
                {
                    Debug.LogError($"ModelScript >{mdsName}< for oCMobContainer not found.");
                    return;
                }

                var openSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S0_2_S1")).SoundEffects.First().Name;
                var closeSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S1_2_S0")).SoundEffects.First().Name;

                _containerOpenedClips.Add(mdsName.ToLower(), VobHelper.GetSoundClip(openSoundEffect));
                _containerClosedClips.Add(mdsName.ToLower(), VobHelper.GetSoundClip(closeSoundEffect));
            }

            // We leverage the same "door" script for containers (As it's also just an object to be rotated.
            var hvrDoor = GetComponentInChildren<HVRPhysicsDoor>();
            hvrDoor.SFXOpened = _containerOpenedClips[mdsName.ToLower()];
            hvrDoor.SFXClosed = _containerClosedClips[mdsName.ToLower()];
        }
    }
}
#endif
