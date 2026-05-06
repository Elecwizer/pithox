using System.Collections;
using TMPro;
using UnityEngine;

namespace Pithox.Game
{
    /// <summary>Faded overlay + Wave # announcement.</summary>
    public class WaveBannerUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text waveLabel;
        [SerializeField] string waveFormat = "Wave {0}";
        [SerializeField] float fadeInSeconds = 0.35f;
        [SerializeField] float holdSeconds = 1.1f;
        [SerializeField] float fadeOutSeconds = 0.45f;

        Coroutine running;

        void Awake()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        public void ShowWave(int waveNumber)
        {
            if (waveLabel != null)
                waveLabel.text = string.Format(waveFormat, waveNumber);

            if (running != null)
                StopCoroutine(running);

            running = StartCoroutine(BannerRoutine());
        }

        IEnumerator BannerRoutine()
        {
            if (canvasGroup == null)
                yield break;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeInSeconds));
                yield return null;
            }

            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(holdSeconds);

            t = 0f;
            float startAlpha = canvasGroup.alpha;
            while (t < fadeOutSeconds)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / Mathf.Max(0.01f, fadeOutSeconds));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            running = null;
        }
    }
}
