﻿#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Util;
using GUZ.Core.Vob;
using HurricaneVR.Framework.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Vobs;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components
{
    public class VRVobContainer : MonoBehaviour
    {
        private static Dictionary<string, AudioClip> _containerOpenedClips = new();
        private static Dictionary<string, AudioClip> _containerClosedClips= new();
        
        
        private void Start()
        {
            var container = GetComponentInParent<VobLoader>().Container.VobAs<IContainer>();

            if (container == null)
            {
                if (SceneManager.GetActiveScene().name != Constants.SceneLab)
                {
                    Logger.LogError("oCMobContainer properties not set for GameObject.", LogCat.VR);
                    return;
                }
            }

            PrepareSounds(container);
        }

        /// <summary>
        /// Add opening and closing sound to the containers HVR settings.
        /// The logic about when to play them is provided by HVR itself.
        /// </summary>
        private void PrepareSounds(IContainer vobContainer)
        {
            if (vobContainer == null)
            {
                return;
            }

            var mdsName = vobContainer.Visual!.Name;

            // If the sound isn't already loaded and cached: Do it now.
            if (!_containerClosedClips.ContainsKey(mdsName.ToLower()))
            {
                var mds = ResourceLoader.TryGetModelScript(mdsName);

                if (mds == null)
                {
                    Logger.LogError($"ModelScript >{mdsName}< for oCMobContainer not found.", LogCat.VR);
                    return;
                }

                var openSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S0_2_S1")).SoundEffects.First().Name;
                var closeSoundEffect = mds.Animations.First(i => i.Name.EqualsIgnoreCase("t_S1_2_S0")).SoundEffects.First().Name;

                _containerOpenedClips.Add(mdsName.ToLower(), GameGlobals.Vobs.GetRandomSoundClip(openSoundEffect));
                _containerClosedClips.Add(mdsName.ToLower(), GameGlobals.Vobs.GetRandomSoundClip(closeSoundEffect));
            }

            // We leverage the same "door" script for containers (As it's also just an object to be rotated.
            var hvrDoor = GetComponentInChildren<HVRPhysicsDoor>();
            hvrDoor.SFXOpened = _containerOpenedClips[mdsName.ToLower()];
            hvrDoor.SFXClosed = _containerClosedClips[mdsName.ToLower()];
        }
    }
}
#endif
