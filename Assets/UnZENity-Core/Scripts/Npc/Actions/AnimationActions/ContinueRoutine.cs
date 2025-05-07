using GUZ.Core.Data.Container;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class ContinueRoutine : AbstractAnimationAction
    {
        public ContinueRoutine(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var ai = PrefabProps.AiHandler;

            ai.ClearState(false);

            var routine = Props.RoutineCurrent;

            ai.StartRoutine(routine.Action, routine.Waypoint);

            IsFinishedFlag = true;
        }
    }
}
