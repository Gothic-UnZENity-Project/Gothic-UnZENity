using System.Linq;
using GUZ.Core;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabVideoHandler : MonoBehaviour, ILabHandler
    {
        [SerializeField] private TMP_Dropdown _fileSelector;


        public void Bootstrap()
        {
            _fileSelector.options = GameGlobals.Video.VideoFileNames.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void VideoPlayClick()
        {
            // TBD
        }
    }
}
