using GUZ.Core.Creator;
using GUZ.Core.Vm;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAniBs : AbstractAnimationAction
    {
        public PlayAniBs(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            Props.BodyState = (VmGothicEnums.BodyState)Action.Int0;
            AnimationCreator.PlayAnimation(Props.MdsNames, Action.String0, NpcGo);
        }
    }
}
