using GUZ.Core.Npc.Routines;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    public class ContinueRoutine : AbstractAnimationAction
    {
        public ContinueRoutine(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            var ai = Props.GetComponent<AiHandler>();

            ai.ClearState(false);

            var routine = Props.GetComponent<Routine>().CurrentRoutine;

            ai.StartRoutine(routine.Action, routine.Waypoint);

            IsFinishedFlag = true;
        }
    }
}
