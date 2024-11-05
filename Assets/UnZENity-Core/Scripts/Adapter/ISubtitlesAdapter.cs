using UnityEngine;

namespace GUZ.Core.Adapter
{
    public interface ISubtitlesAdapter
    {
        public void StartDialogInitially();
        public void EndDialog();
        public void ShowSubtitles(GameObject npcGo);
        public void HideSubtitles();
        public void HideSubtitlesImmediate();
        public void FillSubtitles(string npcName, string text);
    }
}
