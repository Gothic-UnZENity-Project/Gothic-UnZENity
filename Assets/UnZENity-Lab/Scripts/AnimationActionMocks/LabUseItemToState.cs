using GUZ.Core.Models.Container;
using GUZ.Core.Const;
using GUZ.Core.Domain.Npc.Actions;
using GUZ.Core.Domain.Npc.Actions.AnimationActions;

namespace GUZ.Lab.AnimationActionMocks
{
    public class LabUseItemToState : UseItemToState
    {
        public LabUseItemToState(AnimationAction action, NpcContainer npcContainer) : base(CalculateItemIndex(action), npcContainer)
        {
        }

        private static AnimationAction CalculateItemIndex(AnimationAction action)
        {
            var item = GameData.GothicVm.GetSymbolByName(action.String0);

            return new AnimationAction(
                int0: item!.Index,
                int1: action.Int1
            );
        }
    }
}
