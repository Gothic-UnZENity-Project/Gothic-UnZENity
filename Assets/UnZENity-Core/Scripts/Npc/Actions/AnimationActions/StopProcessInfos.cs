using GUZ.Core.Data.Container;
using GUZ.Core.Manager;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Dialog is done. (e.g. END clicked)
    /// Therefore close dialog menu immediately.
    /// </summary>
    public class StopProcessInfos : AbstractAnimationAction
    {
        public StopProcessInfos(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            DialogManager.StopDialog(NpcContainer);
            GameGlobals.Story.SwitchChapterIfPending();
            IsFinishedFlag = true;
        }
    }
}
