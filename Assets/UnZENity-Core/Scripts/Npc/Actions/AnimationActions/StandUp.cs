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
            // FIXME - TODO
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            // FIXME - Use via Timer of the animation instead. Timer value is set within Abstract parent class.
        }

        public override bool IsFinished()
        {
            // FIXME - DEBUG
            return true;
        }
    }
}
