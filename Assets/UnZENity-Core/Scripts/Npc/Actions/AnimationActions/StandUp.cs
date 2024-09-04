using GUZ.Core.Data.ZkEvents;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StandUp : AbstractAnimationAction
    {
        public StandUp(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            // FIXME - TODO
        }

        public override void AnimationEndEventCallback(SerializableEventEndSignal eventData)
        {
            // FIXME - TODO
        }

        public override bool IsFinished()
        {
            // FIXME - DEBUG
            return true;
        }
    }
}
