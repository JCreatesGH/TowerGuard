using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;

namespace TowerGuard.Monetization
{
    /// <summary>
    /// Drives the "Daily Free Gem" panel on MainMenu. On scene start, checks
    /// whether the player has already claimed today's gem (via the
    /// `last_free_gem_date` PlayerPref) — if not, the panel is shown.
    ///
    /// The Watch Ad button calls AdManager.ShowRewardedAd; on success the gem
    /// is granted and today's date is persisted. The "No Thanks" button hides
    /// the panel without writing the date so the offer reappears tomorrow.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        public const string LastClaimKey = "last_free_gem_date";

        [SerializeField] private GameObject panel;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private TMP_Text bodyText;

        private void Start()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastClaim = PlayerPrefs.GetString(LastClaimKey, "");
            if (panel != null) panel.SetActive(false);

            if (today != lastClaim)
            {
                ShowPanel();
            }
        }

        private void OnEnable()
        {
            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);
            if (declineButton != null) declineButton.onClick.AddListener(OnDecline);
        }

        private void OnDisable()
        {
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
            if (declineButton != null) declineButton.onClick.RemoveListener(OnDecline);
        }

        private void ShowPanel()
        {
            if (panel == null) return;
            panel.SetActive(true);
            if (bodyText != null) bodyText.text = "Daily Free Gem!\nWatch a short ad to claim.";
        }

        private void OnWatchAd()
        {
            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackRewardedAdAccepted("daily_free_gem");
            }
            if (AdManager.Instance == null)
            {
                HidePanel();
                return;
            }
            AdManager.Instance.ShowRewardedAd(rewarded =>
            {
                if (rewarded)
                {
                    if (GameManager.Instance != null) GameManager.Instance.EarnHardCurrency(1);
                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    PlayerPrefs.SetString(LastClaimKey, today);
                    PlayerPrefs.Save();
                    ShowGemToast();
                }
                HidePanel();
            });
        }

        private void OnDecline() => HidePanel();

        private void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>"+1 Gem!" toast — surfaced via UIManager if it exists, else logged.</summary>
        private static void ShowGemToast()
        {
            var ui = TowerGuard.UI.UIManager.Instance;
            if (ui != null) ui.ShowToast("+1 Gem!", 1.5f);
            else            Debug.Log("[DailyRewardManager] +1 Gem!");
        }
    }
}
