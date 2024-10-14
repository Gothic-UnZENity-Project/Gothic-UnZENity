#if GUZ_HVR_INSTALLED
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

        private VRSubtitles GetSubtitles()
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
                
                // We need to look through all RootGOs and fetch the first matching VRSubtitles Component.
                _subtitlesComponent = scene.GetRootGameObjects()
                    .Select(i => i.GetComponentInChildren<VRSubtitles>())
                    .First(i => i != null);
            }

            return _subtitlesComponent;
        }
        

        public void ShowSubtitles(GameObject npcGo)
        {
            var dialog = GetSubtitles();
            dialog.ShowSubtitles(npcGo);
        }

        public void HideSubtitlesImmediate(){
            GetSubtitles().HideSubtitlesImmediate();
        }

        public void HideSubtitles()
        {
            GetSubtitles().HideSubtitles();
        }

        public void FillSubtitles(string npcName, string subtitles)
        {
            GetSubtitles().FillSubtitles(npcName, subtitles);
        }
    }
}
#endif
