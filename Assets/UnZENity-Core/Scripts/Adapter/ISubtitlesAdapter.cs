using System.Collections.Generic;
using GUZ.Core.Data;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Adapter
{
    public interface ISubtitlesAdapter
    {
        public void ShowSubtitles(GameObject npcGo);
        public void HideSubtitles();
        public void HideSubtitlesImmediate();
        public void FillSubtitles(string npcName, string text);
    }
}
