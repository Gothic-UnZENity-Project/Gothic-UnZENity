using GUZ.Core;
using GUZ.Core._Npc2;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
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
            var newNpc = new GameObject("NPC");
            var loaderComp = newNpc.AddComponent<NpcLoader2>();
            newNpc.SetParent(NpcSlotGo);

            var npcSymbol = GameData.GothicVm.GetSymbolByName(_bloodwynInstanceId)!;
            _bloodwynInstance = GameData.GothicVm.AllocInstance<NpcInstance>(npcSymbol);

            var npcData = new NpcContainer2
            {
                Instance = _bloodwynInstance,
                Props = new(),
                Vob = new()
            };
            _bloodwynInstance.UserData = npcData;
            loaderComp.Npc = _bloodwynInstance;
            MultiTypeCache.NpcCache2.Add(npcData);

            newNpc.name = _bloodwynInstance.GetName(NpcNameSlot.Slot0);
            GameData.GothicVm.GlobalSelf = _bloodwynInstance;

            GameData.GothicVm.InitInstance(_bloodwynInstance);

            // Hero
            {
                // Need to be set for later usage (e.g. Bloodwyn checks your inventory if enough nuggets are carried)
                var heroInstance = GameData.GothicVm.InitInstance<NpcInstance>("hero");
                GameData.GothicVm.GlobalHero = heroInstance;
            }

            // We need to initialize the NPC at this frame to set the positions of child GOs now.
            GameGlobals.Npcs.InitNpc(newNpc, true);
            newNpc.transform.SetLocalPositionAndRotation(default, default);
            newNpc.transform.GetChild(0).SetLocalPositionAndRotation(default, default);
        }
    }
}
