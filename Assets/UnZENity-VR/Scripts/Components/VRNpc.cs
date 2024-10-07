#if GUZ_HVR_INSTALLED
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.VR.Components
{
    public class VRNpc : MonoBehaviour
    {
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameData.Dialogs.IsInDialog)
            {
                DialogManager.SkipCurrentDialogLine(GetComponent<NpcProperties>());
            }
            else
            {
                // FIXME - We need to call passive Perception Perc_ASSESSTALK rather than starting the dialog this way.
                DialogManager.StartDialog(gameObject, GetComponent<NpcProperties>(), true);
            }
        }
    }
}
#endif
