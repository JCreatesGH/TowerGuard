using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace TowerGuard.Monetization
{
    /// <summary>
    /// Thin wrapper around Unity's legacy custom-analytics API. Each Track*
    /// helper builds a Dictionary<string,object> and forwards it to
    /// Analytics.CustomEvent. Failure is swallowed and logged so a missing
    /// project link or analytics opt-out never breaks gameplay.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        // Public tracking helpers
        // =====================================================================

        public void TrackWaveStarted(int wave)
            => Send("wave_started", new Dictionary<string, object> { { "wave", wave } });

        public void TrackWaveCompleted(int wave, int hpRemaining, int goldEarned)
            => Send("wave_completed", new Dictionary<string, object>
            {
                { "wave", wave },
                { "hp_remaining", hpRemaining },
                { "gold_earned", goldEarned }
            });

        public void TrackGameOver(int waveReached)
            => Send("game_over", new Dictionary<string, object> { { "wave_reached", waveReached } });

        public void TrackTowerPlaced(string towerType, int cost)
            => Send("tower_placed", new Dictionary<string, object>
            {
                { "tower_type", towerType ?? "unknown" },
                { "cost", cost }
            });

        public void TrackAdShown(string adType)
            => Send("ad_shown", new Dictionary<string, object> { { "ad_type", adType ?? "unknown" } });

        public void TrackIAPPurchase(string productId)
            => Send("iap_purchase", new Dictionary<string, object> { { "product_id", productId ?? "unknown" } });

        public void TrackRewardedAdAccepted(string entryPoint)
            => Send("rewarded_ad_accepted", new Dictionary<string, object>
            {
                { "entry_point", entryPoint ?? "unknown" }
            });

        // =====================================================================
        // Internals
        // =====================================================================

        private void Send(string name, IDictionary<string, object> payload)
        {
            try
            {
#pragma warning disable CS0618 // Analytics.CustomEvent is the legacy API but still ships.
                Analytics.CustomEvent(name, payload);
#pragma warning restore CS0618
            }
            catch (System.Exception e)
            {
                Debug.Log($"[AnalyticsManager] Send '{name}' failed: {e.Message}");
            }
        }
    }
}
