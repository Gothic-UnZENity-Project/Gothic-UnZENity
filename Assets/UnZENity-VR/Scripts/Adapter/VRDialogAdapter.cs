#if GUZ_HVR_INSTALLED
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Context;
using GUZ.Core.Data;
using GUZ.Core.Globals;
using GUZ.VR.Components.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZenKit.Daedalus;

namespace GUZ.VR.Adapter
{
    public class VRDialogAdapter : IDialogAdapter
    {
        private VRDialog _dialogComponent;

        private VRDialog GetDialog()
        {
            // The component is stored in General scene. We therefore load it when accessing for the first time.
            if (_dialogComponent == null)
            {
                var scene = SceneManager.GetSceneByName(Constants.SceneGeneral);

                // Try a second time with main scene. Used for Lab.
                if (!scene.IsValid())
                {
                    scene = SceneManager.GetActiveScene();
                }
                
                // We need to look through all RootGOs and fetch the first matching HVRDialog Component.
                _dialogComponent = scene.GetRootGameObjects()
                    .Select(i => i.GetComponentInChildren<VRDialog>())
                    .First(i => i != null);
            }

            return _dialogComponent;
        }
        

        public void ShowDialog(GameObject npcGo)
        {
            var dialog = GetDialog();
            dialog.ShowDialog(npcGo);
        }

        public void HideDialog()
        {
            GetDialog().HideDialog();
        }

        public void FillDialog(int npcInstanceIndex, List<DialogOption> dialogOptions)
        {
            GetDialog().FillDialog(npcInstanceIndex, dialogOptions);
        }

        public void FillDialog(int npcInstanceIndex, List<InfoInstance> dialogOptions)
        {
            GetDialog().FillDialog(npcInstanceIndex, dialogOptions);
        }
    }
}
#endif
