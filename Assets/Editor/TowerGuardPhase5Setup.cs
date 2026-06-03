#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Enemies;
using TowerGuard.Gameplay;
using TowerGuard.Towers;
using TowerGuard.UI;
using TowerGuard.Utils;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 5 — orchestrates art generation, scene patches, sprite/audio rebinding,
    /// SplashScene creation, build settings, and mechanic-manager attachment.
    /// Idempotent: re-running clears all "Phase5_*" objects before rebuilding.
    /// </summary>
    public static class TowerGuardPhase5Setup
    {
        const string MainMenuScenePath  = "Assets/Scenes/MainMenu.unity";
        const string Level01ScenePath   = "Assets/Scenes/Level_01.unity";
        const string SplashScenePath    = "Assets/Scenes/SplashScene.unity";
        const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";

        // Sprite paths used by the LevelSelect polish pass.
        const string PanelDarkSprite    = "Assets/Sprites/UI/Panel_Dark.png";
        const string PanelGoldSprite    = "Assets/Sprites/UI/Panel_Gold.png";
        const string ButtonRedSprite    = "Assets/Sprites/UI/Button_Red.png";
        const string ButtonGoldSprite   = "Assets/Sprites/UI/Button_Gold.png";
        const string ButtonBlueSprite   = "Assets/Sprites/UI/Button_Blue.png";
        const string StarFullSprite     = "Assets/Sprites/UI/Star_Full.png";
        const string StarEmptySprite    = "Assets/Sprites/UI/Star_Empty.png";

        const string SfxFolder    = "Assets/Audio/SFX";
        const string UISpritesFolder = "Assets/Sprites/UI";

        [MenuItem("Tools/TowerGuard/Run All Phase 5 Setup", priority = 500)]
        public static void RunAll()
        {
            // 1) Generate ALL art + audio first.
            CreateGameArt.RunAll();
            CreateAudioClips.RunAll();
            CreateJCreatesLogo.RunAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2) Splash scene + build-settings reorder.
            CreateSplashScene();
            UpdateBuildSettingsForSplash();

            // 3) Patch scenes.
            PatchMainMenuPhase5();
            PatchLevelSelectPhase5();
            PatchLevel01Phase5();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase5Setup] RunAll: complete.");
        }

        [MenuItem("Tools/TowerGuard/Phase 5 - 01 Generate Art Only", priority = 510)]
        public static void Step01() { CreateGameArt.RunAll(); CreateJCreatesLogo.RunAll(); }
        [MenuItem("Tools/TowerGuard/Phase 5 - 02 Generate Audio Only", priority = 511)]
        public static void Step02() { CreateAudioClips.RunAll(); }
        [MenuItem("Tools/TowerGuard/Phase 5 - 03 Build SplashScene + Build Settings", priority = 512)]
        public static void Step03() { CreateSplashScene(); UpdateBuildSettingsForSplash(); AssetDatabase.SaveAssets(); }
        [MenuItem("Tools/TowerGuard/Phase 5 - 04 Patch MainMenu", priority = 513)]
        public static void Step04() { PatchMainMenuPhase5(); AssetDatabase.SaveAssets(); }
        [MenuItem("Tools/TowerGuard/Phase 5 - 05 Patch Level_01", priority = 514)]
        public static void Step05() { PatchLevel01Phase5(); AssetDatabase.SaveAssets(); }
        [MenuItem("Tools/TowerGuard/Phase 5 - 06 Patch LevelSelect", priority = 515)]
        public static void Step06() { PatchLevelSelectPhase5(); AssetDatabase.SaveAssets(); }

        // ============================================================================
        // SplashScene
        // ============================================================================
        static void CreateSplashScene()
        {
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera with black background.
            var camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0A, 0x0A, 0x1A, 0xFF);
            cam.orthographic = true;
            cam.orthographicSize = 5;
            EditorSceneManager.MoveGameObjectToScene(camGO, scene);

            // Canvas.
            var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            EditorSceneManager.MoveGameObjectToScene(canvasGO, scene);

            // EventSystem.
            var es = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            EditorSceneManager.MoveGameObjectToScene(es, scene);

            // Logo image.
            var logoGO = new GameObject("Logo", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            logoGO.transform.SetParent(canvasGO.transform, false);
            var rt = (RectTransform)logoGO.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(400, 200);
            rt.anchoredPosition = Vector2.zero;
            var img = logoGO.GetComponent<Image>();
            var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/JCreates_Logo.png");
            if (logoSprite != null) img.sprite = logoSprite;
            img.preserveAspect = true;
            var cg = logoGO.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            // SplashController.
            var splash = canvasGO.AddComponent<SplashController>();
            SetField(splash, "logoGroup", cg);
            SetField(splash, "logoImage", img);
            SetField(splash, "nextSceneName", "MainMenu");

            EditorSceneManager.SaveScene(scene, SplashScenePath);
            Debug.Log($"[Phase5Setup] SplashScene saved at {SplashScenePath}.");
        }

        static void UpdateBuildSettingsForSplash()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(SplashScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(LevelSelectScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(Level01ScenePath, true));
            // Preserve any other scenes that were already in build settings.
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path == SplashScenePath || s.path == MainMenuScenePath
                 || s.path == LevelSelectScenePath || s.path == Level01ScenePath) continue;
                scenes.Add(s);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Phase5Setup] Build Settings reordered: Splash(0), MainMenu(1), LevelSelect(2), Level_01(3).");
        }

        // ============================================================================
        // MainMenu patches
        // ============================================================================
        static void PatchMainMenuPhase5()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            ClearPhase5Root(scene);

            // Attach AudioManager music start.
            var audioMusic = new GameObject("Phase5_MenuMusic");
            EditorSceneManager.MoveGameObjectToScene(audioMusic, scene);
            var menuMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Music_MainMenu.wav");
            if (menuMusicClip != null)
            {
                var src = audioMusic.AddComponent<AudioSource>();
                src.clip = menuMusicClip;
                src.loop = true;
                src.playOnAwake = true;
                src.volume = 0.6f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase5Setup] MainMenu patched (menu music).");
        }

        // ============================================================================
        // LevelSelect polish — swap flat panel/button images for the new premium
        // gradient sprites, and bump the BACK button's contrast so the screen
        // actually feels designed instead of "buttons over a flat box".
        // ============================================================================
        static void PatchLevelSelectPhase5()
        {
            var scene = EditorSceneManager.OpenScene(LevelSelectScenePath, OpenSceneMode.Single);

            var panelDark = AssetDatabase.LoadAssetAtPath<Sprite>(PanelDarkSprite);
            var panelGold = AssetDatabase.LoadAssetAtPath<Sprite>(PanelGoldSprite);
            var btnRed    = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonRedSprite);
            var btnGold   = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonGoldSprite);
            var btnBlue   = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBlueSprite);
            var starFull  = AssetDatabase.LoadAssetAtPath<Sprite>(StarFullSprite);
            var starEmpty = AssetDatabase.LoadAssetAtPath<Sprite>(StarEmptySprite);

            int patched = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;

                // 1) Walk every level card. Each one is named LevelCard_N.
                var cards = root.GetComponentsInChildren<TowerGuard.UI.LevelCardUI>(true);
                foreach (var card in cards)
                {
                    var t = card.transform;

                    // Card background: gold-bordered Panel for level 1 (the unlocked one),
                    // dark for the others.
                    var cardImg = t.GetComponent<Image>();
                    bool isUnlocked = SafeGetIsUnlocked(card);
                    if (cardImg != null)
                    {
                        cardImg.sprite = isUnlocked ? panelGold : panelDark;
                        cardImg.type = Image.Type.Sliced;
                        cardImg.color = Color.white;
                    }

                    // PLAY button: gold for unlocked, blue for locked (dim look).
                    var playRT = FindDeepChild(t, "PlayButton");
                    if (playRT != null)
                    {
                        var playImg = playRT.GetComponent<Image>();
                        if (playImg != null)
                        {
                            playImg.sprite = isUnlocked ? btnGold : btnBlue;
                            playImg.type = Image.Type.Sliced;
                            playImg.color = Color.white;
                        }
                    }

                    // Star images: assign the proper full-shape sprite so even the
                    // grey "unearned" stars are clearly star-shaped, not faint dots.
                    for (int s = 0; s < 3; s++)
                    {
                        var starT = FindDeepChild(t, $"Star_{s}");
                        if (starT == null) continue;
                        var starImg = starT.GetComponent<Image>();
                        if (starImg != null && starEmpty != null)
                        {
                            starImg.sprite = starEmpty; // RefreshStars will recolor
                            starImg.preserveAspect = true;
                        }
                    }

                    patched++;
                }

                // 2) Bump the < BACK button styling so it doesn't blend into the bg.
                var backT = FindDeepChild(root.transform, "BackButton");
                if (backT != null)
                {
                    var img = backT.GetComponent<Image>();
                    if (img != null && btnRed != null)
                    {
                        img.sprite = btnRed;
                        img.type = Image.Type.Sliced;
                        img.color = Color.white;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Phase5Setup] LevelSelect patched — {patched} level cards restyled.");
        }

        static bool SafeGetIsUnlocked(TowerGuard.UI.LevelCardUI card)
        {
            // The field is private; reflect into it for the styling decision.
            var f = typeof(TowerGuard.UI.LevelCardUI).GetField(
                "isUnlocked", BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null && (bool)f.GetValue(card);
        }

        // ============================================================================
        // Level_01 patches
        // ============================================================================
        static void PatchLevel01Phase5()
        {
            var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            ClearPhase5Root(scene);

            // Polish: disable the Phase 2 OnGUI DebugHUD that overlays a giant
            // "TowerGuard — Debug" panel in the top-left, overlapping the real
            // Phase 3 TopBar HUD. The component itself is left in place so the
            // user can flip it back on for diagnostics if needed.
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                var debugHUDs = root.GetComponentsInChildren<TowerGuard.Utils.DebugHUD>(true);
                foreach (var d in debugHUDs) d.enabled = false;
            }

            // Polish: the Phase 4 gem-shop button was anchored at the right edge
            // of the TopBar where it overlaps Soft + Hard currency labels. Move
            // it well below the TopBar so it has its own room.
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                var gemBtn = root.transform.Find("UICanvas/SafeAreaRoot/TopBar/Phase4_GemShopButton");
                if (gemBtn == null) gemBtn = FindDeepChild(root.transform, "Phase4_GemShopButton");
                if (gemBtn != null)
                {
                    var rt = gemBtn as RectTransform ?? gemBtn.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        // Move into TopBar's right area but at the very corner, smaller.
                        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                        rt.pivot = new Vector2(1f, 1f);
                        rt.sizeDelta = new Vector2(40, 40);
                        rt.anchoredPosition = new Vector2(-12, -78); // just below the TopBar
                    }
                }
            }

            // Mechanic managers.
            var mgrRoot = new GameObject("Phase5_MechanicsRoot");
            EditorSceneManager.MoveGameObjectToScene(mgrRoot, scene);
            mgrRoot.AddComponent<ResonanceManager>();
            var bounty = mgrRoot.AddComponent<BountyManager>();
            var crownSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Crown.png");
            if (crownSprite != null)
            {
                // Build a tiny crown prefab GameObject to hand to BountyManager.
                var crownGO = new GameObject("Phase5_CrownTemplate");
                crownGO.SetActive(false);
                var sr = crownGO.AddComponent<SpriteRenderer>();
                sr.sprite = crownSprite;
                sr.sortingOrder = 8;
                crownGO.transform.SetParent(mgrRoot.transform);
                SetField(bounty, "crownPrefab", crownGO);
            }

            var pwn = mgrRoot.AddComponent<PowerNodeManager>();
            // Three power-node positions (cell centres). Tune in the inspector if needed.
            pwn.SetNodes(new[] { new Vector2(2.5f, 1.5f), new Vector2(-3.5f, 0.5f), new Vector2(0.5f, -2.5f) });

            // Combo + WaveForecast UI on a dedicated overlay canvas so they sit on top
            // of every other Phase 3 UI.
            var canvasGO = new GameObject("Phase5_OverlayCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            EditorSceneManager.MoveGameObjectToScene(canvasGO, scene);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);

            // Combo label
            var comboGO = new GameObject("Phase5_ComboLabel", typeof(RectTransform), typeof(CanvasGroup));
            comboGO.transform.SetParent(canvasGO.transform, false);
            var crt = (RectTransform)comboGO.transform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(400, 80);
            crt.anchoredPosition = Vector2.zero;
            var comboCg = comboGO.GetComponent<CanvasGroup>();

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(comboGO.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 56; label.fontStyle = FontStyles.Bold;
            label.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            label.text = "COMBO!";
            comboGO.SetActive(false);

            // Flash image (full screen white)
            var flashGO = new GameObject("Phase5_Flash", typeof(RectTransform), typeof(Image));
            flashGO.transform.SetParent(canvasGO.transform, false);
            var frt = (RectTransform)flashGO.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            var flashImg = flashGO.GetComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0f);
            flashImg.raycastTarget = false;
            flashGO.SetActive(false);

            // Combo manager
            var combo = mgrRoot.AddComponent<KillComboManager>();
            SetField(combo, "comboLabelRoot", crt);
            SetField(combo, "comboLabelText", label);
            SetField(combo, "comboLabelCanvasGroup", comboCg);
            SetField(combo, "flashImage", flashImg);
            var comboClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/SFX_Combo.wav");
            if (comboClip != null) SetField(combo, "comboClip", comboClip);

            // Wave Forecast UI panel — anchored TOP-CENTER below the TopBar so it
            // doesn't overlap the START WAVE button or the right-side currency
            // readouts. Slightly smaller than the previous version.
            var forecastGO = new GameObject("Phase5_WaveForecast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            forecastGO.transform.SetParent(canvasGO.transform, false);
            var frRT = (RectTransform)forecastGO.transform;
            frRT.anchorMin = frRT.anchorMax = new Vector2(0.5f, 1f);
            frRT.pivot = new Vector2(0.5f, 1f);
            frRT.sizeDelta = new Vector2(320, 96);
            frRT.anchoredPosition = new Vector2(0, -84);
            var forecastBgImg = forecastGO.GetComponent<Image>();
            forecastBgImg.color = new Color32(0x16, 0x21, 0x3E, 0xE6);
            // Polish: don't let this informational panel swallow clicks to the
            // START WAVE button or other gameplay UI underneath it.
            forecastBgImg.raycastTarget = false;
            var forecastCg = forecastGO.GetComponent<CanvasGroup>();
            forecastCg.blocksRaycasts = false;
            forecastCg.interactable = false;

            var compoGO = new GameObject("Composition", typeof(RectTransform));
            compoGO.transform.SetParent(forecastGO.transform, false);
            var cprt = (RectTransform)compoGO.transform;
            cprt.anchorMin = new Vector2(0, 1); cprt.anchorMax = new Vector2(1, 1);
            cprt.pivot = new Vector2(0.5f, 1f);
            cprt.sizeDelta = new Vector2(0, 28); cprt.anchoredPosition = new Vector2(0, -8);
            var compoTMP = compoGO.AddComponent<TextMeshProUGUI>();
            compoTMP.alignment = TextAlignmentOptions.Center;
            compoTMP.fontSize = 16; compoTMP.fontStyle = FontStyles.Bold;
            compoTMP.color = Color.white;
            compoTMP.text = "Next wave:";

            var tipGO = new GameObject("Tip", typeof(RectTransform));
            tipGO.transform.SetParent(forecastGO.transform, false);
            var tprt = (RectTransform)tipGO.transform;
            tprt.anchorMin = new Vector2(0, 0); tprt.anchorMax = new Vector2(1, 1);
            tprt.offsetMin = new Vector2(8, 8); tprt.offsetMax = new Vector2(-8, -36);
            var tipTMP = tipGO.AddComponent<TextMeshProUGUI>();
            tipTMP.alignment = TextAlignmentOptions.TopLeft;
            tipTMP.fontSize = 12; tipTMP.color = new Color32(0xBB, 0xBB, 0xBB, 0xFF);
            tipTMP.text = "";
            tipTMP.enableWordWrapping = true;

            var diffBarGO = new GameObject("DifficultyBar", typeof(RectTransform), typeof(Image));
            diffBarGO.transform.SetParent(forecastGO.transform, false);
            var dbrt = (RectTransform)diffBarGO.transform;
            dbrt.anchorMin = new Vector2(0, 0); dbrt.anchorMax = new Vector2(1, 0);
            dbrt.pivot = new Vector2(0.5f, 0f);
            dbrt.sizeDelta = new Vector2(0, 4); dbrt.anchoredPosition = new Vector2(0, 4);
            var dbImg = diffBarGO.GetComponent<Image>();
            dbImg.color = new Color32(0xE9, 0x45, 0x60, 0xFF);
            dbImg.type = Image.Type.Filled;
            dbImg.fillMethod = Image.FillMethod.Horizontal;
            dbImg.fillAmount = 0.1f;

            var forecast = forecastGO.AddComponent<WaveForecastUI>();
            SetField(forecast, "rootRect", frRT);
            SetField(forecast, "canvasGroup", forecastCg);
            SetField(forecast, "compositionText", compoTMP);
            SetField(forecast, "tipText", tipTMP);
            SetField(forecast, "difficultyBar", dbImg);

            // Gameplay music source.
            var musicGO = new GameObject("Phase5_GameplayMusic");
            EditorSceneManager.MoveGameObjectToScene(musicGO, scene);
            var gameplayMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/Music_Gameplay.wav");
            if (gameplayMusic != null)
            {
                var src = musicGO.AddComponent<AudioSource>();
                src.clip = gameplayMusic;
                src.loop = true;
                src.playOnAwake = true;
                src.volume = 0.5f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase5Setup] Level_01 patched (mechanics + overlay canvas + music).");
        }

        // ============================================================================
        // Helpers
        // ============================================================================
        static void ClearPhase5Root(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name.StartsWith("Phase5_")) Object.DestroyImmediate(root);
            }
            // Clean Phase5_ children inside any canvas.
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
                        if (child != null && child.name.StartsWith("Phase5_")) Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

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
            Debug.LogWarning($"[Phase5Setup] SetField: '{fieldName}' not found on {target.GetType().Name}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Recursively search for a child by exact name, returning the first match.</summary>
        static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeepChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
