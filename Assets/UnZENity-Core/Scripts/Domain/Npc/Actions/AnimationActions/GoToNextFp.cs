using GUZ.Core.Data.Container;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    public class GoToNextFp : GoToFp
    {
        public GoToNextFp(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }
    }
}
