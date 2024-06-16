using System;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using GVR.Core;
using TMPro;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabMusicHandler : MonoBehaviour, ILabHandler
    {
        public TMP_Dropdown fileSelector;


        public void Bootstrap()
        {
            var vm = ResourceLoader.TryGetDaedalusVm("MUSIC");

            var musicInstances = vm.GetInstanceSymbols("C_MUSICTHEME")
                .Select(s => s.Name)
                .ToList();

            fileSelector.options = musicInstances.Select(i => new TMP_Dropdown.OptionData(i)).ToList();
        }

        public void MusicPlayClick()
        {
            if (!FeatureFlags.I.enableMusic)
                Debug.LogError($"Music is deactivated inside ${nameof(FeatureFlags.enableMusic)}");

            MusicManager.Play(fileSelector.options[fileSelector.value].text);
        }
    }
}
