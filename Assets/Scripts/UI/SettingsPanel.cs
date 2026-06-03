using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Monetization;

namespace TowerGuard.UI
{
    /// <summary>
    /// Shared settings panel used by both the Main Menu and the in-game Pause menu.
    /// Sliders write directly into AudioManager and PlayerPrefs on change.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private const string HapticsKey = "haptics_on";

        [SerializeField] private RectTransform rootRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private float slideDuration = 0.25f;

        private void Awake()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            LoadValues();
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestorePressed);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (hapticsToggle != null) hapticsToggle.onValueChanged.RemoveListener(OnHapticsChanged);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestorePressed);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            // Activate parent overlay too. Owners (MainMenuUI / PauseUI) keep the
            // outer dim "SettingsOverlay" parent disabled so it doesn't block UI
            // raycasts when the panel isn't visible. Re-enable it here so the
            // dim background shows and clicks fall through correctly.
            if (transform.parent != null) transform.parent.gameObject.SetActive(true);
            gameObject.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (rootRect != null)
            {
                rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, -600f);
                LeanTween.value(gameObject, v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v), -600f, 0f, slideDuration)
                    .setEaseOutQuart()
                    .setIgnoreTimeScale(true);
            }
            if (canvasGroup != null)
            {
                LeanTween.alphaCanvas(canvasGroup, 1f, slideDuration).setIgnoreTimeScale(true);
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                LeanTween.alphaCanvas(canvasGroup, 0f, slideDuration).setIgnoreTimeScale(true);
            }
            if (rootRect != null)
            {
                LeanTween.value(gameObject, v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v), rootRect.anchoredPosition.y, -600f, slideDuration)
                    .setEaseInQuart()
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        if (transform.parent != null) transform.parent.gameObject.SetActive(false);
                    });
            }
            else
            {
                gameObject.SetActive(false);
                if (transform.parent != null) transform.parent.gameObject.SetActive(false);
            }
        }

        private void LoadValues()
        {
            if (AudioManager.Instance != null)
            {
                float sfx = AudioManager.Instance.GetSFXVolume();
                float music = AudioManager.Instance.GetMusicVolume();
                if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
                if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
                UpdateSfxText(sfx);
                UpdateMusicText(music);
            }
            if (hapticsToggle != null)
            {
                bool on = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
                hapticsToggle.SetIsOnWithoutNotify(on);
            }
        }

        private void OnSfxChanged(float v)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
            UpdateSfxText(v);
        }

        private void OnMusicChanged(float v)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
            UpdateMusicText(v);
        }

        private void OnHapticsChanged(bool on)
        {
            PlayerPrefs.SetInt(HapticsKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnRestorePressed()
        {
            if (IAPManager.Instance != null) IAPManager.Instance.RestorePurchases();
        }

        private void UpdateSfxText(float v)
        {
            if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        private void UpdateMusicText(float v)
        {
            if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(v * 100f) + "%";
        }
    }
}
