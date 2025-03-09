using GUZ.Core._Npc2;
using GUZ.Core.Globals;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Vm;

namespace GUZ.Lab.AnimationActionMocks
{
    public class LabCreateInventoryItem : AbstractLabAnimationAction
    {
        public LabCreateInventoryItem(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var itemSymbol = GameData.GothicVm.GetSymbolByName(Action.String0);

            VmGothicExternals.CreateInvItem(NpcInstance, itemSymbol!.Index);

            base.Start();
        }
    }
}
