using System.Collections.Generic;
using GUZ.Core.Data;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Context
{
    public interface IDialogAdapter
    {
        public void ShowDialog(GameObject npcGo);
        public void HideDialog();
        public void FillDialog(int npcInstanceIndex, List<DialogOption> dialogOptions);
        public void FillDialog(int npcInstanceIndex, List<InfoInstance> dialogOptions);
    }
}
