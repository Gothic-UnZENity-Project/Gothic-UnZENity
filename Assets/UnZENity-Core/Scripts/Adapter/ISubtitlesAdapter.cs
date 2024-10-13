using System.Collections.Generic;
using GUZ.Core.Data;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Adapter
{
    public interface ISubtitlesAdapter
    {
        public void ShowDialog(GameObject npcGo);
        public void HideDialog();
        public void HideDialogImmediate();
        public void FillDialog(string npcName, string text);
    }
}
