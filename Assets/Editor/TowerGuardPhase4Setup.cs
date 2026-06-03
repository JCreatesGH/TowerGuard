#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Monetization;
using TowerGuard.UI;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 4 monetization scene patches. Idempotent: re-running clears the
    /// "Phase4Root" objects on each scene before rebuilding.
    ///
    /// MainMenu adds:
    ///   - MonetizationRoot (AdManager, IAPManager, AnalyticsManager singletons)
    ///   - Daily Free Gem panel + DailyRewardManager
    ///   - Shop button on the bottom bar + Shop overlay
    /// Level_01 adds:
    ///   - MonetizationHooks component on the GameManager GameObject
    ///   - Gem-icon Shop button in the TopBar + Shop overlay
    ///   - DoubleRewardPanel (rewarded ad #2)
    ///   - GenericToast (used by IAPManager / DailyRewardManager / Continue flow)
    /// </summary>
    public static class TowerGuardPhase4Setup
    {
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string Level01ScenePath  = "Assets/Scenes/Level_01.unity";

        // Match Phase 3 palette so the new UI is visually consistent.
        static readonly Color ColBg       = new Color32(0x1A, 0x1A, 0x2E, 0xFF);
        static readonly Color ColPanel    = new Color32(0x16, 0x21, 0x3E, 0xFF);
        static readonly Color ColAccent   = new Color32(0x0F, 0x34, 0x60, 0xFF);
        static readonly Color ColCta      = new Color32(0xE9, 0x45, 0x60, 0xFF);
        static readonly Color ColGold     = new Color32(0xF5, 0xA6, 0x23, 0xFF);
        static readonly Color ColText     = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        static readonly Color ColTextDim  = new Color32(0xBB, 0xBB, 0xBB, 0xFF);
        static readonly Color ColDim70    = new Color(0f, 0f, 0f, 0.7f);

        // ============================================================================
        // Menu entries
        // ============================================================================
        [MenuItem("Tools/TowerGuard/Run All Phase 4 Setup", priority = 300)]
        public static void RunAll()
        {
            BuildMainMenuPhase4();
            BuildLevel01Phase4();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase4Setup] RunAll: complete.");
        }

        [MenuItem("Tools/TowerGuard/Phase 4 - 01 Patch MainMenu", priority = 310)]
        public static void Step01() { BuildMainMenuPhase4(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/Phase 4 - 02 Patch Level_01", priority = 311)]
        public static void Step02() { BuildLevel01Phase4(); AssetDatabase.SaveAssets(); }

        // ============================================================================
        // MainMenu patches
        // ============================================================================
        static void BuildMainMenuPhase4()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            ClearPhase4Root(scene);

            // ---- MonetizationRoot — singletons live here, DontDestroyOnLoad takes them across scenes ----
            var monRoot = new GameObject("Phase4_MonetizationRoot");
            monRoot.AddComponent<AdManager>();
            monRoot.AddComponent<IAPManager>();
            monRoot.AddComponent<AnalyticsManager>();
            EditorSceneManager.MoveGameObjectToScene(monRoot, scene);

            // ---- DailyRewardManager + panel ----
            var canvas = FindCanvas(scene);
            if (canvas == null)
            {
                Debug.LogWarning("[Phase4Setup] MainMenu has no Canvas — skipping UI panels.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }

            var canvasT = canvas.transform;

            // Daily Free Gem panel — small modal at top center.
            var freeGemPanel = NewUIObj("Phase4_FreeGemPanel", canvasT);
            FullStretch(AsRT(freeGemPanel));
            AddImage(freeGemPanel, ColDim70);

            var freeGemInner = NewUIObj("Panel", freeGemPanel.transform);
            AnchorCenter(AsRT(freeGemInner), new Vector2(320, 280));
            AddImage(freeGemInner, ColPanel);

            var (titleGO, _) = NewText(freeGemInner.transform, "Title", "Daily Free Gem!", 22, ColGold, true);
            AnchorTopCenter(AsRT(titleGO), new Vector2(280, 40), new Vector2(0, -20));

            var (bodyGO, bodyTMP) = NewText(freeGemInner.transform, "Body", "Watch a short ad to claim 1 gem.", 16, ColTextDim);
            AnchorTopCenter(AsRT(bodyGO), new Vector2(280, 50), new Vector2(0, -75));

            var (watchGO, watchBtn, _) = NewButton(freeGemInner.transform, "WatchAdButton", "Watch Ad", ColCta, ColText, 18);
            AnchorBottomCenter(AsRT(watchGO), new Vector2(220, 50), new Vector2(0, 90));

            var (declineGO, declineBtn, _) = NewButton(freeGemInner.transform, "NoThanksButton", "No Thanks", ColAccent, ColText, 16);
            AnchorBottomCenter(AsRT(declineGO), new Vector2(220, 44), new Vector2(0, 30));

            freeGemPanel.SetActive(false);

            // Attach DailyRewardManager and wire references.
            var drmGO = new GameObject("Phase4_DailyRewardManager");
            drmGO.transform.SetParent(canvasT.parent, true);
            EditorSceneManager.MoveGameObjectToScene(drmGO, scene);
            var drm = drmGO.AddComponent<DailyRewardManager>();
            SetField(drm, "panel", freeGemPanel);
            SetField(drm, "watchAdButton", watchBtn);
            SetField(drm, "declineButton", declineBtn);
            SetField(drm, "bodyText", bodyTMP);

            // ---- Shop overlay + Shop button ----
            var shopOverlay = BuildShopOverlay(canvasT);
            shopOverlay.SetActive(false);

            // Shop button on MainMenu — append below CREDITS.
            var (shopBtnGO, shopBtn, _) = NewButton(canvasT.Find("SafeAreaRoot") ?? canvasT, "Phase4_ShopButton", "SHOP", ColGold, Color.black, 24);
            AnchorCenter(AsRT(shopBtnGO), new Vector2(260, 56));
            AsRT(shopBtnGO).anchoredPosition = new Vector2(0, -280);

            shopBtn.onClick.AddListener(() =>
            {
                var sc = shopOverlay.GetComponent<ShopScreen>();
                if (sc != null) sc.Show();
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase4Setup] MainMenu patched.");
        }

        // ============================================================================
        // Level_01 patches
        // ============================================================================
        static void BuildLevel01Phase4()
        {
            var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            ClearPhase4Root(scene);

            // 1) MonetizationHooks on GameManager GameObject (or root with GameManager component).
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                if (gm.GetComponent<MonetizationHooks>() == null)
                {
                    gm.gameObject.AddComponent<MonetizationHooks>();
                }
            }
            else
            {
                Debug.LogWarning("[Phase4Setup] Level_01 has no GameManager — MonetizationHooks NOT attached.");
            }

            var canvas = FindCanvas(scene);
            if (canvas == null)
            {
                Debug.LogWarning("[Phase4Setup] Level_01 has no Canvas — skipping UI patches.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }

            var canvasT = canvas.transform;
            var ui = Object.FindFirstObjectByType<UIManager>();

            // 2) Generic toast at top-center.
            var toastGO = NewUIObj("Phase4_GenericToast", canvasT);
            AnchorTopCenter(AsRT(toastGO), new Vector2(320, 50), new Vector2(0, -90));
            AddImage(toastGO, new Color(0.05f, 0.05f, 0.1f, 0.9f));
            var toastCG = toastGO.AddComponent<CanvasGroup>();
            var (toastTextGO, toastTMP) = NewText(toastGO.transform, "Text", "", 18, ColText, true);
            FullStretch(AsRT(toastTextGO));
            toastGO.SetActive(false);
            if (ui != null)
            {
                SetField(ui, "genericToast", toastGO);
                SetField(ui, "genericToastText", toastTMP);
                SetField(ui, "genericToastCanvasGroup", toastCG);
            }

            // 3) DoubleRewardPanel — small modal in the middle.
            var drPanel = NewUIObj("Phase4_DoubleRewardPanel", canvasT);
            FullStretch(AsRT(drPanel));
            AddImage(drPanel, ColDim70);
            var drInner = NewUIObj("Panel", drPanel.transform);
            AnchorCenter(AsRT(drInner), new Vector2(320, 240));
            AddImage(drInner, ColPanel);

            var (drTitleGO, _) = NewText(drInner.transform, "Title", "Double your wave bonus?", 20, ColGold, true);
            AnchorTopCenter(AsRT(drTitleGO), new Vector2(290, 40), new Vector2(0, -20));

            var (drCountGO, drCountTMP) = NewText(drInner.transform, "Countdown", "10s", 14, ColTextDim);
            AnchorTopCenter(AsRT(drCountGO), new Vector2(120, 24), new Vector2(0, -65));

            var (drWatchGO, drWatchBtn, _) = NewButton(drInner.transform, "WatchAdButton", "Watch Ad", ColCta, ColText, 18);
            AnchorBottomCenter(AsRT(drWatchGO), new Vector2(220, 50), new Vector2(0, 90));

            var (drDeclineGO, drDeclineBtn, _) = NewButton(drInner.transform, "NoThanksButton", "No Thanks", ColAccent, ColText, 16);
            AnchorBottomCenter(AsRT(drDeclineGO), new Vector2(220, 44), new Vector2(0, 30));

            drPanel.SetActive(false);
            if (ui != null)
            {
                SetField(ui, "doubleRewardPanel", drPanel);
                SetField(ui, "doubleRewardCountdownText", drCountTMP);
                SetField(ui, "doubleRewardWatchButton", drWatchBtn);
                SetField(ui, "doubleRewardDeclineButton", drDeclineBtn);
            }

            // 4) Shop overlay accessible from gameplay.
            var shopOverlay = BuildShopOverlay(canvasT);
            shopOverlay.SetActive(false);

            // 5) Gem icon Shop button in TopBar (top-right).
            var topBar = canvasT.Find("SafeAreaRoot/TopBar") ?? canvasT;
            // Avoid emoji glyphs that aren't in the default TMP font — use "$" as a
            // simple universally-supported placeholder. Phase 5 can swap in a real icon.
            var (gemBtnGO, gemBtn, gemLabel) = NewButton(topBar, "Phase4_GemShopButton", "$", ColGold, Color.black, 22);
            AsRT(gemBtnGO).anchorMin = AsRT(gemBtnGO).anchorMax = new Vector2(1f, 0.5f);
            AsRT(gemBtnGO).pivot = new Vector2(1f, 0.5f);
            AsRT(gemBtnGO).sizeDelta = new Vector2(48, 48);
            AsRT(gemBtnGO).anchoredPosition = new Vector2(-12, 0);
            gemBtn.onClick.AddListener(() =>
            {
                var sc = shopOverlay.GetComponent<ShopScreen>();
                if (sc != null) sc.Show();
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase4Setup] Level_01 patched.");
        }

        // ============================================================================
        // Shop overlay builder (shared by MainMenu and Level_01)
        // ============================================================================
        static GameObject BuildShopOverlay(Transform canvasT)
        {
            var shop = NewUIObj("Phase4_ShopOverlay", canvasT);
            FullStretch(AsRT(shop));
            AddImage(shop, ColBg);
            var canvasGroup = shop.AddComponent<CanvasGroup>();
            var rt = AsRT(shop);

            // Header
            var (titleGO, _) = NewText(shop.transform, "Title", "SHOP", 32, ColText, true);
            AnchorTopCenter(AsRT(titleGO), new Vector2(300, 50), new Vector2(0, -30));

            var (balanceGO, balanceTMP) = NewText(shop.transform, "Balance", "0", 22, ColGold, TextAlignmentOptions.Right, true);
            AsRT(balanceGO).anchorMin = AsRT(balanceGO).anchorMax = new Vector2(1f, 1f);
            AsRT(balanceGO).pivot = new Vector2(1f, 1f);
            AsRT(balanceGO).sizeDelta = new Vector2(120, 40);
            AsRT(balanceGO).anchoredPosition = new Vector2(-20, -36);

            var (closeBtnGO, closeBtn, _) = NewButton(shop.transform, "CloseButton", "X", ColAccent, ColText, 18);
            AsRT(closeBtnGO).anchorMin = AsRT(closeBtnGO).anchorMax = new Vector2(0f, 1f);
            AsRT(closeBtnGO).pivot = new Vector2(0f, 1f);
            AsRT(closeBtnGO).sizeDelta = new Vector2(44, 44);
            AsRT(closeBtnGO).anchoredPosition = new Vector2(16, -20);

            // Tabs
            var (gemsTabBtnGO, gemsTabBtn, _) = NewButton(shop.transform, "GemsTabButton", "GEMS", ColAccent, ColText, 18);
            AnchorTopCenter(AsRT(gemsTabBtnGO), new Vector2(140, 44), new Vector2(-80, -100));
            var (offersTabBtnGO, offersTabBtn, _) = NewButton(shop.transform, "OffersTabButton", "OFFERS", ColAccent, ColText, 18);
            AnchorTopCenter(AsRT(offersTabBtnGO), new Vector2(140, 44), new Vector2(80, -100));

            // GEMS tab content
            var gemsTab = NewUIObj("GemsTab", shop.transform);
            FullStretch(AsRT(gemsTab), 20, 20, 160, 100);

            var (sm, smPriceTMP, smBuyBtn) = BuildGemCard(gemsTab.transform, "Small", "15 Gems", "", new Vector2(0, -10));
            var (md, mdPriceTMP, mdBuyBtn) = BuildGemCard(gemsTab.transform, "Medium", "50 Gems", "", new Vector2(0, -100));
            var (lg, lgPriceTMP, lgBuyBtn) = BuildGemCard(gemsTab.transform, "Large", "120 Gems", "Best Value", new Vector2(0, -190));

            // OFFERS tab content
            var offersTab = NewUIObj("OffersTab", shop.transform);
            FullStretch(AsRT(offersTab), 20, 20, 160, 100);

            var (starterGO, starterPriceTMP, starterBuyBtn) = BuildOfferCard(
                offersTab.transform, "StarterPack", "STARTER PACK", ColGold,
                "✓ Remove All Ads\n✓ 200 Coins\n✓ 20 Gems",
                new Vector2(0, -10), badge: "BEST VALUE");
            var (raGO, raPriceTMP, raBuyBtn) = BuildOfferCard(
                offersTab.transform, "RemoveAds", "REMOVE ADS", ColCta,
                "✓ No interstitials\n✓ No banners",
                new Vector2(0, -190), badge: null);

            offersTab.SetActive(false);

            // Footer — restore purchases.
            var (restoreGO, restoreBtn, _) = NewButton(shop.transform, "RestoreButton", "Restore Purchases", ColAccent, ColTextDim, 14);
            AnchorBottomCenter(AsRT(restoreGO), new Vector2(220, 36), new Vector2(0, 30));

            // Attach controller and wire.
            var ctrl = shop.AddComponent<ShopScreen>();
            SetField(ctrl, "balanceText", balanceTMP);
            SetField(ctrl, "closeButton", closeBtn);
            SetField(ctrl, "gemsTabButton", gemsTabBtn);
            SetField(ctrl, "offersTabButton", offersTabBtn);
            SetField(ctrl, "gemsTab", gemsTab);
            SetField(ctrl, "offersTab", offersTab);
            SetField(ctrl, "gemSmallPriceText", smPriceTMP);
            SetField(ctrl, "gemSmallBuyButton", smBuyBtn);
            SetField(ctrl, "gemMediumPriceText", mdPriceTMP);
            SetField(ctrl, "gemMediumBuyButton", mdBuyBtn);
            SetField(ctrl, "gemLargePriceText", lgPriceTMP);
            SetField(ctrl, "gemLargeBuyButton", lgBuyBtn);
            SetField(ctrl, "starterPackPriceText", starterPriceTMP);
            SetField(ctrl, "starterPackBuyButton", starterBuyBtn);
            SetField(ctrl, "removeAdsPriceText", raPriceTMP);
            SetField(ctrl, "removeAdsBuyButton", raBuyBtn);
            SetField(ctrl, "restoreButton", restoreBtn);
            SetField(ctrl, "rootRect", rt);
            SetField(ctrl, "canvasGroup", canvasGroup);

            return shop;
        }

        static (GameObject card, TMP_Text priceText, Button buyBtn) BuildGemCard(
            Transform parent, string name, string title, string badge, Vector2 offset)
        {
            var card = NewUIObj($"GemCard_{name}", parent);
            AnchorTopCenter(AsRT(card), new Vector2(320, 80), offset);
            AddImage(card, ColPanel);

            var (titleGO, _) = NewText(card.transform, "Title", title, 22, ColText, TextAlignmentOptions.Left, true);
            AsRT(titleGO).anchorMin = new Vector2(0f, 0.5f);
            AsRT(titleGO).anchorMax = new Vector2(0f, 0.5f);
            AsRT(titleGO).pivot = new Vector2(0f, 0.5f);
            AsRT(titleGO).sizeDelta = new Vector2(180, 40);
            AsRT(titleGO).anchoredPosition = new Vector2(20, 0);

            if (!string.IsNullOrEmpty(badge))
            {
                var (badgeGO, _) = NewText(card.transform, "Badge", badge, 12, ColGold, TextAlignmentOptions.Left, true);
                AsRT(badgeGO).anchorMin = new Vector2(0f, 0.5f);
                AsRT(badgeGO).anchorMax = new Vector2(0f, 0.5f);
                AsRT(badgeGO).pivot = new Vector2(0f, 0.5f);
                AsRT(badgeGO).sizeDelta = new Vector2(120, 20);
                AsRT(badgeGO).anchoredPosition = new Vector2(20, -22);
            }

            var (buyGO, buyBtn, _) = NewButton(card.transform, "BuyButton", "BUY", ColCta, ColText, 16);
            AsRT(buyGO).anchorMin = AsRT(buyGO).anchorMax = new Vector2(1f, 0.5f);
            AsRT(buyGO).pivot = new Vector2(1f, 0.5f);
            AsRT(buyGO).sizeDelta = new Vector2(100, 44);
            AsRT(buyGO).anchoredPosition = new Vector2(-12, 0);

            var (priceGO, priceTMP) = NewText(card.transform, "Price", "Loading...", 14, ColTextDim, TextAlignmentOptions.Right);
            AsRT(priceGO).anchorMin = AsRT(priceGO).anchorMax = new Vector2(1f, 0.5f);
            AsRT(priceGO).pivot = new Vector2(1f, 0.5f);
            AsRT(priceGO).sizeDelta = new Vector2(100, 18);
            AsRT(priceGO).anchoredPosition = new Vector2(-12, -28);

            return (card, priceTMP, buyBtn);
        }

        static (GameObject card, TMP_Text priceText, Button buyBtn) BuildOfferCard(
            Transform parent, string name, string title, Color titleColor, string body, Vector2 offset, string badge)
        {
            var card = NewUIObj($"OfferCard_{name}", parent);
            AnchorTopCenter(AsRT(card), new Vector2(320, 170), offset);
            AddImage(card, ColPanel);

            var (titleGO, _) = NewText(card.transform, "Title", title, 22, titleColor, TextAlignmentOptions.Left, true);
            AsRT(titleGO).anchorMin = new Vector2(0f, 1f);
            AsRT(titleGO).anchorMax = new Vector2(0f, 1f);
            AsRT(titleGO).pivot = new Vector2(0f, 1f);
            AsRT(titleGO).sizeDelta = new Vector2(220, 30);
            AsRT(titleGO).anchoredPosition = new Vector2(16, -12);

            if (!string.IsNullOrEmpty(badge))
            {
                var (badgeGO, _) = NewText(card.transform, "Badge", badge, 11, ColCta, TextAlignmentOptions.Right, true);
                AsRT(badgeGO).anchorMin = new Vector2(1f, 1f);
                AsRT(badgeGO).anchorMax = new Vector2(1f, 1f);
                AsRT(badgeGO).pivot = new Vector2(1f, 1f);
                AsRT(badgeGO).sizeDelta = new Vector2(100, 20);
                AsRT(badgeGO).anchoredPosition = new Vector2(-12, -12);
            }

            var (bodyGO, _) = NewText(card.transform, "Body", body, 14, ColTextDim, TextAlignmentOptions.Left);
            AsRT(bodyGO).anchorMin = new Vector2(0f, 0.5f);
            AsRT(bodyGO).anchorMax = new Vector2(0f, 0.5f);
            AsRT(bodyGO).pivot = new Vector2(0f, 0.5f);
            AsRT(bodyGO).sizeDelta = new Vector2(180, 80);
            AsRT(bodyGO).anchoredPosition = new Vector2(16, -10);

            var (buyGO, buyBtn, _) = NewButton(card.transform, "BuyButton", "BUY", ColCta, ColText, 16);
            AsRT(buyGO).anchorMin = AsRT(buyGO).anchorMax = new Vector2(1f, 0f);
            AsRT(buyGO).pivot = new Vector2(1f, 0f);
            AsRT(buyGO).sizeDelta = new Vector2(120, 44);
            AsRT(buyGO).anchoredPosition = new Vector2(-12, 16);

            var (priceGO, priceTMP) = NewText(card.transform, "Price", "Loading...", 14, ColTextDim, TextAlignmentOptions.Right);
            AsRT(priceGO).anchorMin = AsRT(priceGO).anchorMax = new Vector2(1f, 0f);
            AsRT(priceGO).pivot = new Vector2(1f, 0f);
            AsRT(priceGO).sizeDelta = new Vector2(120, 20);
            AsRT(priceGO).anchoredPosition = new Vector2(-12, 64);

            return (card, priceTMP, buyBtn);
        }

        // ============================================================================
        // Helpers
        // ============================================================================
        static void ClearPhase4Root(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                if (root.name.StartsWith("Phase4_")) Object.DestroyImmediate(root);
            }
            // Also remove Phase4_* children inside Canvas (shop overlay etc.).
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (var c in canvases)
                {
                    if (c == null) continue;
                    var t = c.transform;
                    for (int i = t.childCount - 1; i >= 0; i--)
                    {
                        var child = t.GetChild(i);
                        if (child != null && child.name.StartsWith("Phase4_")) Object.DestroyImmediate(child.gameObject);
                    }
                    // Also remove gem-icon button living inside TopBar.
                    var topBar = c.transform.Find("SafeAreaRoot/TopBar");
                    if (topBar != null)
                    {
                        for (int i = topBar.childCount - 1; i >= 0; i--)
                        {
                            var child = topBar.GetChild(i);
                            if (child != null && child.name.StartsWith("Phase4_")) Object.DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
        }

        static Canvas FindCanvas(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<Canvas>();
                if (c != null) return c;
            }
            return null;
        }

        static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform AsRT(GameObject go) => (RectTransform)go.transform;

        static void FullStretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void AnchorTopCenter(RectTransform rt, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        static void AnchorBottomCenter(RectTransform rt, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        static void AnchorCenter(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static (GameObject go, TMP_Text tmp) NewText(Transform parent, string name, string text, int fontSize, Color color, bool bold)
            => NewText(parent, name, text, fontSize, color, TextAlignmentOptions.Center, bold);

        static (GameObject go, TMP_Text tmp) NewText(Transform parent, string name, string text, int fontSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var go = NewUIObj(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return (go, tmp);
        }

        static (GameObject go, Button btn, TMP_Text label) NewButton(Transform parent, string name, string labelText,
            Color bg, Color textColor, int fontSize)
        {
            var go = NewUIObj(name, parent);
            AddImage(go, bg);
            var btn = go.AddComponent<Button>();
            var (lblGO, lbl) = NewText(go.transform, "Label", labelText, fontSize, textColor, TextAlignmentOptions.Center, true);
            FullStretch(AsRT(lblGO));
            return (go, btn, lbl);
        }

        /// <summary>Reflection-based [SerializeField] setter. Mirrors Phase3Setup's helper.</summary>
        static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var t = target.GetType();
            while (t != null)
            {
                var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) { f.SetValue(target, value); return; }
                t = t.BaseType;
            }
            Debug.LogWarning($"[Phase4Setup] SetField: '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
#endif
