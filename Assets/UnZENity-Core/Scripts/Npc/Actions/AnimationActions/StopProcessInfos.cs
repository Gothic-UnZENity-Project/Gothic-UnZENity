using GUZ.Core.Manager;
using GUZ.Core.Properties;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Dialog is done. (e.g. END clicked)
    /// Therefore close dialog menu immediately.
    /// </summary>
    public class StopProcessInfos : AbstractAnimationAction
    {
        public StopProcessInfos(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            IsFinishedFlag = true;
            
            DialogManager.StopDialog();
            GameGlobals.Story.SwitchChapterIfPending();

            // Restart NPC routine
            Props.CurrentLoopState = NpcProperties.LoopState.Start;
        }
    }
}
