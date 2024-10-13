﻿#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Adapter;
using GUZ.Core.Data;
using GUZ.Core.Globals;
using GUZ.VR.Components.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;

namespace GUZ.VR.Adapter
{
    public class VRSubtitlesAdapter : ISubtitlesAdapter
    {
        private VRSubtitles _subtitlesComponent; 

        private VRSubtitles GetDialog()
        {
            // The component is stored in General scene. We therefore load it when accessing for the first time.
            if (_subtitlesComponent == null)
            {
                var scene = SceneManager.GetSceneByName(Constants.ScenePlayer);

                // Try a second time with main scene. Used for Lab.
                if (!scene.IsValid())
                {
                    scene = SceneManager.GetActiveScene();
                }
                
                // We need to look through all RootGOs and fetch the first matching HVRDialog Component.
                _subtitlesComponent = scene.GetRootGameObjects()
                    .Select(i => i.GetComponentInChildren<VRSubtitles>())
                    .First(i => i != null);
            }

            return _subtitlesComponent;
        }
        

        public void ShowDialog(GameObject npcGo)
        {
            var dialog = GetDialog();
            dialog.ShowDialog(npcGo);
        }

        public void HideDialogImmediate(){
            GetDialog().HideDialogImmediate();
        }

        public void HideDialog()
        {
            GetDialog().HideDialog();
        }

        public void FillDialog(string npcName, string subtitles)
        {
            GetDialog().FillDialog(npcName, subtitles);
        }
    }
}
#endif