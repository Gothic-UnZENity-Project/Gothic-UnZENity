using System;
using System.Collections.Generic;
using GUZ.Core.Const;
using GUZ.Core.Services;
using MyBox;
using Reflex.Attributes;
using ZenKit.Daedalus;

namespace GUZ.Core.Models.Audio
{
    /// <summary>
    /// As there is a potential for multiple instances per key (e.g., BreathBubbles, BreathBubbles_A1, BreathBubbles_A2),
    /// we need to retrieve a container holding all of them.
    /// </summary>
    public class SfxModel
    {
        // TODO - Injecting in a Model wasn't intended. Try to use it without injecting.
        [Inject] private readonly GameStateService _gameStateService;

        private string soundKey;
        private SoundEffectInstance[]  _soundEffects;

        public int Count => _soundEffects.Length;

        
        public SfxModel(string preparedKey)
        {
            soundKey = preparedKey;
        }

        public SoundEffectInstance GetFirstSound()
        {
            if (_soundEffects == null)
                LoadSoundEffects();

            return _soundEffects[0];
        }
        
        public SoundEffectInstance GetRandomSound()
        {
            if (_soundEffects == null)
                LoadSoundEffects();
            
            return _soundEffects.Length == 1 ? GetFirstSound() : _soundEffects.GetRandom();
        }

        private void LoadSoundEffects()
        {
            var sounds = new List<SoundEffectInstance>();
            
            var firstSound = _gameStateService.SfxVm.InitInstance<SoundEffectInstance>(soundKey);
            sounds.Add(firstSound);

            // Check if we have additional sounds which will be picked randomly at runtime.
            var randomIndex = 1;
            do
            {
                // e.g., BreathBubbles_A2
                var nextKey = $"{soundKey}_A{randomIndex}";
                try
                {
                    var nextSound = _gameStateService.SfxVm.InitInstance<SoundEffectInstance>(nextKey);
                    
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
    }
}
