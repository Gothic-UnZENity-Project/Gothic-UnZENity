using GUZ.Core._Npc2;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class None : AbstractAnimationAction
    {
        public None(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            // NOP
        }

        public override bool IsFinished()
        {
            return true;
        }
    }
}
