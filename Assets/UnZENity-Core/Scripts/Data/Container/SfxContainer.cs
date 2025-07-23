using System.Collections.Generic;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using MyBox;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Data.Container
{
    /// <summary>
    /// As there is a potential for multiple instances per key (e.g., BreathBubbles, BreathBubbles_A1, BreathBubbles_A2),
    /// we need to retrieve a container holding all of them.
    /// </summary>
    public class SfxContainer
    {
        private SoundEffectInstance[]  _soundEffects;

        public int Count => _soundEffects.Length;

        
        public SfxContainer(string preparedKey)
        {
            var sounds = new List<SoundEffectInstance>();
            
            var firstSound = GameData.SfxVm.InitInstance<SoundEffectInstance>(preparedKey);
            sounds.Add(firstSound);

            // Check if we have additional sounds which will be picked randomly at runtime.
            var randomIndex = 1;
            do
            {
                // e.g., BreathBubbles_A2
                var nextKey = $"{preparedKey}_A{randomIndex}";
                var nextSound = GameData.SfxVm.InitInstance<SoundEffectInstance>(nextKey);

                if (nextSound == null)
                {
                    break;
                }
                else
                {
                    sounds.Add(nextSound);
                    randomIndex++;
                }
            } while (true);
            
            _soundEffects = sounds.ToArray();
        }

        public SoundEffectInstance GetFirstSound()
        {
            return _soundEffects[0];
        }
        
        public SoundEffectInstance GetRandomSound()
        {
            return _soundEffects.Length == 1 ? GetFirstSound() : _soundEffects.GetRandom();
        }

        public AudioClip GetFirstClip()
        {
            return GetFirstSound().ToAudioClip();
        }

        public AudioClip GetRandomClip()
        {
            return GetRandomSound().ToAudioClip();
        }
    }
}
