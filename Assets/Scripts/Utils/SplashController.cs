using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerGuard.Utils
{
    /// <summary>
    /// SplashScene controller — fades the JCreates logo in over 0.8 s, holds 1.5 s,
    /// fades it out over 0.6 s, and loads MainMenu. Tap-to-skip respected.
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private Image logoImage;
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private float fadeIn = 0.8f;
        [SerializeField] private float hold   = 1.5f;
        [SerializeField] private float fadeOut = 0.6f;

        private bool advancing;

        private void Start()
        {
            if (logoGroup != null) logoGroup.alpha = 0f;
            StartCoroutine(Run());
        }

        private void Update()
        {
            // Tap-to-skip on iOS / mouse click in Editor.
            if (advancing) return;
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                StopAllCoroutines();
                StartCoroutine(SkipToNext());
            }
        }

        private IEnumerator Run()
        {
            // Fade in
            if (logoGroup != null) LeanTween.alphaCanvas(logoGroup, 1f, fadeIn).setIgnoreTimeScale(true);
            yield return new WaitForSecondsRealtime(fadeIn);
            // Hold
            yield return new WaitForSecondsRealtime(hold);
            // Fade out
            if (logoGroup != null) LeanTween.alphaCanvas(logoGroup, 0f, fadeOut).setIgnoreTimeScale(true);
            yield return new WaitForSecondsRealtime(fadeOut);
            advancing = true;
            SceneManager.LoadScene(nextSceneName);
        }

        private IEnumerator SkipToNext()
        {
            advancing = true;
            if (logoGroup != null) LeanTween.alphaCanvas(logoGroup, 0f, 0.15f).setIgnoreTimeScale(true);
            yield return new WaitForSecondsRealtime(0.15f);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
