using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class None : AbstractAnimationAction
    {
        public None(AnimationAction action, GameObject npcGo) : base(action, npcGo)
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
