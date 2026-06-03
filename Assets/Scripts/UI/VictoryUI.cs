using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Monetization;

namespace TowerGuard.UI
{
    /// <summary>
    /// Wires the Victory panel's two always-present buttons (Main Menu / Next Level)
    /// and the "Coming Soon!" toast for the stubbed Next Level. Star animation and
    /// stats text are populated by UIManager.ShowVictory.
    /// </summary>
    public class VictoryUI : MonoBehaviour
    {
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private GameObject comingSoonToast;
        [SerializeField] private Button removeAdsPurchaseButton;
        [SerializeField] private Button removeAdsDeclineButton;
        [SerializeField] private GameObject removeAdsPrompt;

        private void OnEnable()
        {
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevel);
            if (removeAdsPurchaseButton != null) removeAdsPurchaseButton.onClick.AddListener(OnRemoveAdsPurchase);
            if (removeAdsDeclineButton != null) removeAdsDeclineButton.onClick.AddListener(OnRemoveAdsDecline);
            if (comingSoonToast != null) comingSoonToast.SetActive(false);
        }

        private void OnDisable()
        {
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(OnNextLevel);
            if (removeAdsPurchaseButton != null) removeAdsPurchaseButton.onClick.RemoveListener(OnRemoveAdsPurchase);
            if (removeAdsDeclineButton != null) removeAdsDeclineButton.onClick.RemoveListener(OnRemoveAdsDecline);
        }

        private void OnMainMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
        }

        private void OnNextLevel()
        {
            if (comingSoonToast != null) StartCoroutine(ShowComingSoonToast());
        }

        private IEnumerator ShowComingSoonToast()
        {
            comingSoonToast.SetActive(true);
            CanvasGroup cg = comingSoonToast.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                LeanTween.alphaCanvas(cg, 1f, 0.2f).setIgnoreTimeScale(true);
            }
            float t = 0f;
            while (t < 1.5f) { t += Time.unscaledDeltaTime; yield return null; }
            if (cg != null)
            {
                LeanTween.alphaCanvas(cg, 0f, 0.2f).setIgnoreTimeScale(true);
            }
            float f = 0f;
            while (f < 0.2f) { f += Time.unscaledDeltaTime; yield return null; }
            comingSoonToast.SetActive(false);
        }

        private void OnRemoveAdsPurchase()
        {
            if (IAPManager.Instance != null) IAPManager.Instance.PurchaseRemoveAds();
            if (removeAdsPrompt != null) removeAdsPrompt.SetActive(false);
        }

        private void OnRemoveAdsDecline()
        {
            if (removeAdsPrompt != null) removeAdsPrompt.SetActive(false);
        }
    }
}
