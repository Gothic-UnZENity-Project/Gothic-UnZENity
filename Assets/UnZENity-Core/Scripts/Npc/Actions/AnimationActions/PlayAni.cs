using GUZ.Core._Npc2;
using GUZ.Core.Creator;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAni : AbstractAnimationAction
    {
        public PlayAni(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            AnimationCreator.PlayAnimation(Props.MdsNames, Action.String0, NpcGo);
        }
    }
}
