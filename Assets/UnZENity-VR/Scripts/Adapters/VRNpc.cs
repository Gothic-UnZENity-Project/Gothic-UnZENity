#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Adapters.Npc;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GUZ.Core.Models.Vm;
using GUZ.Core.Npc;
using GUZ.Core.Services.Npc;
using GUZ.Core.Vm;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.VR.Adapters
{
    public class VRNpc : MonoBehaviour
    {
        [Inject] private readonly DialogService _dialogService;
        [Inject] private readonly NpcAiService _npcAiService;

        private NpcContainer _npcData;

        private void Awake()
        {
            _npcData = GetComponentInParent<NpcLoader>().Npc.GetUserData();
        }

        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            if (GameData.Dialogs.IsInDialog)
            {
                _dialogService.SkipCurrentDialogLine(_npcData.Props);
            }
            else
            {
                _npcAiService.ExecutePerception(VmGothicEnums.PerceptionType.AssessTalk, _npcData.Props, _npcData.Instance, null, (NpcInstance)GameData.GothicVm.GlobalHero);
            }
        }
    }
}
#endif
