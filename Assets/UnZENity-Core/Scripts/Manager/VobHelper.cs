using System;
using System.Linq;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Manager
{
    [Obsolete("Use VobManager instead.")]
    public static class VobHelper
    {
        private const float _lookupDistance = 10f; // meter
        private const string _noSoundName = "nosound.wav";

        [CanBeNull]
        public static (IInteractiveObject vob, GameObject go) GetFreeInteractableWithin10M(Vector3 position, string visualScheme)
        {
            if (!GameData.VobsInteractable.TryGetValue(visualScheme.ToUpper(), out var vobs))
                return default;
            
            return vobs
                .Where(pair => Vector3.Distance(pair.Vob.Position.ToUnityVector(), position) < _lookupDistance)
                .OrderBy(pair => Vector3.Distance(pair.Vob.Position.ToUnityVector(), position))
                .FirstOrDefault();
        }

        public static void ExtWldInsertItem(int itemInstance, string spawnpoint)
        {
            if (string.IsNullOrEmpty(spawnpoint) || itemInstance <= 0)
            {
                return;
            }

            var config = GameGlobals.Config;
            var activeTypes = config.Dev.SpawnVOBTypes.Value;
            if (config.Dev.EnableVOBs && (activeTypes.IsEmpty() || activeTypes.Contains(VirtualObjectType.oCItem)))
            {
                GameGlobals.Vobs.CreateItemMesh(itemInstance, spawnpoint);
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

            // We need to move into next elements starting from VobLoader root.
            var zm = go.transform.GetChild(0).GetChild(0);

            return zm.gameObject.GetAllDirectChildren()
                .Where(i => i.name.ContainsIgnoreCase("ZS"))
                .OrderBy(i => Vector3.Distance(i.transform.position, position))
                .FirstOrDefault();
        }

        public static AudioClip GetSoundClip(string soundName)
        {
            AudioClip clip;

            if (soundName.EqualsIgnoreCase(_noSoundName))
            {
                //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                return null;
            }

            // Bugfix - Normally the data is to get C_SFX_DEF entries from VM. But sometimes there might be the real .wav file stored.
            if (soundName.EndsWithIgnoreCase(".wav"))
            {
                clip = SoundCreator.ToAudioClip(soundName);
            }
            else
            {
                var sfxData = VmInstanceManager.TryGetSfxData(soundName);

                if (sfxData == null)
                {
                    return null;
                }

                if (sfxData.File.EqualsIgnoreCase(_noSoundName))
                {
                    //instead of decoding nosound.wav which might be decoded incorrectly, just return null
                    return null;
                }

                clip = SoundCreator.ToAudioClip(sfxData.File);
            }

            return clip;
        }
    }
}
