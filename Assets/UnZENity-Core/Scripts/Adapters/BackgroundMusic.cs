using GUZ.Core.Manager;
using Reflex.Attributes;
using UnityEngine;

namespace GUZ.Core.Adapters
{
    public class BackgroundMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioReverbFilter _audioReverbFilter;

        [Inject] private readonly MusicService _musicService;

        private void Start()
        {
            _musicService.SetBackgroundMusic(_audioSource, _audioReverbFilter);
        }
    }
}
