using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TowerGuard.UI
{
    /// <summary>
    /// Main Menu scene controller. Animates the title group on Scene start,
    /// wires the three bottom buttons, opens the shared SettingsPanel.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private RectTransform titleGroup;
        [SerializeField] private CanvasGroup titleCanvasGroup;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private GameObject creditsOverlay;
        [SerializeField] private Button creditsCloseButton;

        [SerializeField] private string levelSelectScene = "LevelSelect";
        [SerializeField] private float titleAnimDuration = 0.8f;
        [SerializeField] private float titleAnimOffset = 40f;

        private void Start()
        {
            // BuildSettingsPanel wraps the panel in a full-stretch dim "SettingsOverlay"
            // parent that has a raycast-blocking Image. Disabling only the inner panel
            // leaves the outer overlay covering the whole screen and silently blocking
            // every button click. Disable the outer overlay too.
            if (settingsPanel != null)
            {
                var outer = settingsPanel.transform.parent;
                if (outer != null) outer.gameObject.SetActive(false);
                settingsPanel.gameObject.SetActive(false);
            }
            if (creditsOverlay != null) creditsOverlay.SetActive(false);
            AnimateTitleIn();
        }

        private void OnEnable()
        {
            // Hide the SettingsOverlay (full-screen dim that blocks raycasts) here in
            // OnEnable as well as Start, so even if Start is delayed the menu buttons
            // are clickable from the very first frame.
            if (settingsPanel != null)
            {
                var outer = settingsPanel.transform.parent;
                if (outer != null) outer.gameObject.SetActive(false);
                settingsPanel.gameObject.SetActive(false);
            }
            if (creditsOverlay != null) creditsOverlay.SetActive(false);
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCredits);
            if (creditsCloseButton != null) creditsCloseButton.onClick.AddListener(HideCredits);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCredits);
            if (creditsCloseButton != null) creditsCloseButton.onClick.RemoveListener(HideCredits);
        }

        private void AnimateTitleIn()
        {
            if (titleGroup == null) return;
            Vector2 endPos = titleGroup.anchoredPosition;
            Vector2 startPos = new Vector2(endPos.x, endPos.y - titleAnimOffset);
            titleGroup.anchoredPosition = startPos;
            if (titleCanvasGroup != null) titleCanvasGroup.alpha = 0f;

            LeanTween.value(titleGroup.gameObject, v => titleGroup.anchoredPosition = new Vector2(endPos.x, v), startPos.y, endPos.y, titleAnimDuration)
                .setEaseOutCubic();
            if (titleCanvasGroup != null)
            {
                LeanTween.alphaCanvas(titleCanvasGroup, 1f, titleAnimDuration).setEaseOutCubic();
            }
        }

        private void OnPlay()
        {
            PunchButton(playButton);
            SceneManager.LoadScene(levelSelectScene);
        }

        private void OnSettings()
        {
            PunchButton(settingsButton);
            if (settingsPanel != null) settingsPanel.Show();
        }

        private void OnCredits()
        {
            PunchButton(creditsButton);
            if (creditsOverlay != null) creditsOverlay.SetActive(true);
        }

        private void HideCredits()
        {
            if (creditsOverlay != null) creditsOverlay.SetActive(false);
        }

        private void PunchButton(Button b)
        {
            if (b == null) return;
            RectTransform rt = b.GetComponent<RectTransform>();
            if (rt == null) return;
            LeanTween.cancel(rt);
            rt.localScale = Vector3.one;
            LeanTween.scale(rt, new Vector3(1.1f, 1.1f, 1f), 0.08f)
                .setEasePunch()
                .setOnComplete(() => rt.localScale = Vector3.one);
        }
    }
}
