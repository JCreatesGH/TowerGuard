/*
 * MONETIZATION PHILOSOPHY — TowerGuard
 * Players can complete all 10 waves without spending money or watching a single ad.
 * Ads are never forced mid-gameplay. Rewarded ads are always opt-in.
 * IAP offers convenience, not progression gates.
 */

using System;
using UnityEngine;
using UnityEngine.Advertisements;

namespace TowerGuard.Monetization
{
    /// <summary>
    /// Real Unity Ads integration. Manages rewarded, interstitial and banner ads
    /// with the philosophy that ads are always opt-in for rewards and never block
    /// gameplay. Falls back to an editor stub on non-iOS platforms so the rest of
    /// the codebase can keep calling AdManager.Instance.* uniformly.
    /// </summary>
    public class AdManager : MonoBehaviour,
        IUnityAdsInitializationListener,
        IUnityAdsLoadListener,
        IUnityAdsShowListener
    {
        public static AdManager Instance { get; private set; }

        // ----- Constants (replace with real IDs before App Store submission) -----
        private const string GAME_ID_IOS     = "placeholder_ios_id";
        private const string AD_REWARDED     = "Rewarded_iOS";
        private const string AD_INTERSTITIAL = "Interstitial_iOS";
        private const string AD_BANNER       = "Banner_iOS";
        private const bool   TEST_MODE       = true; // Set false before App Store submission.

        private const string NoAdsKey            = "no_ads";
        private const float  InterstitialCooldown = 180f; // 3 minutes between interstitials.

        // ----- State -----
        public bool NoAds { get; private set; }
        private bool initialized;
        private bool rewardedLoaded;
        private bool interstitialLoaded;
        private float lastInterstitialTime = -180f;

        private Action<bool> pendingRewardedCallback;
        private Action       pendingInterstitialCallback;

        // ----- Unity lifecycle -----
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            NoAds = PlayerPrefs.GetInt(NoAdsKey, 0) == 1;
            lastInterstitialTime = -InterstitialCooldown; // allow immediate first show

#if UNITY_IOS || UNITY_EDITOR_OSX
            // On real iOS device or while editing on macOS targeting iOS we initialize
            // the Unity Ads SDK. Test mode lets the SDK serve test ads without billing
            // a real placement.
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXEditor)
            {
                if (Advertisement.isSupported)
                {
                    Advertisement.Initialize(GAME_ID_IOS, TEST_MODE, this);
                }
                else
                {
                    Debug.Log("[AdManager] Advertisement.isSupported == false on this platform — skipping init.");
                    initialized = true;
                }
            }
            else
            {
                initialized = true;
            }
#else
            initialized = true;
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        // Public API
        // =====================================================================

        public void LoadRewardedAd()
        {
            if (!initialized) return;
            try { Advertisement.Load(AD_REWARDED, this); }
            catch (Exception e) { Debug.LogWarning($"[AdManager] LoadRewardedAd error: {e.Message}"); }
        }

        public void ShowRewardedAd(Action<bool> onComplete)
        {
            // Fall back path: if SDK isn't initialized or the ad isn't loaded we
            // immediately tell the caller "false" so the UI can dismiss its prompt
            // gracefully instead of blocking the player.
            if (!initialized || !rewardedLoaded)
            {
                Debug.Log($"[AdManager] Rewarded ad not ready (init={initialized}, loaded={rewardedLoaded}). Returning false.");
                onComplete?.Invoke(false);
                // Try to load for next time.
                if (initialized) LoadRewardedAd();
                return;
            }

            pendingRewardedCallback = onComplete;
            try
            {
                Advertisement.Show(AD_REWARDED, this);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdManager] Show rewarded error: {e.Message}");
                pendingRewardedCallback = null;
                onComplete?.Invoke(false);
            }
        }

        public void LoadInterstitialAd()
        {
            if (!initialized) return;
            try { Advertisement.Load(AD_INTERSTITIAL, this); }
            catch (Exception e) { Debug.LogWarning($"[AdManager] LoadInterstitialAd error: {e.Message}"); }
        }

        public void ShowInterstitialAd(Action onComplete)
        {
            // 1) NoAds bypasses all interstitials forever.
            if (NoAds)
            {
                onComplete?.Invoke();
                return;
            }
            // 2) Throttle: never show more than once every 3 minutes.
            if (Time.realtimeSinceStartup - lastInterstitialTime < InterstitialCooldown)
            {
                onComplete?.Invoke();
                return;
            }
            // 3) Fall back if not loaded.
            if (!initialized || !interstitialLoaded)
            {
                onComplete?.Invoke();
                if (initialized) LoadInterstitialAd();
                return;
            }

            pendingInterstitialCallback = onComplete;
            try
            {
                Advertisement.Show(AD_INTERSTITIAL, this);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdManager] Show interstitial error: {e.Message}");
                pendingInterstitialCallback = null;
                onComplete?.Invoke();
            }
        }

        public void ShowBannerAd()
        {
            if (NoAds || !initialized) return;
            try
            {
                Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
                Advertisement.Banner.Show(AD_BANNER);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdManager] ShowBannerAd error: {e.Message}");
            }
        }

        public void HideBannerAd()
        {
            try { Advertisement.Banner.Hide(); }
            catch (Exception e) { Debug.LogWarning($"[AdManager] HideBannerAd error: {e.Message}"); }
        }

        /// <summary>Mark the player as having purchased the Remove Ads SKU.</summary>
        public void RemoveAds()
        {
            NoAds = true;
            PlayerPrefs.SetInt(NoAdsKey, 1);
            PlayerPrefs.Save();
            HideBannerAd();
        }

        /// <summary>Convenience for code that just wants to know whether to suppress ads.</summary>
        public bool AreAdsRemoved() => NoAds;

        // =====================================================================
        // IUnityAdsInitializationListener
        // =====================================================================
        public void OnInitializationComplete()
        {
            Debug.Log("[AdManager] Unity Ads initialized.");
            initialized = true;
            LoadRewardedAd();
            LoadInterstitialAd();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogWarning($"[AdManager] Unity Ads init failed: {error} — {message}");
            // Still mark initialized so callers fall through cleanly instead of blocking.
            initialized = true;
        }

        // =====================================================================
        // IUnityAdsLoadListener
        // =====================================================================
        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId == AD_REWARDED)     rewardedLoaded     = true;
            if (placementId == AD_INTERSTITIAL) interstitialLoaded = true;
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            if (placementId == AD_REWARDED)     rewardedLoaded     = false;
            if (placementId == AD_INTERSTITIAL) interstitialLoaded = false;
            Debug.LogWarning($"[AdManager] Failed to load {placementId}: {error} — {message}");
        }

        // =====================================================================
        // IUnityAdsShowListener
        // =====================================================================
        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning($"[AdManager] Show failure {placementId}: {error} — {message}");
            if (placementId == AD_REWARDED)
            {
                var cb = pendingRewardedCallback; pendingRewardedCallback = null;
                cb?.Invoke(false);
                rewardedLoaded = false;
                LoadRewardedAd();
            }
            else if (placementId == AD_INTERSTITIAL)
            {
                var cb = pendingInterstitialCallback; pendingInterstitialCallback = null;
                cb?.Invoke();
                interstitialLoaded = false;
                LoadInterstitialAd();
            }
        }

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if (placementId == AD_REWARDED)
            {
                bool rewarded = showCompletionState == UnityAdsShowCompletionState.COMPLETED;
                var cb = pendingRewardedCallback; pendingRewardedCallback = null;
                cb?.Invoke(rewarded);
                rewardedLoaded = false;
                LoadRewardedAd();
            }
            else if (placementId == AD_INTERSTITIAL)
            {
                lastInterstitialTime = Time.realtimeSinceStartup;
                var cb = pendingInterstitialCallback; pendingInterstitialCallback = null;
                cb?.Invoke();
                interstitialLoaded = false;
                LoadInterstitialAd();
            }
        }
    }
}
