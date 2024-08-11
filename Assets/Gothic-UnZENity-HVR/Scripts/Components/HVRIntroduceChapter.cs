using GUZ.Core;
using GUZ.Core.Creator.Sounds;
using GUZ.Core.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GUZ.HVR.Components
{
    /// <summary>
    /// UI logic handler for Daedalus call of IntroduceChapter()
    /// </summary>
    public class HVRIntroduceChapter : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Canvas _chapterCanvas;
        [SerializeField] private Image _chapterImage;
        [SerializeField] private TMP_Text _chapterTitle;
        [SerializeField] private TMP_Text _chapterSubtitle;
        

        public void DisplayIntroduction(string chapter, string text, string texture, string wav, int time)
        {
            PlayAudio(wav);
            ShowCover(chapter, text, texture, time);
        }

        private void PlayAudio(string wav)
        {
            var soundData = ResourceLoader.TryGetSound(wav);
            _audioSource.clip = SoundCreator.ToAudioClip(soundData);
            _audioSource.Play();
        }

        private void ShowCover(string chapter, string text, string texture, int time)
        {
            // Set canvas to follow player as world space overlay.
            _chapterCanvas.worldCamera = Camera.main;
            _chapterCanvas.planeDistance = 1;
            
            // Set texture for cover
            _chapterImage.material = GameGlobals.Textures.GetEmptyMaterial(MaterialExtension.BlendMode.Opaque);
            GameGlobals.Textures.SetTexture(texture, _chapterImage.material);

            _chapterTitle.text = chapter;
            _chapterSubtitle.text = text;
        }
    }
}
