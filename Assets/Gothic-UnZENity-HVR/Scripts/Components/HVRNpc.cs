#if GUZ_HVR_INSTALLED
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class HVRNpc : MonoBehaviour
    {
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameData.Dialogs.IsInDialog)
            {
                DialogManager.SkipCurrentDialogLine(GetComponent<NpcProperties>());
            }
            else
            {
                DialogManager.StartDialog(gameObject, GetComponent<NpcProperties>(), true);
            }
        }
    }
}
#endif
