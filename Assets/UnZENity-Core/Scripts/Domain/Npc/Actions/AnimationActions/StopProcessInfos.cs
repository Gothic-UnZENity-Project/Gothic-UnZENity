using GUZ.Core.Models.Container;
using GUZ.Core.Manager;
using Reflex.Attributes;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Dialog is done. (e.g. END clicked)
    /// Therefore close dialog menu immediately.
    /// </summary>
    public class StopProcessInfos : AbstractAnimationAction
    {
        [Inject] private readonly DialogService _dialogService;

        public StopProcessInfos(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            _dialogService.StopDialog(NpcContainer);
            GameGlobals.Story.SwitchChapterIfPending();
            IsFinishedFlag = true;
        }
    }
}
