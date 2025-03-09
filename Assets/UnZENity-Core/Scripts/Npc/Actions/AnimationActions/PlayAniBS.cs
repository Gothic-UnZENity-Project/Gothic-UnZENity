using GUZ.Core._Npc2;
using GUZ.Core.Creator;
using GUZ.Core.Vm;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAniBs : AbstractAnimationAction
    {
        public PlayAniBs(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            Props.BodyState = (VmGothicEnums.BodyState)Action.Int0;
            PrefabProps.AnimationHandler.PlayAnimation(Action.String0);
        }
    }
}
