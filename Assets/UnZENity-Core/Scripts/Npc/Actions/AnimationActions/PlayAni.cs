using GUZ.Core._Npc2;
using GUZ.Core.Creator;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAni : AbstractAnimationAction
    {
        private string _animName => Action.String0;

        public PlayAni(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            PrefabProps.AnimationHandler.PlayAnimation(Action.String0);
            AnimationEndEventTime = PrefabProps.AnimationHandler.GetAnimationLength(_animName);
        }
    }
}
