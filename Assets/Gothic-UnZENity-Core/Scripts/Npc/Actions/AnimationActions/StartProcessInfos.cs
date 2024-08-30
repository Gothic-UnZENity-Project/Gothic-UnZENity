using System.Linq;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Npc.Actions.AnimationActions
{
    /// <summary>
    /// Hint: This is no Daedalus external. We execute this element whenever the Dialog UI needs to be opened.
    ///       It's simply an AnimationAction for convenience reasons and to align with AnimationQueue.
    /// </summary>
    public class StartProcessInfos : AbstractAnimationAction
    {
        private int _dialogId => Action.Int0;

        public StartProcessInfos(AnimationAction action, GameObject npcGo) : base(action, npcGo)
        {
        }

        public override void Start()
        {
            IsFinishedFlag = true;

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

            DialogManager.StartDialog(NpcGo, Props, false);
        }
    }
}
