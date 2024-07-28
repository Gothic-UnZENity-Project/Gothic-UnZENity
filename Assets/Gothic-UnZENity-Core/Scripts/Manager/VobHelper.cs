using System.Linq;
using GUZ.Core.Creator;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Data;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Manager
{
    public static class VobHelper
    {
        private const float _lookupDistance = 10f; // meter

        [CanBeNull]
        public static VobProperties GetFreeInteractableWithin10M(Vector3 position, string visualScheme)
        {
            return GameData.VobsInteractable
                .Where(i => Vector3.Distance(i.transform.position, position) < _lookupDistance)
                .Where(i => i.VisualScheme.EqualsIgnoreCase(visualScheme))
                .OrderBy(i => Vector3.Distance(i.transform.position, position))
                .FirstOrDefault();
        }

        public static void ExtWldInsertItem(int itemInstance, string spawnpoint)
        {
            if (string.IsNullOrEmpty(spawnpoint) || itemInstance <= 0)
            {
                return;
            }

            var config = GameGlobals.Config;
            var activeTypes = config.SpawnVOBTypes.Value;
            if (config.EnableVOBs && (activeTypes.IsEmpty() || activeTypes.Contains(VirtualObjectType.oCItem)))
            {
                VobCreator.CreateItem(itemInstance, spawnpoint, null);
            }
        }

        [CanBeNull]
        public static GameObject GetNearestSlot(GameObject go, Vector3 position)
        {
            var goTransform = go.transform;

            if (goTransform.childCount == 0)
            {
                return null;
            }

            var zm = go.transform.GetChild(0);

            return zm.gameObject.GetAllDirectChildren()
                .Where(i => i.name.ContainsIgnoreCase("ZS"))
                .OrderBy(i => Vector3.Distance(i.transform.position, position))
                .FirstOrDefault();
        }

        public static AudioClip GetSoundClip(string soundName)
        {
            SoundData soundData;

            // FIXME - move to EqualsIgnoreCase()
            if (soundName.ToLower() == "nosound.wav")
            {
                //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                return null;
            }

            // Bugfix - Normally the data is to get C_SFX_DEF entries from VM. But sometimes there might be the real .wav file stored.
            // FIXME - Move to EndsWithIgnoreCase()
            if (soundName.ToLower().EndsWith(".wav"))
            {
                soundData = ResourceLoader.TryGetSound(soundName);
            }
            else
            {
                var sfxData = VmInstanceManager.TryGetSfxData(soundName);

                if (sfxData == null)
                {
                    return null;
                }

                soundData = ResourceLoader.TryGetSound(sfxData.File);
            }

            if (soundData == null)
            {
                return null;
            }

            return SoundCreator.ToAudioClip(soundData);
        }
    }
}
