using GUZ.Core.Creator;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAniBS : AbstractAnimationAction
    {
        public PlayAniBS(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            Props.bodyState = (VmGothicEnums.BodyState)Action.Int0;
            AnimationCreator.PlayAnimation(Props.mdsNames, Action.String0, NpcGo);
        }
    }
}
