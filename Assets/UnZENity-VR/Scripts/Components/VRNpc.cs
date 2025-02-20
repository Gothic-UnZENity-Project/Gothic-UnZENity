#if GUZ_HVR_INSTALLED
using System;
using GUZ.Core;
using GUZ.Core._Npc2;
using GUZ.Core.Extensions;
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
        private NpcContainer2 _npcData;

        private void Awake()
        {
            _npcData = GetComponentInParent<NpcLoader2>().Npc.GetUserData2();
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameData.Dialogs.IsInDialog)
            {
                DialogManager.SkipCurrentDialogLine(_npcData.Properties);
            }
            else
            {
                GameGlobals.NpcAi.ExecutePerception(VmGothicEnums.PerceptionType.AssessTalk, _npcData.Properties, _npcData.Instance, (NpcInstance)GameData.GothicVm.GlobalHero);
            }
        }
    }
}
#endif
