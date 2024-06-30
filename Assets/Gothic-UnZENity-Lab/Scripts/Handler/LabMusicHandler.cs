using System.Linq;
using GUZ.Core;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabMusicHandler : MonoBehaviour, ILabHandler
    {
        [FormerlySerializedAs("fileSelector")] public TMP_Dropdown FileSelector;


        public void Bootstrap()
        {
            var vm = ResourceLoader.TryGetDaedalusVm("MUSIC");

            var musicInstances = vm.GetInstanceSymbols("C_MUSICTHEME")
                .Select(s => s.Name)
                .ToList();

            FileSelector.options = musicInstances.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void MusicPlayClick()
        {
            MusicManager.I.Play(FileSelector.options[FileSelector.value].text);
        }
    }
}
