/*
 * MONETIZATION PHILOSOPHY — TowerGuard
 * IAP offers convenience and a better experience. Nothing is paywalled.
 */

using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using TowerGuard.Core;

namespace TowerGuard.Monetization
{
    /// <summary>
    /// Real Unity IAP integration. Defines five SKUs:
    ///   - remove_ads          NonConsumable $1.99
    ///   - starter_pack        NonConsumable $2.99
    ///   - gem_pack_small      Consumable    $0.99
    ///   - gem_pack_medium     Consumable    $2.99
    ///   - gem_pack_large      Consumable    $4.99
    /// All purchases are silent on success. Failed purchases surface a transient
    /// in-game toast via UIManager.ShowToast (added in Phase 4).
    /// </summary>
    public class IAPManager : MonoBehaviour, IStoreListener
    {
        public static IAPManager Instance { get; private set; }

        // ----- Product IDs -----
        public const string REMOVE_ADS    = "remove_ads";
        public const string STARTER_PACK  = "starter_pack";
        public const string GEMS_SMALL    = "gem_pack_small";
        public const string GEMS_MEDIUM   = "gem_pack_medium";
        public const string GEMS_LARGE    = "gem_pack_large";

        // PlayerPrefs keys for non-consumables.
        public const string RemoveAdsKey   = "no_ads";
        public const string StarterPackKey = "starter_pack_owned";

        // ----- State -----
        public bool IsInitialized { get; private set; }
        private IStoreController storeController;
        private IExtensionProvider extensions;

        public event Action OnStoreInitialized;
        public event Action<string> OnPurchaseSuccess;
        public event Action<string, string> OnPurchaseFailedEvent; // (productId, reason)

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

        private void Start()
        {
            InitializePurchasing();
        }

        // =====================================================================
        // Initialization
        // =====================================================================
        private void InitializePurchasing()
        {
            if (IsInitialized) return;

            try
            {
                var module = StandardPurchasingModule.Instance();
                var builder = ConfigurationBuilder.Instance(module);
                builder.AddProduct(REMOVE_ADS,   ProductType.NonConsumable);
                builder.AddProduct(STARTER_PACK, ProductType.NonConsumable);
                builder.AddProduct(GEMS_SMALL,   ProductType.Consumable);
                builder.AddProduct(GEMS_MEDIUM,  ProductType.Consumable);
                builder.AddProduct(GEMS_LARGE,   ProductType.Consumable);
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAPManager] InitializePurchasing exception: {e.Message}");
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider ext)
        {
            storeController = controller;
            extensions = ext;
            IsInitialized = true;
            Debug.Log("[IAPManager] Initialized — products available.");
            OnStoreInitialized?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning($"[IAPManager] OnInitializeFailed: {error}");
            IsInitialized = false;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning($"[IAPManager] OnInitializeFailed: {error} — {message}");
            IsInitialized = false;
        }

        // =====================================================================
        // Public purchase API
        // =====================================================================
        public void PurchaseRemoveAds()    => BuyProductID(REMOVE_ADS);
        public void PurchaseStarterPack()  => BuyProductID(STARTER_PACK);
        public void PurchaseGems(string id) => BuyProductID(id);

        /// <summary>iOS-only: triggers Apple's restore-purchases sheet.</summary>
        public void RestorePurchases()
        {
            if (!IsInitialized)
            {
                ShowToast("Store not ready yet — please try again.");
                return;
            }
            try
            {
                if (extensions != null)
                {
                    var apple = extensions.GetExtension<IAppleExtensions>();
                    if (apple != null)
                    {
                        apple.RestoreTransactions(result =>
                        {
                            Debug.Log($"[IAPManager] RestoreTransactions result={result}");
                            if (result) ShowToast("Purchases restored.");
                            else        ShowToast("Nothing to restore.");
                        });
                        return;
                    }
                }
                ShowToast("Restore not available on this platform.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAPManager] RestorePurchases exception: {e.Message}");
                ShowToast("Restore failed.");
            }
        }

        // =====================================================================
        // Inspect product metadata (used by ShopScreen for prices)
        // =====================================================================
        public Product GetProduct(string id)
        {
            if (!IsInitialized || storeController == null) return null;
            return storeController.products.WithID(id);
        }

        public string GetLocalizedPrice(string id)
        {
            var p = GetProduct(id);
            if (p == null || p.metadata == null) return "Loading...";
            return p.metadata.localizedPriceString;
        }

        public bool HasPurchased(string id)
        {
            if (id == REMOVE_ADS)   return PlayerPrefs.GetInt(RemoveAdsKey, 0) == 1;
            if (id == STARTER_PACK) return PlayerPrefs.GetInt(StarterPackKey, 0) == 1;
            return false;
        }

        public bool AreAdsRemoved() => PlayerPrefs.GetInt(RemoveAdsKey, 0) == 1;

        // =====================================================================
        // Internals
        // =====================================================================
        private void BuyProductID(string id)
        {
            if (!IsInitialized || storeController == null)
            {
                Debug.LogWarning("[IAPManager] BuyProductID called before init.");
                ShowToast("Store not ready yet.");
                return;
            }
            Product product = storeController.products.WithID(id);
            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogWarning($"[IAPManager] Product unavailable: {id}");
                ShowToast("Purchase unavailable");
            }
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string id = args.purchasedProduct.definition.id;
            switch (id)
            {
                case REMOVE_ADS:
                    if (AdManager.Instance != null) AdManager.Instance.RemoveAds();
                    PlayerPrefs.SetInt(RemoveAdsKey, 1);
                    PlayerPrefs.Save();
                    ShowToast("Ads removed!");
                    break;
                case STARTER_PACK:
                    if (AdManager.Instance != null) AdManager.Instance.RemoveAds();
                    PlayerPrefs.SetInt(RemoveAdsKey, 1);
                    PlayerPrefs.SetInt(StarterPackKey, 1);
                    PlayerPrefs.Save();
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.EarnSoftCurrency(200);
                        GameManager.Instance.EarnHardCurrency(20);
                    }
                    ShowToast("Starter Pack unlocked!");
                    break;
                case GEMS_SMALL:
                    if (GameManager.Instance != null) GameManager.Instance.EarnHardCurrency(15);
                    ShowToast("+15 Gems!");
                    break;
                case GEMS_MEDIUM:
                    if (GameManager.Instance != null) GameManager.Instance.EarnHardCurrency(50);
                    ShowToast("+50 Gems!");
                    break;
                case GEMS_LARGE:
                    if (GameManager.Instance != null) GameManager.Instance.EarnHardCurrency(120);
                    ShowToast("+120 Gems!");
                    break;
                default:
                    Debug.LogWarning($"[IAPManager] Unknown productId: {id}");
                    break;
            }

            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackIAPPurchase(id);
            }

            OnPurchaseSuccess?.Invoke(id);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string id = product != null ? product.definition.id : "unknown";
            Debug.LogWarning($"[IAPManager] Purchase failed {id}: {failureReason}");
            ShowToast("Purchase failed: " + failureReason);
            OnPurchaseFailedEvent?.Invoke(id, failureReason.ToString());
        }

        // =====================================================================
        // Toast helper — finds whichever UIManager is around.
        // =====================================================================
        private static void ShowToast(string msg)
        {
            var ui = TowerGuard.UI.UIManager.Instance;
            if (ui != null) ui.ShowToast(msg, 2f);
            else            Debug.Log($"[IAPManager] (toast) {msg}");
        }
    }
}
