using GUZ.Core.Data.Container;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StopLookAtNpc : AbstractAnimationAction
    {
        public StopLookAtNpc(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        { }

        public override void Start()
        {
            PrefabProps.AnimationHeadHandler.StopLookAt();

            IsFinishedFlag = true;
        }
    }
}
