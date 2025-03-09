using GUZ.Core._Npc2;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class StartState : AbstractAnimationAction
    {
        public StartState(AnimationAction action, NpcContainer2 npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var ai = PrefabProps.AiHandler;

            ai.ClearState(Action.Bool0);

            Props.IsStateTimeActive = true;
            Props.StateTime = 0;

            ai.StartRoutine(Action.Int0, Action.String0);
        }

        /// <summary>
        /// This one is actually no animation, but we need to call Start() only.
        /// FIXME - We need to create an additional inheritance below AbstractAnimationAction if we have more like this class.
        /// </summary>
        /// <returns></returns>
        public override bool IsFinished()
        {
            return true;
        }
    }
}
