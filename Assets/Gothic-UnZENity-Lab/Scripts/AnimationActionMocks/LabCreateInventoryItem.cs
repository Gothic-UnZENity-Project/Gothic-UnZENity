using GUZ.Core.Globals;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Lab.AnimationActionMocks
{
    public class LabCreateInventoryItem : AbstractLabAnimationAction
    {
        public LabCreateInventoryItem(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            var itemSymbol = GameData.GothicVm.GetSymbolByName(Action.String0);

            VmGothicExternals.CreateInvItem(Props.NpcInstance, itemSymbol!.Index);

            base.Start();
        }
    }
}
