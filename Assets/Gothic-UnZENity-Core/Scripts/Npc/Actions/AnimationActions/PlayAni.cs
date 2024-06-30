using GUZ.Core.Creator;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class PlayAni : AbstractAnimationAction
    {
        public PlayAni(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            AnimationCreator.PlayAnimation(Props.MdsNames, Action.String0, NpcGo);
        }
    }
}
