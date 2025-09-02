using System;
using System.Collections.Generic;
using GUZ.Core.Globals;
using MyBox;
using ZenKit.Daedalus;

namespace GUZ.Core.Models.Audio
{
    /// <summary>
    /// As there is a potential for multiple instances per key (e.g., BreathBubbles, BreathBubbles_A1, BreathBubbles_A2),
    /// we need to retrieve a container holding all of them.
    /// </summary>
    public class SfxModel
    {
        private SoundEffectInstance[]  _soundEffects;

        public int Count => _soundEffects.Length;

        
        public SfxModel(string preparedKey)
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
                try
                {
                    var nextSound = GameData.SfxVm.InitInstance<SoundEffectInstance>(nextKey);
                    
                    // Hint: We also add nosound.wav entries. In G1, e.g., MOL_Ambient_A4 which is randomly picked, sometimes do not yell a sound - intended.
                    sounds.Add(nextSound);
                    randomIndex++;
                }
                catch (Exception)
                {
                    // Ignore
                    break;
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
    }
}
