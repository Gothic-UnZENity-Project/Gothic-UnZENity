using System.Linq;
using GUZ.Core;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabMusicHandler : AbstractLabHandler
    {
        public TMP_Dropdown FileSelector;

        public MusicManager MusicManager;

        public override void Bootstrap()
        {
            var vm = ResourceLoader.TryGetDaedalusVm("MUSIC");

            var musicInstances = vm.GetInstanceSymbols("C_MUSICTHEME")
                .Select(s => s.Name)
                .ToList();

            FileSelector.options = musicInstances.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void MusicPlayClick()
        {
            MusicManager.Play(FileSelector.options[FileSelector.value].text);
        }
    }
}
