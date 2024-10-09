#if GUZ_HVR_INSTALLED
using GUZ.Core.Caches;
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
            var foo = MultiTypeCache.NpcCache[GameData.GothicVm.GlobalOther.Index];
            var isPlayerInvincible = foo.instance.GetAiVar(Constants.DaedalusConst.AIVInvincibleKey);
            Debug.Log($"NPC Grabbes: IsInDialog = {GameData.Dialogs.IsInDialog}, AIVInvincible = {isPlayerInvincible}");
            if (GameData.Dialogs.IsInDialog && isPlayerInvincible == 1)
            {
                DialogManager.SkipCurrentDialogLine(GetComponent<NpcProperties>());
                Debug.Log("Skipping current dialog line");
            }
            else
            {
                // FIXME - We need to call passive Perception Perc_ASSESSTALK rather than starting the dialog this way.
                DialogManager.StartDialog(gameObject, GetComponent<NpcProperties>(), true);
                Debug.Log("Starting dialog");
            }
        }
    }
}
#endif
