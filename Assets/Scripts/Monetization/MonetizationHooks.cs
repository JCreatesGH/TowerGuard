using UnityEngine;
using TowerGuard.Core;
using TowerGuard.UI;

namespace TowerGuard.Monetization
{
    /// <summary>
    /// One-stop subscriber that wires gameplay events into the monetization +
    /// analytics layer. Place this on the GameManager GameObject in Level_01.
    ///
    /// Responsibilities:
    ///   - Forward wave/game-over/tower events to AnalyticsManager.
    ///   - Award the per-wave bonus (20 soft + 1 hard every 3 waves) on each
    ///     OnWaveComplete.
    ///   - Fire a (throttled) interstitial when the player loses.
    ///   - Surface a "Remove Ads" prompt 2s after victory if not yet purchased.
    /// </summary>
    public class MonetizationHooks : MonoBehaviour
    {
        private int waveStartHP;
        private int waveStartSoft;
        private int currentWaveIndex = -1;

        private void OnEnable()
        {
            WaveManager.OnWaveStart    += OnWaveStart;
            WaveManager.OnWaveComplete += OnWaveComplete;
            GameManager.OnGameOver     += OnGameOver;
            GameManager.OnVictory      += OnVictory;
        }

        private void OnDisable()
        {
            WaveManager.OnWaveStart    -= OnWaveStart;
            WaveManager.OnWaveComplete -= OnWaveComplete;
            GameManager.OnGameOver     -= OnGameOver;
            GameManager.OnVictory      -= OnVictory;
        }

        // =====================================================================
        // Wave events
        // =====================================================================
        private void OnWaveStart(int waveIndex)
        {
            currentWaveIndex = waveIndex;
            if (GameManager.Instance != null)
            {
                waveStartHP   = GameManager.Instance.PlayerHP;
                waveStartSoft = GameManager.Instance.SoftCurrency;
            }
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackWaveStarted(waveIndex);
            }
        }

        private void OnWaveComplete(int waveIndex)
        {
            int hp = GameManager.Instance != null ? GameManager.Instance.PlayerHP : 0;
            int soft = GameManager.Instance != null ? GameManager.Instance.SoftCurrency : 0;
            int earnedThisWave = soft - waveStartSoft;
            if (earnedThisWave < 0) earnedThisWave = 0;

            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackWaveCompleted(waveIndex, hp, earnedThisWave);
            }

            // Per-wave bonus reward, regardless of whether the player watched the ad.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EarnSoftCurrency(20);
                if (((waveIndex + 1) % 3) == 0)
                {
                    GameManager.Instance.EarnHardCurrency(1);
                }
            }
        }

        // =====================================================================
        // Game over → throttled interstitial
        // =====================================================================
        private void OnGameOver()
        {
            if (AnalyticsManager.Instance != null)
            {
                int wave = GameManager.Instance != null ? GameManager.Instance.CurrentWave : 0;
                AnalyticsManager.Instance.TrackGameOver(wave);
            }
            if (AdManager.Instance != null)
            {
                AdManager.Instance.ShowInterstitialAd(() => { /* no-op: UI continue panel handles flow */ });
            }
        }

        // =====================================================================
        // Victory → soft remove-ads prompt after 2s
        // =====================================================================
        private void OnVictory()
        {
            if (PlayerPrefs.GetInt(IAPManager.RemoveAdsKey, 0) == 1) return;
            // UIManager.ShowVictory already schedules MaybeShowRemoveAdsAfter(2f),
            // so this hook is intentionally minimal — it exists for future
            // experimentation (analytics, alternate prompts).
        }
    }
}
