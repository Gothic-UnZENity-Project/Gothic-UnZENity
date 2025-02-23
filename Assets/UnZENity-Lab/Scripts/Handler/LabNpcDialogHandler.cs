using System.Linq;
using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Data;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabNpcDialogHandler : AbstractLabHandler
    {
        public GameObject NpcSlotGo;

        private string _bloodwynInstanceId = "GRD_233_Bloodwyn";
        private NpcInstance _bloodwynInstance;


        public override void Bootstrap()
        {
            BootstrapBloodwyn();
        }

        private void BootstrapBloodwyn()
        {
            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);
            newNpc.SetParent(NpcSlotGo);

            var npcSymbol = GameData.GothicVm.GetSymbolByName(_bloodwynInstanceId);
            _bloodwynInstance = GameData.GothicVm.InitInstance<NpcInstance>(npcSymbol!);
            var properties = newNpc.GetComponent<NpcProperties>();
            properties.NpcData.Instance = _bloodwynInstance;

            var npcData = new NpcContainer
            {
                Instance = _bloodwynInstance,
                Properties = properties
            };
            MultiTypeCache.NpcCache.Add(npcData);

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
    }
}
