using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Monetization;

namespace TowerGuard.UI
{
    /// <summary>
    /// Runtime controller for the Shop overlay. Owns two tabs (Gems / Offers),
    /// pulls localized prices from IAPManager when available, and routes BUY
    /// presses to IAPManager. Fully data-bound — the editor automation script
    /// (Phase4Setup) builds the visual hierarchy and wires the references.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private Button closeButton;

        [Header("Tabs")]
        [SerializeField] private Button gemsTabButton;
        [SerializeField] private Button offersTabButton;
        [SerializeField] private GameObject gemsTab;
        [SerializeField] private GameObject offersTab;

        [Header("Gem cards")]
        [SerializeField] private TMP_Text gemSmallPriceText;
        [SerializeField] private Button gemSmallBuyButton;
        [SerializeField] private TMP_Text gemMediumPriceText;
        [SerializeField] private Button gemMediumBuyButton;
        [SerializeField] private TMP_Text gemLargePriceText;
        [SerializeField] private Button gemLargeBuyButton;

        [Header("Offer cards")]
        [SerializeField] private TMP_Text starterPackPriceText;
        [SerializeField] private Button starterPackBuyButton;
        [SerializeField] private TMP_Text removeAdsPriceText;
        [SerializeField] private Button removeAdsBuyButton;

        [Header("Footer")]
        [SerializeField] private Button restoreButton;

        [Header("Anim")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float slideDuration = 0.25f;

        private bool gemsTabActive = true;

        private void Awake()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            // Wire buttons (idempotent across re-shows).
            if (closeButton != null)         { closeButton.onClick.RemoveAllListeners();         closeButton.onClick.AddListener(Hide); }
            if (gemsTabButton != null)       { gemsTabButton.onClick.RemoveAllListeners();       gemsTabButton.onClick.AddListener(() => SwitchTab(true)); }
            if (offersTabButton != null)     { offersTabButton.onClick.RemoveAllListeners();     offersTabButton.onClick.AddListener(() => SwitchTab(false)); }
            if (gemSmallBuyButton != null)   { gemSmallBuyButton.onClick.RemoveAllListeners();   gemSmallBuyButton.onClick.AddListener(() => Buy(IAPManager.GEMS_SMALL)); }
            if (gemMediumBuyButton != null)  { gemMediumBuyButton.onClick.RemoveAllListeners();  gemMediumBuyButton.onClick.AddListener(() => Buy(IAPManager.GEMS_MEDIUM)); }
            if (gemLargeBuyButton != null)   { gemLargeBuyButton.onClick.RemoveAllListeners();   gemLargeBuyButton.onClick.AddListener(() => Buy(IAPManager.GEMS_LARGE)); }
            if (starterPackBuyButton != null){ starterPackBuyButton.onClick.RemoveAllListeners();starterPackBuyButton.onClick.AddListener(BuyStarterPack); }
            if (removeAdsBuyButton != null)  { removeAdsBuyButton.onClick.RemoveAllListeners();  removeAdsBuyButton.onClick.AddListener(BuyRemoveAds); }
            if (restoreButton != null)       { restoreButton.onClick.RemoveAllListeners();       restoreButton.onClick.AddListener(OnRestorePressed); }

            GameManager.OnHardCurrencyChanged += UpdateBalance;
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnStoreInitialized += RefreshPrices;
                IAPManager.Instance.OnPurchaseSuccess  += OnPurchaseSuccess;
            }

            UpdateBalance(GameManager.Instance != null ? GameManager.Instance.HardCurrency : 0);
            RefreshPrices();
            SwitchTab(true);
        }

        private void OnDisable()
        {
            GameManager.OnHardCurrencyChanged -= UpdateBalance;
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnStoreInitialized -= RefreshPrices;
                IAPManager.Instance.OnPurchaseSuccess  -= OnPurchaseSuccess;
            }
        }

        // =====================================================================
        // Show / Hide
        // =====================================================================
        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (rootRect != null)
            {
                rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, -1000f);
                LeanTween.value(gameObject, v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v), -1000f, 0f, slideDuration)
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
                LeanTween.value(gameObject, v => rootRect.anchoredPosition = new Vector2(rootRect.anchoredPosition.x, v), rootRect.anchoredPosition.y, -1000f, slideDuration)
                    .setEaseInQuart()
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // Tabs
        // =====================================================================
        private void SwitchTab(bool gems)
        {
            gemsTabActive = gems;
            if (gemsTab != null)   gemsTab.SetActive(gems);
            if (offersTab != null) offersTab.SetActive(!gems);
        }

        // =====================================================================
        // Balance / Prices
        // =====================================================================
        private void UpdateBalance(int hard)
        {
            if (balanceText != null) balanceText.text = hard.ToString();
        }

        private void RefreshPrices()
        {
            string loading = "Loading...";
            bool hasIAP = IAPManager.Instance != null && IAPManager.Instance.IsInitialized;

            if (gemSmallPriceText != null)
                gemSmallPriceText.text = hasIAP ? IAPManager.Instance.GetLocalizedPrice(IAPManager.GEMS_SMALL) : loading;
            if (gemMediumPriceText != null)
                gemMediumPriceText.text = hasIAP ? IAPManager.Instance.GetLocalizedPrice(IAPManager.GEMS_MEDIUM) : loading;
            if (gemLargePriceText != null)
                gemLargePriceText.text = hasIAP ? IAPManager.Instance.GetLocalizedPrice(IAPManager.GEMS_LARGE) : loading;

            // Starter pack — show OWNED if already purchased.
            if (starterPackPriceText != null)
            {
                if (hasIAP && IAPManager.Instance.HasPurchased(IAPManager.STARTER_PACK))
                {
                    starterPackPriceText.text = "OWNED";
                    if (starterPackBuyButton != null) starterPackBuyButton.interactable = false;
                }
                else
                {
                    starterPackPriceText.text = hasIAP ? IAPManager.Instance.GetLocalizedPrice(IAPManager.STARTER_PACK) : loading;
                    if (starterPackBuyButton != null) starterPackBuyButton.interactable = hasIAP;
                }
            }

            if (removeAdsPriceText != null)
            {
                if (hasIAP && IAPManager.Instance.HasPurchased(IAPManager.REMOVE_ADS))
                {
                    removeAdsPriceText.text = "OWNED";
                    if (removeAdsBuyButton != null) removeAdsBuyButton.interactable = false;
                }
                else
                {
                    removeAdsPriceText.text = hasIAP ? IAPManager.Instance.GetLocalizedPrice(IAPManager.REMOVE_ADS) : loading;
                    if (removeAdsBuyButton != null) removeAdsBuyButton.interactable = hasIAP;
                }
            }
        }

        private void OnPurchaseSuccess(string productId)
        {
            // Refresh "OWNED" badges if the just-bought SKU was one of the
            // non-consumables.
            RefreshPrices();
        }

        // =====================================================================
        // Buy actions
        // =====================================================================
        private void Buy(string productId)
        {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.PurchaseGems(productId);
        }

        private void BuyStarterPack()
        {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.PurchaseStarterPack();
        }

        private void BuyRemoveAds()
        {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.PurchaseRemoveAds();
        }

        private void OnRestorePressed()
        {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.RestorePurchases();
        }
    }
}
