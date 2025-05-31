using System.Linq;
using GUZ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace GUZ.Lab.Handler
{
    public class LabVideoHandler : AbstractLabHandler
    {
        [SerializeField] private TMP_Dropdown _fileSelector;
        [SerializeField] private VideoPlayer _videoPlayer;
        

        public override void Bootstrap()
        {
            _fileSelector.options = GameGlobals.Video.VideoFileNamesMp4.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void VideoPlayClick()
        {
            _videoPlayer.url = GameGlobals.Video.VideoFilePathsMp4[_fileSelector.value];
            _videoPlayer.Play();
        }
    }
}
