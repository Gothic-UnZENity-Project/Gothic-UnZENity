using GUZ.Core._Npc2;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class AlignToFp : AbstractRotateAnimationAction
    {
        public AlignToFp(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        protected override Quaternion GetRotationDirection()
        {
            var euler = Props.CurrentFreePoint.Direction;

            return Quaternion.Euler(euler);
        }
    }
}
