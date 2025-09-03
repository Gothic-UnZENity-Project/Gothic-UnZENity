using System.Linq;
using GUZ.Core.Models.Container;
using GUZ.Core.Const;
using GUZ.Core.Manager;
using Reflex.Attributes;

namespace GUZ.Core.Domain.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Hint: This is no Daedalus external. We execute this element whenever the Dialog UI needs to be opened.
    ///       It's simply an AnimationAction for convenience reasons and to align with AnimationQueue.
    /// </summary>
    public class StartProcessInfos : AbstractAnimationAction
    {
        [Inject] private readonly DialogService _dialogService;

        private bool _isDialogStarting => Action.Bool0;
        private int _dialogId => Action.Int0;


        public StartProcessInfos(AnimationAction action, NpcContainer npcData) : base(action, npcData)
        {
        }

        public override void Start()
        {
            // Whatever comes next, this is no animation Action and we go on. ;-)
            IsFinishedFlag = true;


            if (_isDialogStarting)
            {
                _dialogService.StartDialog(NpcContainer, true);

                return;
            }


            var isInSubDialog = GameData.Dialogs.CurrentDialog.Options.Any();

            if (isInSubDialog)
            {
                var foundItem = GameData.Dialogs.CurrentDialog.Options
                        .FirstOrDefault(option => option.Function == _dialogId);

                // If a dialog calls Info_ClearChoices(), then the current sub dialog is already gone.
                if (foundItem != null)
                {
                    GameData.Dialogs.CurrentDialog.Options.Remove(foundItem);
                }
            }

            _dialogService.StartDialog(NpcContainer, false);
        }
    }
}
