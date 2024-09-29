using System.Linq;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabSoundHandler : AbstractLabHandler
    {
        [FormerlySerializedAs("fileSelector")] public TMP_Dropdown FileSelector;


        public override void Bootstrap()
        {
            var vm = ResourceLoader.TryGetDaedalusVm("SFX");

            var sfxInstances = vm.GetInstanceSymbols("C_SFX")
                .Select(s => s.Name)
                .ToList();

            FileSelector.options = sfxInstances.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void SoundPlayClick()
        {
            GetComponent<AudioSource>().clip = SoundCreator.ToAudioClip(FileSelector.options[FileSelector.value].text);
            GetComponent<AudioSource>().loop = true;
            GetComponent<AudioSource>().Play();
        }
    }
}
