using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace TowerGuard.UI
{
    /// <summary>
    /// One level card on the LevelSelect scene. Loads star count from PlayerPrefs
    /// on enable, paints the 3-star row in gold/grey, and (when unlocked) routes
    /// the PLAY button to a scene load.
    /// </summary>
    public class LevelCardUI : MonoBehaviour
    {
        [SerializeField] private string prefsStarsKey = "level1_stars";
        [SerializeField] private string sceneToLoad = "Level_01";
        [SerializeField] private bool isUnlocked = true;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image[] starImages = new Image[3];
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private CanvasGroup lockedDimmerGroup;

        [SerializeField] private Color starEarnedColor = new Color(0.961f, 0.651f, 0.137f, 1f);
        [SerializeField] private Color starUnearnedColor = new Color(0.266f, 0.266f, 0.266f, 1f);

        private void OnEnable()
        {
            ApplyLockState();
            RefreshStars();
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
        }

        private void ApplyLockState()
        {
            // Polish fix: the dimmer is a white-tinted CanvasGroup whose alpha
            // controls how washed-out the card looks. Unlocked cards must NOT
            // be dimmed (alpha 0), or they render as a solid white block. The
            // separate lockOverlay handles the dim treatment for locked cards.
            if (lockedDimmerGroup != null) lockedDimmerGroup.alpha = 0f;
            if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
            if (playButton != null) playButton.interactable = isUnlocked;
        }

        private void RefreshStars()
        {
            int earned = PlayerPrefs.GetInt(prefsStarsKey, 0);
            earned = Mathf.Clamp(earned, 0, 3);
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = i < earned ? starEarnedColor : starUnearnedColor;
                }
            }
        }

        private void OnPlay()
        {
            if (!isUnlocked || string.IsNullOrEmpty(sceneToLoad)) return;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
