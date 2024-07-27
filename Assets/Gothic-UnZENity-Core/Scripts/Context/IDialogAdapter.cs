using System.Collections.Generic;
using GUZ.Core.Data;
using ZenKit.Daedalus;

namespace GUZ.Core.Context
{
    public interface IDialogAdapter
    {
        public void ShowDialog();
        public void HideDialog();
        public void FillDialog(int npcInstanceIndex, List<DialogOption> dialogOptions);
        public void FillDialog(int npcInstanceIndex, List<InfoInstance> dialogOptions);
    }
}
