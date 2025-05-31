using GUZ.Core.Data.Container;
using GUZ.Core.Globals;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;

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
