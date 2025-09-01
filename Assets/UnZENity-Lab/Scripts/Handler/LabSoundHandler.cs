using System.Linq;
using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Manager;
using GUZ.Core.Services;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GUZ.Lab.Handler
{
    public class LabSoundHandler : AbstractLabHandler
    {
        public TMP_Dropdown FileSelector;

        [Inject] private readonly AudioService _audioService;

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
            GetComponent<AudioSource>().clip = _audioService.CreateAudioClip(FileSelector.options[FileSelector.value].text);
            GetComponent<AudioSource>().loop = true;
            GetComponent<AudioSource>().Play();
        }
    }
}
