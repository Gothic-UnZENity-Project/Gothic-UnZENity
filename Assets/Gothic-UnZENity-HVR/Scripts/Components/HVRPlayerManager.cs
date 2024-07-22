#if GUZ_HVR_INSTALLED
using System.Collections;
using System.Linq;
using GUZ.Core.Manager;
using GUZ.Core.Util;
using HurricaneVR.Framework.Core.Player;
using HurricaneVR.Framework.Core.UI;
using UnityEngine;

namespace GVR
{
    public class HVRPlayerManager : SingletonBehaviour<HVRPlayerManager>
    {
        private void Start()
        {
            // Find all ui canvases and add to HVR Input module.
            Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
            HVRInputModule.Instance.UICanvases = allCanvases.ToList();

            // Make sure UI pointers are added to input module.
            HVRUIPointer[] pointers = GetComponentsInChildren<HVRUIPointer>();
            for (int i = 0; i < pointers.Length; i++)
            {
                HVRInputModule.Instance.AddPointer(pointers[i]);
            }
        }
    }
}
#endif
