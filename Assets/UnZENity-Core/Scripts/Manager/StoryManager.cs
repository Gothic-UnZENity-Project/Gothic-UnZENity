using GUZ.Core.Context;

namespace GUZ.Core.Manager
{
    public class StoryManager
    {
        public bool _isChapterSwitchPending;
        private (string chapter, string text, string texture, string wav, int time) _chapterSwitchData;

        public StoryManager(GameConfiguration config)
        {
            // For later. ;-)
        }

        public void ExtIntroduceChapter(string chapter, string text, string texture, string wav, int time)
        {
            _isChapterSwitchPending = true;
            _chapterSwitchData = (chapter, text, texture, wav, time);
        }

        public void SwitchChapterIfPending()
        {
            if (!_isChapterSwitchPending)
            {
                return;
            }

            GuzContext.InteractionAdapter.IntroduceChapter(
                _chapterSwitchData.chapter,
                _chapterSwitchData.text,
                _chapterSwitchData.texture,
                _chapterSwitchData.wav,
                _chapterSwitchData.time);
            
            _isChapterSwitchPending = false;
            _chapterSwitchData = default;
        }
    }
}
