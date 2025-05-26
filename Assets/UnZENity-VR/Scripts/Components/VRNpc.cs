#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Npc;
using GUZ.Core.Vm;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.VR.Components
{
    public class VRNpc : MonoBehaviour
    {
        private NpcContainer _npcData;

        private void Awake()
        {
            _npcData = GetComponentInParent<NpcLoader>().Npc.GetUserData();
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameData.Dialogs.IsInDialog)
            {
                DialogManager.SkipCurrentDialogLine(_npcData.Props);
            }
            else
            {
                GameGlobals.NpcAi.ExecutePerception(VmGothicEnums.PerceptionType.AssessTalk, _npcData.Props, _npcData.Instance, null, (NpcInstance)GameData.GothicVm.GlobalHero);
            }
        }
    }
}
#endif
