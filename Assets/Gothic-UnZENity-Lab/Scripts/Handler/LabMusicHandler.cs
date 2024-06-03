using System;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Manager;
using TMPro;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabMusicHandler : MonoBehaviour, ILabHandler
    {
        public TMP_Dropdown fileSelector;


        public void Bootstrap()
        {
            var prototype = GameData.MusicVm.GetSymbolByName("C_MUSICTHEME_DEF");

            var musicInstances = GameData.MusicVm.Symbols
                .Where(s => s.Parent == prototype.Index)
                .Select(s => AssetCache.TryGetMusic(s.Name))
                .GroupBy(instance => instance.File, StringComparer.InvariantCultureIgnoreCase)
                .Select(group => group.First())
                .OrderBy(instance => instance.File)
                .ToList();

            fileSelector.options = musicInstances.Select(i => new TMP_Dropdown.OptionData(i.File)).ToList();
        }

        public void MusicPlayClick()
        {
            if (!FeatureFlags.I.enableMusic)
                Debug.LogError($"Music is deactivated inside ${nameof(FeatureFlags.enableMusic)}");

            MusicManager.Play(fileSelector.options[fileSelector.value].text);
        }
    }
}
