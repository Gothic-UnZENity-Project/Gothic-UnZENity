using System.Collections;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GUZ.Core.Player.Camera
{
    public class CameraFade : SingletonBehaviour<CameraFade>
    {
        [FormerlySerializedAs("cameraFadeImage")]
        public Image CameraFadeImage;

        public const float DefaultCameraFadeDuration = 0.15f;

        private void Start()
        {
            Fade(DefaultCameraFadeDuration, 0);
        }

        public void Fade(float duration, float targetAlpha)
        {
            StartCoroutine(FadeCamera(duration, targetAlpha));
        }

        private IEnumerator FadeCamera(float duration, float targetAlpha)
        {
            float currentTime = 0;

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                CameraFadeImage.color = Color.Lerp(CameraFadeImage.color,
                    new Color(CameraFadeImage.color.r, CameraFadeImage.color.g, CameraFadeImage.color.b, targetAlpha),
                    currentTime / duration);
                yield return null;
            }
        }
    }
}
