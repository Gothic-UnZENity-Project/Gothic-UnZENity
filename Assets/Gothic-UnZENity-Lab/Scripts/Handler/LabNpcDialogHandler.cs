using System.Linq;
using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabNpcDialogHandler : MonoBehaviour, ILabHandler
    {
        [FormerlySerializedAs("animationsDropdown")]
        public TMP_Dropdown AnimationsDropdown;

        [FormerlySerializedAs("bloodwynSlotGo")]
        public GameObject BloodwynSlotGo;

        private string _bloodwynInstanceId = "GRD_233_Bloodwyn";


        private NpcInstance _bloodwynInstance;

        private string[] _animations =
        {
            "T_LGUARD_2_STAND", "T_STAND_2_LGUARD", "T_LGUARD_SCRATCH", "T_LGUARD_STRETCH", "T_LGUARD_CHANGELEG",
            "T_HGUARD_2_STAND", "T_STAND_2_HGUARD", "T_HGUARD_LOOKAROUND"
        };

        public void Bootstrap()
        {
            AnimationsDropdown.options = _animations.Select(item => new TMP_Dropdown.OptionData(item)).ToList();

            BootstrapBloodwyn();
        }


        private void BootstrapBloodwyn()
        {
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);
            newNpc.SetParent(BloodwynSlotGo);

            var npcSymbol = GameData.GothicVm.GetSymbolByName(_bloodwynInstanceId);
            _bloodwynInstance = GameData.GothicVm.AllocInstance<NpcInstance>(npcSymbol!);
            var properties = newNpc.GetComponent<NpcProperties>();
            LookupCache.NpcCache[_bloodwynInstance.Index] = (instance: _bloodwynInstance, properties: properties);
            properties.NpcInstance = _bloodwynInstance;

            GameData.GothicVm.InitInstance(_bloodwynInstance);

            properties.Dialogs = GameData.Dialogs.Instances
                .Where(dialog => dialog.Npc == _bloodwynInstance.Index)
                .OrderByDescending(dialog => dialog.Important)
                .ToList();
            newNpc.name = _bloodwynInstance.GetName(NpcNameSlot.Slot0);
            GameData.GothicVm.GlobalSelf = _bloodwynInstance;

            // Hero
            {
                // Need to be set for later usage (e.g. Bloodwyn checks your inventory if enough nuggets are carried)
                var heroInstance = GameData.GothicVm.InitInstance<NpcInstance>("hero");
                GameData.GothicVm.GlobalHero = heroInstance;
            }

            var mdmName = "Hum_GRDM_ARMOR.asc";
            var mdhName = "Humans_Militia.mds";
            var body = new VmGothicExternals.ExtSetVisualBodyData
            {
                Armor = -1,
                Body = "hum_body_Naked0",
                BodyTexColor = 1,
                BodyTexNr = 0,
                Head = "Hum_Head_Bald",
                HeadTexNr = 18,
                TeethTexNr = 1
            };

            MeshFactory.CreateNpc(newNpc.name, mdmName, mdhName, body, newNpc);
        }

        public void AnimationStartClick()
        {
            VmGothicExternals.AI_PlayAni(_bloodwynInstance, AnimationsDropdown.options[AnimationsDropdown.value].text);
        }
    }
}
