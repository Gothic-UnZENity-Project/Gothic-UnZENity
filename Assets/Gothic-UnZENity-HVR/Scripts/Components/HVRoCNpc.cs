#if GUZ_HVR_INSTALLED
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace GUZ.HVR.Components
{
    public class HVRoCNpc : MonoBehaviour
    {
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            DialogManager.StartDialog(gameObject, GetComponent<NpcProperties>());
        }
    }
}
#endif
