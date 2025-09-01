using GUZ.Core;
using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using GUZ.Core.Npc.Actions;

namespace GUZ.Lab.AnimationActionMocks
{
    public class LabCreateInventoryItem : AbstractLabAnimationAction
    {
        public LabCreateInventoryItem(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var itemSymbol = GameData.GothicVm.GetSymbolByName(Action.String0);

            GameGlobals.Npcs.ExtCreateInvItems(NpcInstance, itemSymbol!.Index, 1);

            base.Start();
        }
    }
}
