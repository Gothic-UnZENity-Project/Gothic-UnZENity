using GUZ.Core._Npc2;
using GUZ.Core.Data.ZkEvents;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StandUp : AbstractAnimationAction
    {
        public StandUp(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            PrefabProps.AnimationHandler.PlayIdleAnimation();
        }

        public override bool IsFinished()
        {
            return true;
        }
    }
}
