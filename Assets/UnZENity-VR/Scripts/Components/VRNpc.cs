#if GUZ_HVR_INSTALLED
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.VR.Components
{
    public class VRNpc : MonoBehaviour
    {
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            var properties = GetComponent<NpcProperties>();

            if (GameData.Dialogs.IsInDialog)
            {
                DialogManager.SkipCurrentDialogLine(properties);
            }
            else
            {
                NpcHelper.ExecutePerception(VmGothicEnums.PerceptionType.AssessTalk, properties, properties.NpcInstance, (NpcInstance)GameData.GothicVm.GlobalHero);
            }
        }
    }
}
#endif
