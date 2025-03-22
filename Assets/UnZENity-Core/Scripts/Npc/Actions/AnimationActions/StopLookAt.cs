using GUZ.Core._Npc2;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StopLookAtNpc : AbstractAnimationAction
    {
        public StopLookAtNpc(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        { }

        public override void Start()
        {
            PrefabProps.AnimationHeadHandler.StopLookAt();

            IsFinishedFlag = true;
        }
    }
}
