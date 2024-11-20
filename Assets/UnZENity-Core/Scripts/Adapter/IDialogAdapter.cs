using System.Collections.Generic;
using GUZ.Core.Data;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Adapter
{
    public interface IDialogAdapter
    {
        public void StartDialogInitially();
        public void EndDialog();
        public void ShowDialog(GameObject npcGo);
        public void HideDialog();
        public void FillDialog(NpcInstance instance, List<DialogOption> dialogOptions);
        public void FillDialog(NpcInstance instance, List<InfoInstance> dialogOptions);
    }
}
