using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Towers;
using TowerGuard.Monetization;

namespace TowerGuard.UI
{
    /// <summary>
    /// Gameplay-scene UI orchestrator. Owns every HUD element, panel, and toast
    /// in Level_01, subscribes to GameManager + WaveManager events, and drives
    /// LeanTween animations for every transition.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD text (TopBar)")]
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text softCurrencyText;
        [SerializeField] private TMP_Text hardCurrencyText;

        [Header("Selected-tower panel (bottom-center)")]
        [SerializeField] private GameObject selectedTowerPanel;
        [SerializeField] private TMP_Text towerNameText;
        [SerializeField] private TMP_Text towerStatsText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellButtonText;
        [SerializeField] private Button closeTowerPanelButton;
        [SerializeField] private Button towerPanelBackdropButton;

        [Header("Pause overlay")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private CanvasGroup pauseCanvasGroup;

        [Header("Game Over panel")]
        [SerializeField] private RectTransform gameOverPanel;
        [SerializeField] private TMP_Text gameOverWaveText;
        [SerializeField] private GameObject continueAdPanel;
        [SerializeField] private Button continueAdWatchButton;
        [SerializeField] private Button continueAdDeclineButton;

        [Header("Victory panel")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Image[] victoryStars = new Image[3];
        [SerializeField] private TMP_Text victoryEnemiesText;
        [SerializeField] private TMP_Text victoryGoldText;
        [SerializeField] private GameObject removeAdsPrompt;

        [Header("Wave Complete toast")]
        [SerializeField] private GameObject waveCompleteToast;
        [SerializeField] private TMP_Text waveCompleteText;
        [SerializeField] private CanvasGroup waveCompleteCanvasGroup;

        [Header("Generic Toast (Phase 4)")]
        [SerializeField] private GameObject genericToast;
        [SerializeField] private TMP_Text genericToastText;
        [SerializeField] private CanvasGroup genericToastCanvasGroup;

        [Header("Double Reward panel (Phase 4)")]
        [SerializeField] private GameObject doubleRewardPanel;
        [SerializeField] private TMP_Text doubleRewardCountdownText;
        [SerializeField] private Button doubleRewardWatchButton;
        [SerializeField] private Button doubleRewardDeclineButton;

        [Header("Tower cards (bottom bar)")]
        [SerializeField] private TowerCardUI[] towerCards = new TowerCardUI[4];

        [Header("Start-wave pulsing button")]
        [SerializeField] private Button startWaveButton;
        [SerializeField] private RectTransform startWaveButtonRect;

        [Header("Speed button")]
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedButtonText;

        [Header("Pause button")]
        [SerializeField] private Button pauseButton;

        [Header("Colors")]
        [SerializeField] private Color starEarnedColor = new Color(0.961f, 0.651f, 0.137f, 1f);   // #F5A623
        [SerializeField] private Color starUnearnedColor = new Color(0.266f, 0.266f, 0.266f, 1f);  // #444

        // --- Runtime state ---
        private bool shownContinueOffer;
        private Coroutine waveToastRoutine;
        private Coroutine genericToastRoutine;
        private Coroutine doubleRewardRoutine;
        private int startWaveTweenId = -1;
        private int lastDoubleRewardWaveIndex = -10; // ensure first offer is allowed
        private const int DoubleRewardMinGap = 3;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            HideAllOverlaysOnStart();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameManager.OnHPChanged += UpdateHP;
            GameManager.OnSoftCurrencyChanged += UpdateSoftCurrency;
            GameManager.OnHardCurrencyChanged += UpdateHardCurrency;
            GameManager.OnWaveChanged += UpdateWave;
            GameManager.OnGameOver += ShowGameOver;
            GameManager.OnVictory += ShowVictory;
            WaveManager.OnWaveComplete += OnWaveComplete;
            TowerPlacement.OnTowerSelected += ShowTowerPanel;
            TowerPlacement.OnTowerDeselected += HideTowerPanel;

            WireButtons();
        }

        private void OnDisable()
        {
            GameManager.OnHPChanged -= UpdateHP;
            GameManager.OnSoftCurrencyChanged -= UpdateSoftCurrency;
            GameManager.OnHardCurrencyChanged -= UpdateHardCurrency;
            GameManager.OnWaveChanged -= UpdateWave;
            GameManager.OnGameOver -= ShowGameOver;
            GameManager.OnVictory -= ShowVictory;
            WaveManager.OnWaveComplete -= OnWaveComplete;
            TowerPlacement.OnTowerSelected -= ShowTowerPanel;
            TowerPlacement.OnTowerDeselected -= HideTowerPanel;
        }

        private void Start()
        {
            // Initial paint with whatever state GameManager holds.
            if (GameManager.Instance != null)
            {
                UpdateHP(GameManager.Instance.PlayerHP);
                UpdateSoftCurrency(GameManager.Instance.SoftCurrency);
                UpdateHardCurrency(GameManager.Instance.HardCurrency);
                UpdateWave(GameManager.Instance.CurrentWave);
            }
            UpdateSpeedButton();
            StartPulsingStartButton();
        }

        // =====================================================================
        // Initial state
        // =====================================================================
        private void HideAllOverlaysOnStart()
        {
            if (selectedTowerPanel != null) selectedTowerPanel.SetActive(false);
            // The tower-panel backdrop is a full-stretch transparent Image that
            // blocks raycasts when active. Keep it OFF until ShowTowerPanel is
            // called, otherwise it captures every play-area click and prevents
            // tower placement.
            if (towerPanelBackdropButton != null) towerPanelBackdropButton.gameObject.SetActive(false);
            if (pauseOverlay != null) pauseOverlay.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);
            if (continueAdPanel != null) continueAdPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (waveCompleteToast != null) waveCompleteToast.SetActive(false);
            if (removeAdsPrompt != null) removeAdsPrompt.SetActive(false);
            if (genericToast != null) genericToast.SetActive(false);
            if (doubleRewardPanel != null) doubleRewardPanel.SetActive(false);
        }

        private void WireButtons()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradePressed);
            }
            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(OnSellPressed);
            }
            if (closeTowerPanelButton != null)
            {
                closeTowerPanelButton.onClick.RemoveAllListeners();
                closeTowerPanelButton.onClick.AddListener(HideTowerPanel);
            }
            if (towerPanelBackdropButton != null)
            {
                towerPanelBackdropButton.onClick.RemoveAllListeners();
                towerPanelBackdropButton.onClick.AddListener(HideTowerPanel);
            }
            if (startWaveButton != null)
            {
                startWaveButton.onClick.RemoveAllListeners();
                startWaveButton.onClick.AddListener(OnStartWavePressed);
            }
            if (speedButton != null)
            {
                speedButton.onClick.RemoveAllListeners();
                speedButton.onClick.AddListener(OnSpeedPressed);
            }
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(OnPausePressed);
            }
            if (continueAdWatchButton != null)
            {
                continueAdWatchButton.onClick.RemoveAllListeners();
                continueAdWatchButton.onClick.AddListener(OnContinueAdAccepted);
            }
            if (continueAdDeclineButton != null)
            {
                continueAdDeclineButton.onClick.RemoveAllListeners();
                continueAdDeclineButton.onClick.AddListener(OnContinueAdDeclined);
            }
            if (doubleRewardWatchButton != null)
            {
                doubleRewardWatchButton.onClick.RemoveAllListeners();
                doubleRewardWatchButton.onClick.AddListener(OnDoubleRewardAccepted);
            }
            if (doubleRewardDeclineButton != null)
            {
                doubleRewardDeclineButton.onClick.RemoveAllListeners();
                doubleRewardDeclineButton.onClick.AddListener(HideDoubleRewardPanel);
            }
        }

        // =====================================================================
        // HUD text
        // =====================================================================
        public void UpdateHP(int val)
        {
            if (hpText != null) hpText.text = val.ToString();
        }

        public void UpdateSoftCurrency(int val)
        {
            if (softCurrencyText != null) softCurrencyText.text = val.ToString();
            RefreshTowerCardAffordability(val);
            if (selectedTowerPanel != null && selectedTowerPanel.activeSelf && TowerPlacement.Instance != null && TowerPlacement.Instance.SelectedTower != null)
            {
                RefreshSelectedTowerPanel(TowerPlacement.Instance.SelectedTower);
            }
        }

        public void UpdateHardCurrency(int val)
        {
            if (hardCurrencyText != null) hardCurrencyText.text = val.ToString();
        }

        public void UpdateWave(int val)
        {
            if (waveText == null) return;
            int total = 10;
            if (TowerGuard.Core.GameManager.Instance != null)
            {
                var wm = GameObject.FindFirstObjectByType<WaveManager>();
                if (wm != null) total = Mathf.Max(1, wm.TotalWaves);
            }
            int shown = Mathf.Max(1, val);
            waveText.text = $"WAVE {shown} / {total}";
        }

        // =====================================================================
        // Pause
        // =====================================================================
        public void ShowPause()
        {
            if (pauseOverlay == null) return;
            pauseOverlay.SetActive(true);
            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.alpha = 0f;
                pauseCanvasGroup.blocksRaycasts = true;
                LeanTween.alphaCanvas(pauseCanvasGroup, 1f, 0.2f).setIgnoreTimeScale(true);
            }
        }

        public void HidePause()
        {
            if (pauseOverlay == null) return;
            if (pauseCanvasGroup != null) pauseCanvasGroup.blocksRaycasts = false;
            pauseOverlay.SetActive(false);
        }

        // =====================================================================
        // Tower panel
        // =====================================================================
        public void ShowTowerPanel(TowerBase tower)
        {
            if (selectedTowerPanel == null || tower == null) return;
            selectedTowerPanel.SetActive(true);
            // Activate the full-screen tap-to-close backdrop in lockstep with the
            // panel. The backdrop is a transparent raycast-blocking Image; if it
            // were left active when the panel is hidden, it would swallow every
            // click on the play area below and break tower placement.
            if (towerPanelBackdropButton != null)
            {
                towerPanelBackdropButton.gameObject.SetActive(true);
            }
            RefreshSelectedTowerPanel(tower);

            RectTransform rt = selectedTowerPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
                rt.localScale = startScale;
                LeanTween.scale(rt, Vector3.one, 0.18f).setEaseOutBack();
            }
        }

        public void HideTowerPanel()
        {
            if (selectedTowerPanel != null) selectedTowerPanel.SetActive(false);
            if (towerPanelBackdropButton != null)
            {
                towerPanelBackdropButton.gameObject.SetActive(false);
            }
            if (TowerPlacement.Instance != null && TowerPlacement.Instance.SelectedTower != null)
            {
                TowerPlacement.Instance.DeselectTower();
            }
        }

        private void RefreshSelectedTowerPanel(TowerBase tower)
        {
            if (tower == null || tower.Data == null) return;

            if (towerNameText != null)
            {
                towerNameText.text = tower.IsUpgraded
                    ? $"{tower.Data.towerName} ★"
                    : tower.Data.towerName;
            }

            if (towerStatsText != null)
            {
                float dmg = tower.IsUpgraded ? tower.Data.upgradedDamage : tower.Data.damage;
                float rng = tower.IsUpgraded ? tower.Data.upgradedRange : tower.Data.range;
                float rate = tower.IsUpgraded ? tower.Data.upgradedFireRate : tower.Data.fireRate;
                towerStatsText.text = $"DMG {dmg:0}   RANGE {rng:0.0}   RATE {rate:0.0}/s";
            }

            if (upgradeButton != null)
            {
                bool canUpgrade = !tower.IsUpgraded
                                  && GameManager.Instance != null
                                  && GameManager.Instance.SoftCurrency >= tower.Data.upgradeCost;
                upgradeButton.interactable = canUpgrade;
                if (upgradeButtonText != null)
                {
                    upgradeButtonText.text = tower.IsUpgraded
                        ? "Upgraded"
                        : $"Upgrade ({tower.Data.upgradeCost}g)";
                }
            }

            if (sellButton != null && sellButtonText != null)
            {
                int refund = (tower.Data.cost + (tower.IsUpgraded ? tower.Data.upgradeCost : 0)) / 2;
                sellButtonText.text = $"Sell (+{refund}g)";
            }
        }

        private void OnUpgradePressed()
        {
            if (TowerPlacement.Instance == null) return;
            TowerBase t = TowerPlacement.Instance.SelectedTower;
            if (t == null) return;
            if (t.Upgrade())
            {
                RefreshSelectedTowerPanel(t);
            }
        }

        private void OnSellPressed()
        {
            if (TowerPlacement.Instance == null) return;
            TowerBase t = TowerPlacement.Instance.SelectedTower;
            if (t == null) return;
            t.Sell();
            HideTowerPanel();
        }

        // =====================================================================
        // Tower cards (bottom bar affordability)
        // =====================================================================
        private void RefreshTowerCardAffordability(int softCurrency)
        {
            if (towerCards == null) return;
            for (int i = 0; i < towerCards.Length; i++)
            {
                if (towerCards[i] != null) towerCards[i].RefreshAffordability(softCurrency);
            }
        }

        // =====================================================================
        // Control buttons
        // =====================================================================
        private void OnStartWavePressed()
        {
            WaveManager wm = GameObject.FindFirstObjectByType<WaveManager>();
            if (wm != null) wm.StartNextWave();
        }

        private void OnSpeedPressed()
        {
            if (TowerGuard.Utils.SpeedController.Instance != null)
            {
                TowerGuard.Utils.SpeedController.Instance.ToggleSpeed();
                UpdateSpeedButton();
            }
        }

        private void UpdateSpeedButton()
        {
            if (speedButtonText == null) return;
            bool fast = TowerGuard.Utils.SpeedController.Instance != null
                        && TowerGuard.Utils.SpeedController.Instance.IsFast;
            speedButtonText.text = fast ? "2x" : "1x";
        }

        private void OnPausePressed()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.PauseGame();
            ShowPause();
        }

        private void StartPulsingStartButton()
        {
            if (startWaveButtonRect == null) return;
            if (startWaveTweenId != -1) LeanTween.cancel(startWaveTweenId);
            startWaveTweenId = LeanTween.scale(startWaveButtonRect, new Vector3(1.06f, 1.06f, 1f), 0.6f)
                .setEaseInOutSine()
                .setLoopPingPong()
                .id;
        }

        // =====================================================================
        // Game Over + rewarded-ad continue
        // =====================================================================
        public void ShowGameOver()
        {
            if (gameOverPanel == null) return;
            gameOverPanel.gameObject.SetActive(true);

            if (gameOverWaveText != null && GameManager.Instance != null)
            {
                gameOverWaveText.text = $"You reached Wave {Mathf.Max(1, GameManager.Instance.CurrentWave)}";
            }

            // Slide up from bottom.
            float h = Screen.height > 0 ? Screen.height : 844f;
            gameOverPanel.anchoredPosition = new Vector2(0f, -h);
            LeanTween.move(gameOverPanel, new Vector3(gameOverPanel.position.x, 0f, 0f), 0.4f)
                .setEaseOutQuart()
                .setIgnoreTimeScale(true);
            LeanTween.value(gameOverPanel.gameObject, v => gameOverPanel.anchoredPosition = new Vector2(0f, v), -h, 0f, 0.4f)
                .setEaseOutQuart()
                .setIgnoreTimeScale(true);

            if (!shownContinueOffer)
            {
                shownContinueOffer = true;
                StartCoroutine(ShowContinueAdAfterDelay(1.5f));
            }
        }

        private IEnumerator ShowContinueAdAfterDelay(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ShowRewardedAdPrompt(OnContinueAdAccepted, OnContinueAdDeclined);
        }

        public void ShowRewardedAdPrompt(Action onAccept, Action onDecline)
        {
            if (continueAdPanel == null) return;
            continueAdPanel.SetActive(true);
            RectTransform rt = continueAdPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                float h = rt.rect.height > 0 ? rt.rect.height : 200f;
                Vector2 start = new Vector2(rt.anchoredPosition.x, -h);
                Vector2 end = new Vector2(rt.anchoredPosition.x, 20f);
                rt.anchoredPosition = start;
                LeanTween.value(rt.gameObject, v => rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, v), start.y, end.y, 0.35f)
                    .setEaseOutQuart()
                    .setIgnoreTimeScale(true);
            }
        }

        private void OnContinueAdAccepted()
        {
            if (Monetization.AnalyticsManager.Instance != null)
            {
                Monetization.AnalyticsManager.Instance.TrackRewardedAdAccepted("continue_after_death");
            }
            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowRewardedAd(success =>
                {
                    if (success && GameManager.Instance != null)
                    {
                        GameManager.Instance.SetHP(5);
                        GameManager.Instance.ContinueAfterDeath(5);
                        HideGameOver();
                    }
                    else
                    {
                        if (continueAdPanel != null) continueAdPanel.SetActive(false);
                    }
                });
            }
            else
            {
                if (continueAdPanel != null) continueAdPanel.SetActive(false);
            }
        }

        private void OnContinueAdDeclined()
        {
            if (continueAdPanel != null) continueAdPanel.SetActive(false);
        }

        /// <summary>Hide the Game Over panel (used by the rewarded-ad continue flow).</summary>
        public void HideGameOver()
        {
            if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);
            if (continueAdPanel != null) continueAdPanel.SetActive(false);
            shownContinueOffer = false;
        }

        // =====================================================================
        // Double Wave Reward (Phase 4 rewarded ad flow #2)
        // =====================================================================

        /// <summary>
        /// Offer the player a rewarded ad in exchange for double the wave bonus.
        /// Enforces a minimum 3-wave gap between offers and never shows on the
        /// final wave. Auto-dismisses after 10 seconds.
        /// </summary>
        public void ShowDoubleRewardPanel(int waveIndex)
        {
            if (doubleRewardPanel == null) return;
            // Suppress when ads are removed: the player paid to skip ads, don't
            // pester them with one immediately after every wave.
            if (Monetization.AdManager.Instance != null && Monetization.AdManager.Instance.AreAdsRemoved()) return;
            if (waveIndex - lastDoubleRewardWaveIndex < DoubleRewardMinGap) return;

            lastDoubleRewardWaveIndex = waveIndex;
            doubleRewardPanel.SetActive(true);
            if (doubleRewardRoutine != null) StopCoroutine(doubleRewardRoutine);
            doubleRewardRoutine = StartCoroutine(DoubleRewardCountdown(10f));
        }

        private IEnumerator DoubleRewardCountdown(float seconds)
        {
            float t = seconds;
            while (t > 0f && doubleRewardPanel != null && doubleRewardPanel.activeSelf)
            {
                if (doubleRewardCountdownText != null)
                {
                    doubleRewardCountdownText.text = Mathf.CeilToInt(t).ToString() + "s";
                }
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            HideDoubleRewardPanel();
        }

        private void HideDoubleRewardPanel()
        {
            if (doubleRewardPanel != null) doubleRewardPanel.SetActive(false);
            if (doubleRewardRoutine != null) { StopCoroutine(doubleRewardRoutine); doubleRewardRoutine = null; }
        }

        private void OnDoubleRewardAccepted()
        {
            if (Monetization.AdManager.Instance == null)
            {
                HideDoubleRewardPanel();
                return;
            }
            if (Monetization.AnalyticsManager.Instance != null)
            {
                Monetization.AnalyticsManager.Instance.TrackRewardedAdAccepted("double_wave_bonus");
            }
            Monetization.AdManager.Instance.ShowRewardedAd(rewarded =>
            {
                if (rewarded && GameManager.Instance != null)
                {
                    GameManager.Instance.EarnSoftCurrency(20);
                    ShowToast("+20 Bonus Coins!", 1.5f);
                }
                HideDoubleRewardPanel();
            });
        }

        // =====================================================================
        // Generic toast (Phase 4)
        // =====================================================================
        public void ShowToast(string msg, float seconds = 2f)
        {
            if (genericToast == null || genericToastText == null)
            {
                Debug.Log($"[UIManager] (toast) {msg}");
                return;
            }
            genericToastText.text = msg;
            if (genericToastRoutine != null) StopCoroutine(genericToastRoutine);
            genericToastRoutine = StartCoroutine(GenericToastRoutine(seconds));
        }

        private IEnumerator GenericToastRoutine(float seconds)
        {
            genericToast.SetActive(true);
            if (genericToastCanvasGroup != null)
            {
                genericToastCanvasGroup.alpha = 0f;
                LeanTween.alphaCanvas(genericToastCanvasGroup, 1f, 0.2f).setIgnoreTimeScale(true);
            }
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            if (genericToastCanvasGroup != null)
            {
                LeanTween.alphaCanvas(genericToastCanvasGroup, 0f, 0.2f).setIgnoreTimeScale(true);
            }
            float f = 0f;
            while (f < 0.2f) { f += Time.unscaledDeltaTime; yield return null; }
            genericToast.SetActive(false);
            genericToastRoutine = null;
        }

        // =====================================================================
        // Victory
        // =====================================================================
        public void ShowVictory()
        {
            if (victoryPanel == null) return;
            victoryPanel.SetActive(true);

            int hp = GameManager.Instance != null ? GameManager.Instance.PlayerHP : 0;
            int stars = hp >= 15 ? 3 : hp >= 8 ? 2 : 1;

            if (victoryEnemiesText != null && GameManager.Instance != null)
            {
                victoryEnemiesText.text = $"Enemies defeated: {GameManager.Instance.EnemiesDefeatedThisRun}";
            }
            if (victoryGoldText != null && GameManager.Instance != null)
            {
                victoryGoldText.text = $"Gold earned: {GameManager.Instance.TotalSoftCurrencyEarnedThisRun}";
            }

            StartCoroutine(AnimateVictoryStars(stars));
            StartCoroutine(MaybeShowRemoveAdsAfter(2f));

            // Also persist stars on the level-1 prefs slot so LevelSelect reads the high score.
            int bestStars = PlayerPrefs.GetInt("level1_stars", 0);
            if (stars > bestStars) PlayerPrefs.SetInt("level1_stars", stars);
        }

        private IEnumerator AnimateVictoryStars(int earned)
        {
            if (victoryStars == null) yield break;
            for (int i = 0; i < victoryStars.Length; i++)
            {
                if (victoryStars[i] == null) continue;
                bool isEarned = i < earned;
                victoryStars[i].color = isEarned ? starEarnedColor : starUnearnedColor;
                victoryStars[i].rectTransform.localScale = Vector3.zero;
                LeanTween.scale(victoryStars[i].rectTransform, Vector3.one, 0.25f)
                    .setEaseOutBack()
                    .setIgnoreTimeScale(true);
                float wait = 0.15f;
                float t = 0f;
                while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
            }
        }

        private IEnumerator MaybeShowRemoveAdsAfter(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            if (removeAdsPrompt == null) yield break;
            bool adsRemoved = IAPManager.Instance != null && IAPManager.Instance.AreAdsRemoved();
            if (adsRemoved) yield break;
            removeAdsPrompt.SetActive(true);
        }

        // =====================================================================
        // Wave complete toast
        // =====================================================================
        public void OnWaveComplete(int waveIndex)
        {
            if (waveCompleteToast != null)
            {
                if (waveCompleteText != null) waveCompleteText.text = $"Wave {waveIndex + 1} Complete!";
                if (waveToastRoutine != null) StopCoroutine(waveToastRoutine);
                waveToastRoutine = StartCoroutine(WaveToastRoutine());
            }
            // Phase 4: offer the rewarded "double wave bonus" ad — but never on the
            // last wave (Victory takes over) and gated by the 3-wave minimum gap.
            if (waveIndex < 9)
            {
                StartCoroutine(DelayedDoubleReward(waveIndex, 1.0f));
            }
        }

        private IEnumerator DelayedDoubleReward(int waveIndex, float delaySec)
        {
            float t = 0f;
            while (t < delaySec) { t += Time.unscaledDeltaTime; yield return null; }
            ShowDoubleRewardPanel(waveIndex);
        }

        private IEnumerator WaveToastRoutine()
        {
            waveCompleteToast.SetActive(true);
            if (waveCompleteCanvasGroup != null)
            {
                waveCompleteCanvasGroup.alpha = 0f;
                LeanTween.alphaCanvas(waveCompleteCanvasGroup, 1f, 0.25f).setIgnoreTimeScale(true);
            }
            float t = 0f;
            while (t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            if (waveCompleteCanvasGroup != null)
            {
                LeanTween.alphaCanvas(waveCompleteCanvasGroup, 0f, 0.25f).setIgnoreTimeScale(true);
            }
            float f = 0f;
            while (f < 0.25f) { f += Time.unscaledDeltaTime; yield return null; }
            waveCompleteToast.SetActive(false);
            waveToastRoutine = null;
        }
    }
}
