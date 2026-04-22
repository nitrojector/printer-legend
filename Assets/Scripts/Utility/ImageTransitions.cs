using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Utility
{
    public class ImageTransitions
    {
        /// <summary>
        /// Fades in the image, waits for the specified duration, then fades it out.
        /// </summary>
        /// <param name="image">image to operate on</param>
        /// <param name="duration">duration image should be shown with full alpha</param>
        /// <param name="fadeInDuration">duration of fade in animation</param>
        /// <param name="fadeOutDuration">duration of fade out animation</param>
        public static IEnumerator ShowLinear(Image image, float duration, float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f)
        {
            image.gameObject.SetActive(true);
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            yield return FadeToLinear(image, 1f, fadeInDuration);
            yield return new WaitForSeconds(duration);
            yield return FadeToLinear(image, 0f, fadeOutDuration);
            image.gameObject.SetActive(false);
        }

        /// <summary>
        /// Fades the image's alpha to the target value over the specified duration using linear interpolation.
        /// </summary>
        /// <param name="image">image to operate on</param>
        /// <param name="target">target alpha</param>
        /// <param name="duration">duration for the fade</param>
        public static IEnumerator FadeToLinear(Image image, float target, float duration)
        {
            float start = image.color.a;
            float elapsed = 0f;
            Color color = image.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                image.color = color;
                yield return null;
            }
            color.a = target;
            image.color = color;
        }
    }
}
