#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TowerGuard.Core;
using TowerGuard.Monetization;
using TowerGuard.Towers;
using TowerGuard.UI;
using TowerGuard.Utils;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 3 scene-level UI bring-up. "Run All Phase 3 Setup" rebuilds the UI
    /// hierarchies on MainMenu.unity, LevelSelect.unity, and Level_01.unity, wiring
    /// every serialized [SerializeField] reference on UIManager / PauseUI / GameOverUI /
    /// VictoryUI / MainMenuUI / LevelSelectUI / LevelCardUI / SettingsPanel / TowerCardUI
    /// programmatically so no manual Inspector clicking is required.
    ///
    /// Idempotent: each scene's UI root is destroyed and rebuilt on every run.
    /// </summary>
    public static class TowerGuardPhase3Setup
    {
        // ---- Scene paths ----
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        const string LevelSelectScenePath = "Assets/Scenes/LevelSelect.unity";
        const string Level01ScenePath = "Assets/Scenes/Level_01.unity";
        const string UiSpritesFolder = "Assets/Sprites/UI";
        const string TowerDataFolder = "Assets/ScriptableObjects/Towers";

        // ---- Palette ----
        static readonly Color ColBg = new Color32(0x1A, 0x1A, 0x2E, 0xFF);        // background
        static readonly Color ColPanel = new Color32(0x16, 0x21, 0x3E, 0xFF);     // panel
        static readonly Color ColAccent = new Color32(0x0F, 0x34, 0x60, 0xFF);    // accent
        static readonly Color ColCta = new Color32(0xE9, 0x45, 0x60, 0xFF);       // CTA / danger
        static readonly Color ColGold = new Color32(0xF5, 0xA6, 0x23, 0xFF);      // gold
        static readonly Color ColText = new Color32(0xFF, 0xFF, 0xFF, 0xFF);      // text
        static readonly Color ColTextDim = new Color32(0xBB, 0xBB, 0xBB, 0xFF);   // dim text
        static readonly Color ColStarUnearned = new Color32(0x44, 0x44, 0x44, 0xFF);
        static readonly Color ColDim50 = new Color(0f, 0f, 0f, 0.5f);
        static readonly Color ColDim70 = new Color(0f, 0f, 0f, 0.7f);
        static readonly Color ColDisabledOverlay = new Color(0f, 0f, 0f, 0.55f);

        // iPhone 14 reference portrait res.
        static readonly Vector2 ReferenceRes = new Vector2(390f, 844f);

        // ============================================================================
        // Menu entry points
        // ============================================================================
        [MenuItem("Tools/TowerGuard/Run All Phase 3 Setup", priority = 200)]
        public static void RunAll()
        {
            EnsureTMPResources();
            EnsureFolder(UiSpritesFolder);
            EnsureUISprites();

            BuildMainMenuScene();
            BuildLevelSelectScene();
            BuildLevel01UI();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3Setup] RunAll: complete.");
        }

        [MenuItem("Tools/TowerGuard/Phase 3 - 01 Build MainMenu Scene", priority = 210)]
        public static void Step01_MainMenu() { EnsureTMPResources(); EnsureUISprites(); BuildMainMenuScene(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/Phase 3 - 02 Build LevelSelect Scene", priority = 211)]
        public static void Step02_LevelSelect() { EnsureTMPResources(); EnsureUISprites(); BuildLevelSelectScene(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/Phase 3 - 03 Build Level_01 UI", priority = 212)]
        public static void Step03_Level01() { EnsureTMPResources(); EnsureUISprites(); BuildLevel01UI(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/Phase 3 - Import TMP Essentials", priority = 213)]
        public static void Step_ImportTMP() { EnsureTMPResources(); AssetDatabase.Refresh(); }

        // ============================================================================
        // TMP resources — import the default font so every TextMeshProUGUI renders.
        // ============================================================================
        const string TMPDefaultFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        static TMP_FontAsset _cachedDefaultFont;

        /// <summary>
        /// Imports TMP Essential Resources from the UGUI package cache if the default
        /// LiberationSans SDF font asset is missing. Without this, every TMP_Text created
        /// by NewText() has a null font and renders blank.
        /// </summary>
        static void EnsureTMPResources()
        {
            if (File.Exists(TMPDefaultFontPath)) return;

            // Unity 6 ships TMP inside com.unity.ugui. The cache dir has a hash suffix.
            string cacheRoot = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot))
            {
                Debug.LogWarning("[Phase3Setup] Library/PackageCache not found — skipping TMP import.");
                return;
            }

            string found = null;
            foreach (var dir in Directory.GetDirectories(cacheRoot, "com.unity.ugui*"))
            {
                string candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(candidate)) { found = candidate; break; }
            }

            if (found == null)
            {
                Debug.LogWarning("[Phase3Setup] TMP Essential Resources.unitypackage not found in com.unity.ugui cache. Import manually via Window > TextMeshPro > Import TMP Essential Resources.");
                return;
            }

            Debug.Log($"[Phase3Setup] Importing TMP Essentials from {found}");
            AssetDatabase.ImportPackage(found, false);
            AssetDatabase.Refresh();
        }

        /// <summary>Loads the TMP default font, caching it across calls. Called from NewText().</summary>
        static TMP_FontAsset GetDefaultTMPFont()
        {
            if (_cachedDefaultFont != null) return _cachedDefaultFont;
            _cachedDefaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMPDefaultFontPath);
            if (_cachedDefaultFont == null)
            {
                _cachedDefaultFont = TMP_Settings.defaultFontAsset;
            }
            return _cachedDefaultFont;
        }

        // ============================================================================
        // Sprite atoms (flat color + rounded-corner approximations as flat square pngs)
        // ============================================================================
        static Sprite sprSquare;
        static Sprite sprStar;
        static Sprite sprLock;

        static void EnsureUISprites()
        {
            sprSquare = CreateOrGetFlatSprite("UI_Square", 4, 4, Color.white);
            sprStar = CreateOrGetStarSprite("UI_Star", 32);
            sprLock = CreateOrGetLockSprite("UI_Lock", 32);
        }

        static Sprite CreateOrGetFlatSprite(string name, int w, int h, Color color)
        {
            string path = $"{UiSpritesFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color[] px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = color;
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            ApplyUiImportSettings(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite CreateOrGetStarSprite(string name, int size)
        {
            string path = $"{UiSpritesFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                float cx = size * 0.5f, cy = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - cx) / (size * 0.5f);
                        float dy = (y + 0.5f - cy) / (size * 0.5f);
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx);
                        // 5-point star radial mask.
                        float petal = Mathf.Cos(5f * ang) * 0.3f + 0.7f;
                        px[y * size + x] = (r < petal) ? Color.white : new Color(0, 0, 0, 0);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            ApplyUiImportSettings(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite CreateOrGetLockSprite(string name, int size)
        {
            string path = $"{UiSpritesFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool body = (x > size * 0.25f && x < size * 0.75f && y > size * 0.15f && y < size * 0.6f);
                        bool shackle = false;
                        float cx = size * 0.5f, cy = size * 0.65f;
                        float dx = x - cx, dy = y - cy;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r > size * 0.15f && r < size * 0.25f && y > cy) shackle = true;
                        px[y * size + x] = (body || shackle) ? Color.white : new Color(0, 0, 0, 0);
                    }
                }
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            ApplyUiImportSettings(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ApplyUiImportSettings(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }

        // ============================================================================
        // Folder / field helpers
        // ============================================================================
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void SetField(Object target, string fieldName, object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Phase3Setup] Missing field '{fieldName}' on {target.GetType().Name}.");
                return;
            }
            AssignSerialized(prop, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArrayField<T>(Object target, string fieldName, IList<T> values) where T : Object
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Phase3Setup] Missing array field '{fieldName}' on {target.GetType().Name}.");
                return;
            }
            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignSerialized(SerializedProperty prop, object value)
        {
            if (value == null) { if (prop.propertyType == SerializedPropertyType.ObjectReference) prop.objectReferenceValue = null; return; }
            if (value is Object uo) { prop.objectReferenceValue = uo; return; }
            if (value is bool b) { prop.boolValue = b; return; }
            if (value is int i) { prop.intValue = i; return; }
            if (value is float f) { prop.floatValue = f; return; }
            if (value is string s) { prop.stringValue = s; return; }
            if (value is Color c) { prop.colorValue = c; return; }
            if (value is Vector2 v2) { prop.vector2Value = v2; return; }
            if (value is Vector3 v3) { prop.vector3Value = v3; return; }
        }

        // ============================================================================
        // RectTransform / UI object builders
        // ============================================================================
        static GameObject NewUIObj(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform AsRT(GameObject go) => (RectTransform)go.transform;

        static void FullStretch(RectTransform rt, float leftPad = 0, float rightPad = 0, float topPad = 0, float bottomPad = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(leftPad, bottomPad);
            rt.offsetMax = new Vector2(-rightPad, -topPad);
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

        static Image AddImage(GameObject go, Color color, Sprite sprite = null)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite != null ? sprite : sprSquare;
            img.type = Image.Type.Sliced;
            return img;
        }

        static (GameObject go, TMP_Text tmp) NewText(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var go = NewUIObj(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = GetDefaultTMPFont();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return (go, tmp);
        }

        static (GameObject go, Button btn, TMP_Text label) NewButton(Transform parent, string name, string labelText, Color bg, Color textColor, int fontSize)
        {
            var go = NewUIObj(name, parent);
            AddImage(go, bg);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            btn.colors = colors;

            var (lblGO, lbl) = NewText(go.transform, "Label", labelText, fontSize, textColor, TextAlignmentOptions.Center, true);
            FullStretch(AsRT(lblGO));
            return (go, btn, lbl);
        }

        static Canvas EnsureCanvas(Scene scene, out EventSystem eventSystem)
        {
            eventSystem = null;
            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (canvas == null)
                {
                    canvas = root.GetComponentInChildren<Canvas>();
                }
                var es = root.GetComponentInChildren<EventSystem>();
                if (es != null) eventSystem = es;
            }

            if (canvas == null)
            {
                var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = cgo.GetComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceRes;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (eventSystem == null)
            {
                var esgo = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                eventSystem = esgo.GetComponent<EventSystem>();
            }
            return canvas;
        }

        static GameObject BuildSafeRoot(Transform canvas)
        {
            var root = NewUIObj("SafeAreaRoot", canvas);
            FullStretch(AsRT(root));
            root.AddComponent<SafeAreaPanel>();
            return root;
        }

        // Remove any existing auto-generated UI canvas so re-runs are idempotent.
        // Phase 2 scenes ship with a "UICanvas" already in place that we want to REUSE
        // (its position/scaler is fine), but every UI child it owns must be wiped so the
        // rebuild doesn't stack a fresh SafeAreaRoot on top of the previous one. We
        // therefore: (1) destroy any stray root named "Canvas"/"EventSystem" or any of
        // the named UI-controller roots that the Phase 3 builders create at the scene
        // root ("MainMenuUI", "LevelSelectUI" — without this, every re-run accumulates
        // another duplicate that subscribes to button events and silently no-ops because
        // its serialized references are null), and (2) clear ALL children of every
        // Canvas component left in the scene.
        static void ClearScenes_PhaseUIRoots(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Canvas" || root.name == "EventSystem"
                    || root.name == "MainMenuUI" || root.name == "LevelSelectUI")
                {
                    Object.DestroyImmediate(root);
                }
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                var canvases = root.GetComponentsInChildren<Canvas>(true);
                foreach (var c in canvases)
                {
                    if (c == null) continue;
                    var t = c.transform;
                    // Iterate backwards so DestroyImmediate doesn't shift indices we still need.
                    for (int i = t.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(t.GetChild(i).gameObject);
                    }
                }
            }
        }

        // ============================================================================
        // MainMenu scene
        // ============================================================================
        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            ClearScenes_PhaseUIRoots(scene);

            var canvas = EnsureCanvas(scene, out _);
            var canvasT = canvas.transform;

            // Full-screen background.
            var bg = NewUIObj("Background", canvasT);
            FullStretch(AsRT(bg));
            AddImage(bg, ColBg);

            // Safe-area root hosts everything UI-facing.
            var safe = BuildSafeRoot(canvasT);

            // Title group
            var titleGroup = NewUIObj("TitleGroup", safe.transform);
            AnchorTopCenter(AsRT(titleGroup), new Vector2(360, 200), new Vector2(0, -140));
            var titleCG = titleGroup.AddComponent<CanvasGroup>();

            var (titleGO, titleTMP) = NewText(titleGroup.transform, "Title", "TOWER GUARD", 56, ColText, TextAlignmentOptions.Center, true);
            FullStretch(AsRT(titleGO), 0, 0, 0, 80);
            var (tagGO, tagTMP) = NewText(titleGroup.transform, "Tagline", "Defend Your Base", 22, ColTextDim, TextAlignmentOptions.Center);
            AsRT(tagGO).anchorMin = new Vector2(0f, 0f); AsRT(tagGO).anchorMax = new Vector2(1f, 0f);
            AsRT(tagGO).pivot = new Vector2(0.5f, 0f);
            AsRT(tagGO).sizeDelta = new Vector2(0, 40);
            AsRT(tagGO).anchoredPosition = new Vector2(0, 20);

            // Buttons column (bottom half)
            var (playGO, playBtn, _) = NewButton(safe.transform, "PlayButton", "PLAY", ColCta, ColText, 32);
            AnchorCenter(AsRT(playGO), new Vector2(260, 64));
            AsRT(playGO).anchoredPosition = new Vector2(0, -40);

            var (settingsGO, settingsBtn, _) = NewButton(safe.transform, "SettingsButton", "SETTINGS", ColAccent, ColText, 24);
            AnchorCenter(AsRT(settingsGO), new Vector2(260, 56));
            AsRT(settingsGO).anchoredPosition = new Vector2(0, -120);

            var (creditsGO, creditsBtn, _) = NewButton(safe.transform, "CreditsButton", "CREDITS", ColAccent, ColText, 24);
            AnchorCenter(AsRT(creditsGO), new Vector2(260, 56));
            AsRT(creditsGO).anchoredPosition = new Vector2(0, -200);

            // Credits overlay (inactive at start)
            var creditsOverlay = NewUIObj("CreditsOverlay", canvasT);
            FullStretch(AsRT(creditsOverlay));
            AddImage(creditsOverlay, ColDim70);
            creditsOverlay.AddComponent<CanvasGroup>();

            var creditsPanel = NewUIObj("Panel", creditsOverlay.transform);
            AnchorCenter(AsRT(creditsPanel), new Vector2(320, 380));
            AddImage(creditsPanel, ColPanel);

            var (creditsTitleGO, _) = NewText(creditsPanel.transform, "Title", "CREDITS", 28, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(creditsTitleGO), new Vector2(300, 50), new Vector2(0, -20));
            var (creditsBodyGO, _) = NewText(creditsPanel.transform, "Body",
                "TowerGuard\nA TowerGuard Studios game\nDeveloped with Unity 6\nAll art: placeholder.",
                18, ColTextDim, TextAlignmentOptions.Center);
            AnchorCenter(AsRT(creditsBodyGO), new Vector2(280, 180));
            var (credCloseGO, credCloseBtn, _) = NewButton(creditsPanel.transform, "CloseButton", "CLOSE", ColAccent, ColText, 22);
            AnchorBottomCenter(AsRT(credCloseGO), new Vector2(200, 52), new Vector2(0, 24));

            creditsOverlay.SetActive(false);

            // Settings panel (inactive at start; reused by MainMenuUI). The panel
            // factory wraps the panel in a full-screen dim "SettingsOverlay" parent
            // whose Image blocks raycasts to everything beneath it. Disable BOTH the
            // inner panel and the outer overlay so the menu buttons remain clickable.
            var settingsPanel = BuildSettingsPanel(canvasT);
            settingsPanel.gameObject.SetActive(false);
            if (settingsPanel.transform.parent != null)
                settingsPanel.transform.parent.gameObject.SetActive(false);

            // Attach MainMenuUI controller.
            var controller = new GameObject("MainMenuUI", typeof(MainMenuUI));
            controller.transform.SetParent(canvasT.parent, true);
            var mm = controller.GetComponent<MainMenuUI>();
            SetField(mm, "titleGroup", AsRT(titleGroup));
            SetField(mm, "titleCanvasGroup", titleCG);
            SetField(mm, "playButton", playBtn);
            SetField(mm, "settingsButton", settingsBtn);
            SetField(mm, "creditsButton", creditsBtn);
            SetField(mm, "settingsPanel", settingsPanel);
            SetField(mm, "creditsOverlay", creditsOverlay);
            SetField(mm, "creditsCloseButton", credCloseBtn);
            SetField(mm, "levelSelectScene", "LevelSelect");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase3Setup] MainMenu scene built.");
        }

        // ============================================================================
        // Shared Settings panel
        // ============================================================================
        static SettingsPanel BuildSettingsPanel(Transform canvasT)
        {
            var overlay = NewUIObj("SettingsOverlay", canvasT);
            FullStretch(AsRT(overlay));
            AddImage(overlay, ColDim70);

            var panelGO = NewUIObj("SettingsPanel", overlay.transform);
            AnchorCenter(AsRT(panelGO), new Vector2(340, 520));
            AddImage(panelGO, ColPanel);
            var cg = panelGO.AddComponent<CanvasGroup>();
            var panel = panelGO.AddComponent<SettingsPanel>();

            var (titleGO, _) = NewText(panelGO.transform, "Title", "SETTINGS", 28, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(titleGO), new Vector2(300, 50), new Vector2(0, -20));

            // SFX slider row
            var (sfxLblGO, _) = NewText(panelGO.transform, "SfxLabel", "SFX", 20, ColText, TextAlignmentOptions.Left);
            AsRT(sfxLblGO).anchorMin = new Vector2(0f, 1f); AsRT(sfxLblGO).anchorMax = new Vector2(0f, 1f);
            AsRT(sfxLblGO).pivot = new Vector2(0f, 1f);
            AsRT(sfxLblGO).sizeDelta = new Vector2(90, 30);
            AsRT(sfxLblGO).anchoredPosition = new Vector2(20, -100);

            var sfxSlider = BuildSlider(panelGO.transform, "SfxSlider", new Vector2(120, -110), new Vector2(180, 20));
            var (sfxValGO, _) = NewText(panelGO.transform, "SfxValue", "80%", 20, ColTextDim, TextAlignmentOptions.Right);
            AsRT(sfxValGO).anchorMin = new Vector2(1f, 1f); AsRT(sfxValGO).anchorMax = new Vector2(1f, 1f);
            AsRT(sfxValGO).pivot = new Vector2(1f, 1f);
            AsRT(sfxValGO).sizeDelta = new Vector2(90, 30);
            AsRT(sfxValGO).anchoredPosition = new Vector2(-20, -100);

            // Music slider row
            var (musLblGO, _) = NewText(panelGO.transform, "MusicLabel", "MUSIC", 20, ColText, TextAlignmentOptions.Left);
            AsRT(musLblGO).anchorMin = new Vector2(0f, 1f); AsRT(musLblGO).anchorMax = new Vector2(0f, 1f);
            AsRT(musLblGO).pivot = new Vector2(0f, 1f);
            AsRT(musLblGO).sizeDelta = new Vector2(90, 30);
            AsRT(musLblGO).anchoredPosition = new Vector2(20, -170);

            var musicSlider = BuildSlider(panelGO.transform, "MusicSlider", new Vector2(120, -180), new Vector2(180, 20));
            var (musValGO, _) = NewText(panelGO.transform, "MusicValue", "60%", 20, ColTextDim, TextAlignmentOptions.Right);
            AsRT(musValGO).anchorMin = new Vector2(1f, 1f); AsRT(musValGO).anchorMax = new Vector2(1f, 1f);
            AsRT(musValGO).pivot = new Vector2(1f, 1f);
            AsRT(musValGO).sizeDelta = new Vector2(90, 30);
            AsRT(musValGO).anchoredPosition = new Vector2(-20, -170);

            // Haptics toggle
            var hapticsToggle = BuildToggle(panelGO.transform, "HapticsToggle", "HAPTICS", new Vector2(20, -240));

            // Restore button
            var (restoreGO, restoreBtn, _) = NewButton(panelGO.transform, "RestoreButton", "RESTORE PURCHASES", ColAccent, ColText, 18);
            AnchorBottomCenter(AsRT(restoreGO), new Vector2(260, 52), new Vector2(0, 90));

            // Close
            var (closeGO, closeBtn, _) = NewButton(panelGO.transform, "CloseButton", "CLOSE", ColCta, ColText, 22);
            AnchorBottomCenter(AsRT(closeGO), new Vector2(260, 52), new Vector2(0, 26));

            SetField(panel, "rootRect", AsRT(panelGO));
            SetField(panel, "canvasGroup", cg);
            SetField(panel, "sfxSlider", sfxSlider);
            SetField(panel, "sfxValueText", sfxValGO.GetComponent<TMP_Text>());
            SetField(panel, "musicSlider", musicSlider);
            SetField(panel, "musicValueText", musValGO.GetComponent<TMP_Text>());
            SetField(panel, "hapticsToggle", hapticsToggle);
            SetField(panel, "restoreButton", restoreBtn);
            SetField(panel, "closeButton", closeBtn);
            SetField(panel, "slideDuration", 0.25f);

            return panel;
        }

        static Slider BuildSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = NewUIObj(name, parent);
            AsRT(go).anchorMin = AsRT(go).anchorMax = new Vector2(0f, 1f);
            AsRT(go).pivot = new Vector2(0f, 1f);
            AsRT(go).sizeDelta = size;
            AsRT(go).anchoredPosition = pos;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Background
            var bg = NewUIObj("Background", go.transform);
            FullStretch(AsRT(bg));
            AddImage(bg, new Color(0.1f, 0.1f, 0.15f, 1f));

            // Fill area + fill
            var fa = NewUIObj("Fill Area", go.transform);
            AsRT(fa).anchorMin = new Vector2(0f, 0.25f);
            AsRT(fa).anchorMax = new Vector2(1f, 0.75f);
            AsRT(fa).offsetMin = new Vector2(5, 0);
            AsRT(fa).offsetMax = new Vector2(-15, 0);
            var fill = NewUIObj("Fill", fa.transform);
            FullStretch(AsRT(fill));
            AddImage(fill, ColCta);
            slider.fillRect = AsRT(fill);

            // Handle area + handle
            var ha = NewUIObj("Handle Slide Area", go.transform);
            AsRT(ha).anchorMin = new Vector2(0f, 0f); AsRT(ha).anchorMax = new Vector2(1f, 1f);
            AsRT(ha).offsetMin = new Vector2(10, 0); AsRT(ha).offsetMax = new Vector2(-10, 0);
            var handle = NewUIObj("Handle", ha.transform);
            AsRT(handle).anchorMin = new Vector2(0f, 0.5f);
            AsRT(handle).anchorMax = new Vector2(0f, 0.5f);
            AsRT(handle).pivot = new Vector2(0.5f, 0.5f);
            AsRT(handle).sizeDelta = new Vector2(20, 20);
            AddImage(handle, ColText);
            slider.handleRect = AsRT(handle);
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        static Toggle BuildToggle(Transform parent, string name, string labelText, Vector2 anchoredPos)
        {
            var go = NewUIObj(name, parent);
            AsRT(go).anchorMin = AsRT(go).anchorMax = new Vector2(0f, 1f);
            AsRT(go).pivot = new Vector2(0f, 1f);
            AsRT(go).sizeDelta = new Vector2(300, 36);
            AsRT(go).anchoredPosition = anchoredPos;
            var toggle = go.AddComponent<Toggle>();

            var bg = NewUIObj("Background", go.transform);
            AsRT(bg).anchorMin = AsRT(bg).anchorMax = new Vector2(1f, 0.5f);
            AsRT(bg).pivot = new Vector2(1f, 0.5f);
            AsRT(bg).sizeDelta = new Vector2(32, 32);
            AsRT(bg).anchoredPosition = Vector2.zero;
            AddImage(bg, new Color(0.1f, 0.1f, 0.15f, 1f));

            var check = NewUIObj("Checkmark", bg.transform);
            FullStretch(AsRT(check), 4, 4, 4, 4);
            AddImage(check, ColGold);
            toggle.graphic = check.GetComponent<Image>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.isOn = true;

            var (lblGO, _) = NewText(go.transform, "Label", labelText, 20, ColText, TextAlignmentOptions.Left);
            AsRT(lblGO).anchorMin = new Vector2(0f, 0.5f);
            AsRT(lblGO).anchorMax = new Vector2(0f, 0.5f);
            AsRT(lblGO).pivot = new Vector2(0f, 0.5f);
            AsRT(lblGO).sizeDelta = new Vector2(200, 32);
            AsRT(lblGO).anchoredPosition = new Vector2(0, 0);

            return toggle;
        }

        // ============================================================================
        // LevelSelect scene
        // ============================================================================
        static void BuildLevelSelectScene()
        {
            var scene = EditorSceneManager.OpenScene(LevelSelectScenePath, OpenSceneMode.Single);
            ClearScenes_PhaseUIRoots(scene);

            var canvas = EnsureCanvas(scene, out _);
            var canvasT = canvas.transform;

            // Background
            var bg = NewUIObj("Background", canvasT);
            FullStretch(AsRT(bg));
            AddImage(bg, ColBg);

            var safe = BuildSafeRoot(canvasT);

            // Header
            var (headerGO, _) = NewText(safe.transform, "Header", "SELECT LEVEL", 32, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(headerGO), new Vector2(360, 50), new Vector2(0, -30));

            // Back button (top-left)
            var (backGO, backBtn, _) = NewButton(safe.transform, "BackButton", "< BACK", ColAccent, ColText, 20);
            AsRT(backGO).anchorMin = AsRT(backGO).anchorMax = new Vector2(0f, 1f);
            AsRT(backGO).pivot = new Vector2(0f, 1f);
            AsRT(backGO).sizeDelta = new Vector2(96, 44);
            AsRT(backGO).anchoredPosition = new Vector2(16, -20);

            // Horizontal scroll view with 6 cards.
            var scrollGO = NewUIObj("ScrollView", safe.transform);
            FullStretch(AsRT(scrollGO), 0, 0, 100, 60);
            AddImage(scrollGO, new Color(0, 0, 0, 0.15f));
            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal = true;
            sr.vertical = false;

            var viewport = NewUIObj("Viewport", scrollGO.transform);
            FullStretch(AsRT(viewport));
            AddImage(viewport, new Color(0, 0, 0, 0.01f));
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            sr.viewport = AsRT(viewport);

            var content = NewUIObj("Content", viewport.transform);
            AsRT(content).anchorMin = new Vector2(0f, 0.5f);
            AsRT(content).anchorMax = new Vector2(0f, 0.5f);
            AsRT(content).pivot = new Vector2(0f, 0.5f);
            AsRT(content).sizeDelta = new Vector2(6 * 260 + 40, 460);
            AsRT(content).anchoredPosition = new Vector2(20, 0);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            sr.content = AsRT(content);

            for (int i = 0; i < 6; i++)
            {
                BuildLevelCard(content.transform, levelIndex: i + 1, unlocked: (i == 0));
            }

            // LevelSelectUI controller on canvas.
            var ctrlGO = new GameObject("LevelSelectUI", typeof(LevelSelectUI));
            ctrlGO.transform.SetParent(canvasT.parent, true);
            var ls = ctrlGO.GetComponent<LevelSelectUI>();
            SetField(ls, "backButton", backBtn);
            SetField(ls, "mainMenuScene", "MainMenu");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase3Setup] LevelSelect scene built.");
        }

        static void BuildLevelCard(Transform parent, int levelIndex, bool unlocked)
        {
            var cardGO = NewUIObj($"LevelCard_{levelIndex}", parent);
            var le = cardGO.AddComponent<LayoutElement>();
            le.preferredWidth = 240f;
            le.preferredHeight = 420f;
            le.minWidth = 240f;
            le.minHeight = 420f;
            AddImage(cardGO, ColPanel);
            var card = cardGO.AddComponent<LevelCardUI>();

            // Dimmer
            var dimmer = NewUIObj("Dimmer", cardGO.transform);
            FullStretch(AsRT(dimmer));
            AddImage(dimmer, Color.white); // Tinted via CanvasGroup.alpha
            dimmer.GetComponent<Image>().color = Color.white;
            var cg = dimmer.AddComponent<CanvasGroup>();
            cg.alpha = unlocked ? 1f : 0.4f;
            cg.blocksRaycasts = false;

            // Title
            var (titleGO, titleTMP) = NewText(cardGO.transform, "Title", $"LEVEL {levelIndex}", 26, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(titleGO), new Vector2(220, 40), new Vector2(0, -20));

            var (subtitleGO, subTMP) = NewText(cardGO.transform, "Subtitle", unlocked ? "Ready" : "Locked", 16, ColTextDim, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(subtitleGO), new Vector2(220, 24), new Vector2(0, -60));

            // Stars row
            var starsRow = NewUIObj("Stars", cardGO.transform);
            AnchorTopCenter(AsRT(starsRow), new Vector2(180, 50), new Vector2(0, -100));
            var rowHLG = starsRow.AddComponent<HorizontalLayoutGroup>();
            rowHLG.childAlignment = TextAnchor.MiddleCenter;
            rowHLG.spacing = 10f;
            rowHLG.childForceExpandHeight = false;
            rowHLG.childForceExpandWidth = false;

            var stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                var sgo = NewUIObj($"Star_{s}", starsRow.transform);
                var le2 = sgo.AddComponent<LayoutElement>();
                le2.preferredWidth = 44f; le2.preferredHeight = 44f;
                var simg = sgo.AddComponent<Image>();
                simg.sprite = sprStar;
                simg.color = ColStarUnearned;
                stars[s] = simg;
            }

            // Play button / lock overlay
            var (playGO, playBtn, _) = NewButton(cardGO.transform, "PlayButton", unlocked ? "PLAY" : "LOCKED", unlocked ? ColCta : ColAccent, ColText, 22);
            AnchorBottomCenter(AsRT(playGO), new Vector2(200, 56), new Vector2(0, 30));
            playBtn.interactable = unlocked;

            var lockOverlay = NewUIObj("LockOverlay", cardGO.transform);
            FullStretch(AsRT(lockOverlay));
            AddImage(lockOverlay, ColDim50);
            var lockIcon = NewUIObj("LockIcon", lockOverlay.transform);
            AnchorCenter(AsRT(lockIcon), new Vector2(90, 90));
            var li = lockIcon.AddComponent<Image>();
            li.sprite = sprLock;
            li.color = ColText;
            lockOverlay.SetActive(!unlocked);

            SetField(card, "prefsStarsKey", $"level{levelIndex}_stars");
            SetField(card, "sceneToLoad", levelIndex == 1 ? "Level_01" : $"Level_{levelIndex:00}");
            SetField(card, "isUnlocked", unlocked);
            SetField(card, "titleText", titleTMP);
            SetField(card, "subtitleText", subTMP);
            SetArrayField(card, "starImages", stars);
            SetField(card, "playButton", playBtn);
            SetField(card, "lockOverlay", lockOverlay);
            SetField(card, "lockedDimmerGroup", cg);
            SetField(card, "starEarnedColor", ColGold);
            SetField(card, "starUnearnedColor", ColStarUnearned);
        }

        // ============================================================================
        // Level_01 UI
        // ============================================================================
        static void BuildLevel01UI()
        {
            var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            ClearScenes_PhaseUIRoots(scene);

            var canvas = EnsureCanvas(scene, out _);
            var canvasT = canvas.transform;

            var safe = BuildSafeRoot(canvasT);

            // ---- Top bar ----
            var topBar = NewUIObj("TopBar", safe.transform);
            AsRT(topBar).anchorMin = new Vector2(0f, 1f);
            AsRT(topBar).anchorMax = new Vector2(1f, 1f);
            AsRT(topBar).pivot = new Vector2(0.5f, 1f);
            AsRT(topBar).sizeDelta = new Vector2(0, 72);
            AsRT(topBar).anchoredPosition = new Vector2(0, 0);
            AddImage(topBar, new Color(ColPanel.r, ColPanel.g, ColPanel.b, 0.85f));

            var (hpGO, hpTMP) = NewText(topBar.transform, "HP", "20", 28, ColText, TextAlignmentOptions.Left, true);
            AsRT(hpGO).anchorMin = new Vector2(0f, 0.5f); AsRT(hpGO).anchorMax = new Vector2(0f, 0.5f);
            AsRT(hpGO).pivot = new Vector2(0f, 0.5f);
            AsRT(hpGO).sizeDelta = new Vector2(90, 50);
            AsRT(hpGO).anchoredPosition = new Vector2(20, 0);
            var (hpLblGO, _) = NewText(topBar.transform, "HPLabel", "HP", 12, ColTextDim, TextAlignmentOptions.Left);
            AsRT(hpLblGO).anchorMin = new Vector2(0f, 0.5f); AsRT(hpLblGO).anchorMax = new Vector2(0f, 0.5f);
            AsRT(hpLblGO).pivot = new Vector2(0f, 0.5f);
            AsRT(hpLblGO).sizeDelta = new Vector2(40, 14);
            AsRT(hpLblGO).anchoredPosition = new Vector2(20, -22);

            var (waveGO, waveTMP) = NewText(topBar.transform, "Wave", "WAVE 1 / 10", 18, ColText, TextAlignmentOptions.Center, true);
            AsRT(waveGO).anchorMin = AsRT(waveGO).anchorMax = new Vector2(0.5f, 0.5f);
            AsRT(waveGO).pivot = new Vector2(0.5f, 0.5f);
            AsRT(waveGO).sizeDelta = new Vector2(200, 40);

            var (softGO, softTMP) = NewText(topBar.transform, "SoftCurrency", "150", 24, ColGold, TextAlignmentOptions.Right, true);
            AsRT(softGO).anchorMin = new Vector2(1f, 0.5f); AsRT(softGO).anchorMax = new Vector2(1f, 0.5f);
            AsRT(softGO).pivot = new Vector2(1f, 0.5f);
            AsRT(softGO).sizeDelta = new Vector2(110, 40);
            AsRT(softGO).anchoredPosition = new Vector2(-20, 8);

            var (hardGO, hardTMP) = NewText(topBar.transform, "HardCurrency", "0", 14, new Color32(0x88, 0xCC, 0xFF, 0xFF), TextAlignmentOptions.Right);
            AsRT(hardGO).anchorMin = new Vector2(1f, 0.5f); AsRT(hardGO).anchorMax = new Vector2(1f, 0.5f);
            AsRT(hardGO).pivot = new Vector2(1f, 0.5f);
            AsRT(hardGO).sizeDelta = new Vector2(100, 24);
            AsRT(hardGO).anchoredPosition = new Vector2(-20, -18);

            // ---- Pause button (top-right, above top bar) ----
            var (pauseGO, pauseBtn, _) = NewButton(safe.transform, "PauseButton", "||", ColAccent, ColText, 22);
            AsRT(pauseGO).anchorMin = AsRT(pauseGO).anchorMax = new Vector2(1f, 1f);
            AsRT(pauseGO).pivot = new Vector2(1f, 1f);
            AsRT(pauseGO).sizeDelta = new Vector2(50, 50);
            AsRT(pauseGO).anchoredPosition = new Vector2(-10, -90);

            // ---- Tower selection bottom bar (4 tower cards horizontally) ----
            var towerBar = NewUIObj("TowerSelectionPanel", safe.transform);
            AsRT(towerBar).anchorMin = new Vector2(0f, 0f);
            AsRT(towerBar).anchorMax = new Vector2(1f, 0f);
            AsRT(towerBar).pivot = new Vector2(0.5f, 0f);
            AsRT(towerBar).sizeDelta = new Vector2(0, 130);
            AsRT(towerBar).anchoredPosition = new Vector2(0, 0);
            AddImage(towerBar, new Color(ColPanel.r, ColPanel.g, ColPanel.b, 0.85f));

            var towerRow = NewUIObj("Row", towerBar.transform);
            FullStretch(AsRT(towerRow), 8, 8, 8, 8);
            var rowLayout = towerRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            string[] towerPrefabNames = { "Tower_Basic", "Tower_Sniper", "Tower_Slow", "Tower_Area" };
            var towerCards = new List<TowerCardUI>();
            foreach (var tname in towerPrefabNames)
            {
                var td = AssetDatabase.LoadAssetAtPath<TowerData>($"{TowerDataFolder}/{tname}_Data.asset");
                towerCards.Add(BuildTowerCard(towerRow.transform, tname, td));
            }

            // ---- Control buttons (just above the tower bar): Start Wave + Speed ----
            var (startGO, startBtn, _) = NewButton(safe.transform, "StartWaveButton", "START WAVE", ColCta, ColText, 22);
            AsRT(startGO).anchorMin = new Vector2(0.5f, 0f);
            AsRT(startGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(startGO).pivot = new Vector2(0.5f, 0f);
            AsRT(startGO).sizeDelta = new Vector2(220, 52);
            AsRT(startGO).anchoredPosition = new Vector2(-60, 140);

            var (spdGO, spdBtn, spdLbl) = NewButton(safe.transform, "SpeedButton", "1x", ColAccent, ColText, 22);
            AsRT(spdGO).anchorMin = new Vector2(1f, 0f); AsRT(spdGO).anchorMax = new Vector2(1f, 0f);
            AsRT(spdGO).pivot = new Vector2(1f, 0f);
            AsRT(spdGO).sizeDelta = new Vector2(60, 52);
            AsRT(spdGO).anchoredPosition = new Vector2(-16, 140);

            // ---- Selected tower panel ----
            var selectedPanel = NewUIObj("SelectedTowerPanel", safe.transform);
            AsRT(selectedPanel).anchorMin = new Vector2(0.5f, 0f);
            AsRT(selectedPanel).anchorMax = new Vector2(0.5f, 0f);
            AsRT(selectedPanel).pivot = new Vector2(0.5f, 0f);
            AsRT(selectedPanel).sizeDelta = new Vector2(340, 200);
            AsRT(selectedPanel).anchoredPosition = new Vector2(0, 140);
            AddImage(selectedPanel, ColPanel);

            var (tNameGO, tNameTMP) = NewText(selectedPanel.transform, "Name", "Tower Name", 22, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(tNameGO), new Vector2(300, 34), new Vector2(0, -12));
            var (tStatsGO, tStatsTMP) = NewText(selectedPanel.transform, "Stats", "DMG 10   RANGE 3.0   RATE 1.0/s", 16, ColTextDim, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(tStatsGO), new Vector2(320, 24), new Vector2(0, -50));

            var (upgGO, upgBtn, upgLbl) = NewButton(selectedPanel.transform, "UpgradeButton", "Upgrade", ColCta, ColText, 18);
            AsRT(upgGO).anchorMin = new Vector2(0f, 0f); AsRT(upgGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(upgGO).pivot = new Vector2(0f, 0f);
            AsRT(upgGO).sizeDelta = new Vector2(-8, 48);
            AsRT(upgGO).offsetMin = new Vector2(12, 12); AsRT(upgGO).offsetMax = new Vector2(-4, 60);

            var (sellGO, sellBtn, sellLbl) = NewButton(selectedPanel.transform, "SellButton", "Sell", ColAccent, ColText, 18);
            AsRT(sellGO).anchorMin = new Vector2(0.5f, 0f); AsRT(sellGO).anchorMax = new Vector2(1f, 0f);
            AsRT(sellGO).pivot = new Vector2(0f, 0f);
            AsRT(sellGO).offsetMin = new Vector2(4, 12); AsRT(sellGO).offsetMax = new Vector2(-12, 60);

            var (closeGO, closeBtn, _) = NewButton(selectedPanel.transform, "CloseButton", "X", new Color(0.2f, 0.2f, 0.25f, 0.8f), ColText, 18);
            AsRT(closeGO).anchorMin = AsRT(closeGO).anchorMax = new Vector2(1f, 1f);
            AsRT(closeGO).pivot = new Vector2(1f, 1f);
            AsRT(closeGO).sizeDelta = new Vector2(36, 36);
            AsRT(closeGO).anchoredPosition = new Vector2(-6, -6);

            // Backdrop (full screen tap-to-close, behind panel).
            // The backdrop is a SIBLING of selectedPanel (so it can cover the
            // whole canvas, not just the 340x200 panel rect) and lives at
            // first-sibling so it draws BEHIND the panel. Critical: it must
            // start INACTIVE — its full-stretch transparent Image with
            // raycastTarget=true would otherwise silently swallow every click
            // on the play area below, breaking tower placement. UIManager
            // toggles backdrop active state together with the panel.
            var backdrop = NewUIObj("TowerPanelBackdrop", safe.transform);
            FullStretch(AsRT(backdrop));
            AddImage(backdrop, new Color(0, 0, 0, 0.01f));
            var backdropBtn = backdrop.AddComponent<Button>();
            backdrop.transform.SetAsFirstSibling();
            backdrop.SetActive(false);

            selectedPanel.SetActive(false);

            // ---- Wave Complete toast ----
            var waveToast = NewUIObj("WaveCompleteToast", safe.transform);
            AnchorCenter(AsRT(waveToast), new Vector2(300, 64));
            AsRT(waveToast).anchoredPosition = new Vector2(0, 180);
            AddImage(waveToast, ColPanel);
            var waveToastCG = waveToast.AddComponent<CanvasGroup>();
            var (waveToastGO, waveToastTMP) = NewText(waveToast.transform, "Text", "Wave 1 Complete!", 22, ColGold, TextAlignmentOptions.Center, true);
            FullStretch(AsRT(waveToastGO));
            waveToast.SetActive(false);

            // ---- Pause overlay ----
            var pauseOverlay = NewUIObj("PauseOverlay", canvasT);
            FullStretch(AsRT(pauseOverlay));
            AddImage(pauseOverlay, ColDim70);
            var pauseCG = pauseOverlay.AddComponent<CanvasGroup>();

            var pausePanel = NewUIObj("PausePanel", pauseOverlay.transform);
            AnchorCenter(AsRT(pausePanel), new Vector2(320, 420));
            AddImage(pausePanel, ColPanel);

            var (pauseTitleGO, _) = NewText(pausePanel.transform, "Title", "PAUSED", 28, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(pauseTitleGO), new Vector2(280, 50), new Vector2(0, -20));

            var (resumeGO, resumeBtn, _) = NewButton(pausePanel.transform, "ResumeButton", "RESUME", ColCta, ColText, 22);
            AnchorTopCenter(AsRT(resumeGO), new Vector2(240, 56), new Vector2(0, -90));
            var (restartGO, restartBtn, _) = NewButton(pausePanel.transform, "RestartButton", "RESTART", ColAccent, ColText, 22);
            AnchorTopCenter(AsRT(restartGO), new Vector2(240, 56), new Vector2(0, -160));
            var (mmGO, mmBtn, _) = NewButton(pausePanel.transform, "MainMenuButton", "MAIN MENU", ColAccent, ColText, 22);
            AnchorTopCenter(AsRT(mmGO), new Vector2(240, 56), new Vector2(0, -230));

            // Confirm-restart overlay inside the pause panel.
            var confirmRestart = NewUIObj("ConfirmRestart", pauseOverlay.transform);
            FullStretch(AsRT(confirmRestart));
            AddImage(confirmRestart, ColDim70);
            var confirmPanel = NewUIObj("Panel", confirmRestart.transform);
            AnchorCenter(AsRT(confirmPanel), new Vector2(280, 180));
            AddImage(confirmPanel, ColPanel);
            var (confirmTitleGO, _) = NewText(confirmPanel.transform, "Title", "Restart run?", 22, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(confirmTitleGO), new Vector2(260, 40), new Vector2(0, -20));
            var (cYesGO, cYesBtn, _) = NewButton(confirmPanel.transform, "YesButton", "YES", ColCta, ColText, 20);
            AsRT(cYesGO).anchorMin = new Vector2(0f, 0f); AsRT(cYesGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(cYesGO).pivot = new Vector2(0f, 0f);
            AsRT(cYesGO).offsetMin = new Vector2(12, 12); AsRT(cYesGO).offsetMax = new Vector2(-4, 60);
            var (cNoGO, cNoBtn, _) = NewButton(confirmPanel.transform, "NoButton", "NO", ColAccent, ColText, 20);
            AsRT(cNoGO).anchorMin = new Vector2(0.5f, 0f); AsRT(cNoGO).anchorMax = new Vector2(1f, 0f);
            AsRT(cNoGO).pivot = new Vector2(0f, 0f);
            AsRT(cNoGO).offsetMin = new Vector2(4, 12); AsRT(cNoGO).offsetMax = new Vector2(-12, 60);
            confirmRestart.SetActive(false);

            // Inline settings panel (shared prefab-style, built fresh).
            // Disable the OUTER SettingsOverlay (dim full-stretch) — disabling only the inner
            // SettingsPanel leaves the dim image active and silently blocks raycasts to
            // the pause buttons (Resume/Restart/MainMenu). Reference: inlineSettings is the
            // SettingsPanel component on the inner panelGO; its parent is the dim overlay.
            var inlineSettings = BuildSettingsPanel(pauseOverlay.transform);
            inlineSettings.transform.parent.gameObject.SetActive(false);

            pauseOverlay.SetActive(false);

            // ---- Game Over panel ----
            var gameOver = NewUIObj("GameOverPanel", canvasT);
            FullStretch(AsRT(gameOver));
            AddImage(gameOver, ColDim70);

            var goInner = NewUIObj("Panel", gameOver.transform);
            AnchorCenter(AsRT(goInner), new Vector2(320, 340));
            AddImage(goInner, ColPanel);

            var (goTitleGO, _) = NewText(goInner.transform, "Title", "DEFEAT", 36, ColCta, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(goTitleGO), new Vector2(300, 50), new Vector2(0, -20));
            var (goWaveGO, goWaveTMP) = NewText(goInner.transform, "WaveText", "You reached Wave 1", 18, ColText, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(goWaveGO), new Vector2(300, 30), new Vector2(0, -80));

            var (retryGO, retryBtn, _) = NewButton(goInner.transform, "RetryButton", "RETRY", ColCta, ColText, 22);
            AnchorTopCenter(AsRT(retryGO), new Vector2(240, 56), new Vector2(0, -150));
            var (goMenuGO, goMenuBtn, _) = NewButton(goInner.transform, "MainMenuButton", "MAIN MENU", ColAccent, ColText, 22);
            AnchorTopCenter(AsRT(goMenuGO), new Vector2(240, 56), new Vector2(0, -220));

            // Rewarded-ad continue prompt (separate overlay)
            var continueAd = NewUIObj("ContinueAdPanel", canvasT);
            AnchorCenter(AsRT(continueAd), new Vector2(320, 180));
            AddImage(continueAd, ColPanel);
            var (caTitleGO, _) = NewText(continueAd.transform, "Title", "Watch ad to continue?", 20, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(caTitleGO), new Vector2(300, 34), new Vector2(0, -14));
            var (caWatchGO, caWatchBtn, _) = NewButton(continueAd.transform, "WatchButton", "WATCH AD", ColCta, ColText, 20);
            AsRT(caWatchGO).anchorMin = new Vector2(0f, 0f); AsRT(caWatchGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(caWatchGO).pivot = new Vector2(0f, 0f);
            AsRT(caWatchGO).offsetMin = new Vector2(12, 12); AsRT(caWatchGO).offsetMax = new Vector2(-4, 60);
            var (caDeclineGO, caDeclineBtn, _) = NewButton(continueAd.transform, "DeclineButton", "NO THANKS", ColAccent, ColText, 18);
            AsRT(caDeclineGO).anchorMin = new Vector2(0.5f, 0f); AsRT(caDeclineGO).anchorMax = new Vector2(1f, 0f);
            AsRT(caDeclineGO).pivot = new Vector2(0f, 0f);
            AsRT(caDeclineGO).offsetMin = new Vector2(4, 12); AsRT(caDeclineGO).offsetMax = new Vector2(-12, 60);
            continueAd.SetActive(false);
            gameOver.SetActive(false);

            // ---- Victory panel ----
            var victory = NewUIObj("VictoryPanel", canvasT);
            FullStretch(AsRT(victory));
            AddImage(victory, ColDim70);

            var vInner = NewUIObj("Panel", victory.transform);
            AnchorCenter(AsRT(vInner), new Vector2(340, 500));
            AddImage(vInner, ColPanel);

            var (vTitleGO, _) = NewText(vInner.transform, "Title", "VICTORY!", 40, ColGold, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(vTitleGO), new Vector2(300, 60), new Vector2(0, -20));

            // Stars row
            var vStarsRow = NewUIObj("StarsRow", vInner.transform);
            AnchorTopCenter(AsRT(vStarsRow), new Vector2(240, 80), new Vector2(0, -100));
            var vrLayout = vStarsRow.AddComponent<HorizontalLayoutGroup>();
            vrLayout.spacing = 20f;
            vrLayout.childAlignment = TextAnchor.MiddleCenter;
            vrLayout.childForceExpandWidth = false;
            vrLayout.childForceExpandHeight = false;
            var victoryStars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                var sgo = NewUIObj($"Star_{s}", vStarsRow.transform);
                var le = sgo.AddComponent<LayoutElement>();
                le.preferredWidth = 60f; le.preferredHeight = 60f;
                var simg = sgo.AddComponent<Image>();
                simg.sprite = sprStar;
                simg.color = ColGold;
                victoryStars[s] = simg;
            }

            var (vEnemiesGO, vEnemiesTMP) = NewText(vInner.transform, "Enemies", "Enemies defeated: 0", 18, ColText, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(vEnemiesGO), new Vector2(300, 30), new Vector2(0, -210));
            var (vGoldGO, vGoldTMP) = NewText(vInner.transform, "Gold", "Gold earned: 0", 18, ColText, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(vGoldGO), new Vector2(300, 30), new Vector2(0, -240));

            var (vMenuGO, vMenuBtn, _) = NewButton(vInner.transform, "MainMenuButton", "MAIN MENU", ColAccent, ColText, 20);
            AsRT(vMenuGO).anchorMin = new Vector2(0f, 0f); AsRT(vMenuGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(vMenuGO).pivot = new Vector2(0f, 0f);
            AsRT(vMenuGO).offsetMin = new Vector2(16, 20); AsRT(vMenuGO).offsetMax = new Vector2(-4, 76);
            var (vNextGO, vNextBtn, _) = NewButton(vInner.transform, "NextLevelButton", "NEXT LEVEL", ColCta, ColText, 20);
            AsRT(vNextGO).anchorMin = new Vector2(0.5f, 0f); AsRT(vNextGO).anchorMax = new Vector2(1f, 0f);
            AsRT(vNextGO).pivot = new Vector2(0f, 0f);
            AsRT(vNextGO).offsetMin = new Vector2(4, 20); AsRT(vNextGO).offsetMax = new Vector2(-16, 76);

            // Coming-soon toast
            var comingSoon = NewUIObj("ComingSoonToast", vInner.transform);
            AnchorCenter(AsRT(comingSoon), new Vector2(240, 50));
            AsRT(comingSoon).anchoredPosition = new Vector2(0, 110);
            AddImage(comingSoon, ColAccent);
            comingSoon.AddComponent<CanvasGroup>();
            var (csTxtGO, _) = NewText(comingSoon.transform, "Text", "Coming Soon!", 18, ColText, TextAlignmentOptions.Center, true);
            FullStretch(AsRT(csTxtGO));
            comingSoon.SetActive(false);

            // Remove-ads prompt inside the victory panel
            var removeAdsPrompt = NewUIObj("RemoveAdsPrompt", vInner.transform);
            AnchorBottomCenter(AsRT(removeAdsPrompt), new Vector2(320, 110), new Vector2(0, 96));
            AddImage(removeAdsPrompt, ColAccent);
            var (raTitleGO, _) = NewText(removeAdsPrompt.transform, "Title", "Enjoying the game? Remove ads $2.99", 14, ColText, TextAlignmentOptions.Center);
            AnchorTopCenter(AsRT(raTitleGO), new Vector2(300, 30), new Vector2(0, -10));
            var (raBuyGO, raBuyBtn, _) = NewButton(removeAdsPrompt.transform, "BuyButton", "BUY", ColCta, ColText, 16);
            AsRT(raBuyGO).anchorMin = new Vector2(0f, 0f); AsRT(raBuyGO).anchorMax = new Vector2(0.5f, 0f);
            AsRT(raBuyGO).pivot = new Vector2(0f, 0f);
            AsRT(raBuyGO).offsetMin = new Vector2(12, 10); AsRT(raBuyGO).offsetMax = new Vector2(-4, 50);
            var (raDeclineGO, raDeclineBtn, _) = NewButton(removeAdsPrompt.transform, "DeclineButton", "LATER", ColPanel, ColText, 16);
            AsRT(raDeclineGO).anchorMin = new Vector2(0.5f, 0f); AsRT(raDeclineGO).anchorMax = new Vector2(1f, 0f);
            AsRT(raDeclineGO).pivot = new Vector2(0f, 0f);
            AsRT(raDeclineGO).offsetMin = new Vector2(4, 10); AsRT(raDeclineGO).offsetMax = new Vector2(-12, 50);
            removeAdsPrompt.SetActive(false);

            victory.SetActive(false);

            // ================================================================
            // Wire controllers
            // ================================================================

            // UIManager on Canvas.
            var ui = canvas.gameObject.GetComponent<UIManager>() ?? canvas.gameObject.AddComponent<UIManager>();
            SetField(ui, "hpText", hpTMP);
            SetField(ui, "waveText", waveTMP);
            SetField(ui, "softCurrencyText", softTMP);
            SetField(ui, "hardCurrencyText", hardTMP);
            SetField(ui, "selectedTowerPanel", selectedPanel);
            SetField(ui, "towerNameText", tNameTMP);
            SetField(ui, "towerStatsText", tStatsTMP);
            SetField(ui, "upgradeButton", upgBtn);
            SetField(ui, "upgradeButtonText", upgLbl);
            SetField(ui, "sellButton", sellBtn);
            SetField(ui, "sellButtonText", sellLbl);
            SetField(ui, "closeTowerPanelButton", closeBtn);
            SetField(ui, "towerPanelBackdropButton", backdropBtn);
            SetField(ui, "pauseOverlay", pauseOverlay);
            SetField(ui, "pauseCanvasGroup", pauseCG);
            SetField(ui, "gameOverPanel", AsRT(gameOver));
            SetField(ui, "gameOverWaveText", goWaveTMP);
            SetField(ui, "continueAdPanel", continueAd);
            SetField(ui, "continueAdWatchButton", caWatchBtn);
            SetField(ui, "continueAdDeclineButton", caDeclineBtn);
            SetField(ui, "victoryPanel", victory);
            SetArrayField(ui, "victoryStars", victoryStars);
            SetField(ui, "victoryEnemiesText", vEnemiesTMP);
            SetField(ui, "victoryGoldText", vGoldTMP);
            SetField(ui, "removeAdsPrompt", removeAdsPrompt);
            SetField(ui, "waveCompleteToast", waveToast);
            SetField(ui, "waveCompleteText", waveToastTMP);
            SetField(ui, "waveCompleteCanvasGroup", waveToastCG);
            SetArrayField(ui, "towerCards", towerCards);
            SetField(ui, "startWaveButton", startBtn);
            SetField(ui, "startWaveButtonRect", AsRT(startGO));
            SetField(ui, "speedButton", spdBtn);
            SetField(ui, "speedButtonText", spdLbl);
            SetField(ui, "pauseButton", pauseBtn);

            // PauseUI on the overlay.
            var pauseCtrl = pauseOverlay.GetComponent<PauseUI>() ?? pauseOverlay.AddComponent<PauseUI>();
            SetField(pauseCtrl, "resumeButton", resumeBtn);
            SetField(pauseCtrl, "restartButton", restartBtn);
            SetField(pauseCtrl, "mainMenuButton", mmBtn);
            SetField(pauseCtrl, "confirmRestartYesButton", cYesBtn);
            SetField(pauseCtrl, "confirmRestartNoButton", cNoBtn);
            SetField(pauseCtrl, "confirmRestartOverlay", confirmRestart);
            SetField(pauseCtrl, "inlineSettingsPanel", inlineSettings);

            // GameOverUI on the panel.
            var goCtrl = gameOver.GetComponent<GameOverUI>() ?? gameOver.AddComponent<GameOverUI>();
            SetField(goCtrl, "retryButton", retryBtn);
            SetField(goCtrl, "mainMenuButton", goMenuBtn);

            // VictoryUI on the panel.
            var vCtrl = victory.GetComponent<VictoryUI>() ?? victory.AddComponent<VictoryUI>();
            SetField(vCtrl, "mainMenuButton", vMenuBtn);
            SetField(vCtrl, "nextLevelButton", vNextBtn);
            SetField(vCtrl, "comingSoonToast", comingSoon);
            SetField(vCtrl, "removeAdsPurchaseButton", raBuyBtn);
            SetField(vCtrl, "removeAdsDeclineButton", raDeclineBtn);
            SetField(vCtrl, "removeAdsPrompt", removeAdsPrompt);

            // ---- Scene-level systems: TouchInputHandler, AdManager, IAPManager ----
            EnsureSceneSystem<TouchInputHandler>("TouchInputHandler");
            EnsureSceneSystem<AdManager>("AdManager");
            EnsureSceneSystem<IAPManager>("IAPManager");
            EnsureSceneSystem<GameManager>("GameManager");

            // Point TouchInputHandler at the main camera.
            var tih = GameObject.FindFirstObjectByType<TouchInputHandler>();
            if (tih != null)
            {
                var cam = Camera.main;
                if (cam == null) cam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
                SetField(tih, "mainCamera", cam);
                SetField(tih, "blockTapsOverUI", true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase3Setup] Level_01 UI built.");
        }

        static TowerCardUI BuildTowerCard(Transform parent, string prefabName, TowerData data)
        {
            var go = NewUIObj("TowerCard_" + prefabName, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80f;
            le.preferredHeight = 110f;
            le.minWidth = 64f;
            AddImage(go, ColAccent);
            var card = go.AddComponent<TowerCardUI>();
            var btn = go.AddComponent<Button>();

            // Icon
            var iconGO = NewUIObj("Icon", go.transform);
            AnchorTopCenter(AsRT(iconGO), new Vector2(52, 52), new Vector2(0, -6));
            var icon = iconGO.AddComponent<Image>();
            if (data != null) icon.sprite = data.icon;

            // Name
            var (nameGO, nameTMP) = NewText(go.transform, "Name", data != null ? data.towerName : prefabName, 12, ColText, TextAlignmentOptions.Center, true);
            AnchorTopCenter(AsRT(nameGO), new Vector2(80, 16), new Vector2(0, -60));

            // Cost
            var (costGO, costTMP) = NewText(go.transform, "Cost", data != null ? data.cost.ToString() : "0", 14, ColGold, TextAlignmentOptions.Center, true);
            AnchorBottomCenter(AsRT(costGO), new Vector2(80, 20), new Vector2(0, 8));

            // Disabled overlay
            var disabled = NewUIObj("DisabledOverlay", go.transform);
            FullStretch(AsRT(disabled));
            AddImage(disabled, ColDisabledOverlay);
            disabled.SetActive(false);

            // Selected highlight
            var selected = NewUIObj("SelectedHighlight", go.transform);
            FullStretch(AsRT(selected), -3, -3, -3, -3);
            var sImg = selected.AddComponent<Image>();
            sImg.color = ColGold;
            sImg.raycastTarget = false;
            selected.SetActive(false);
            selected.transform.SetAsFirstSibling();

            SetField(card, "towerData", data);
            SetField(card, "iconImage", icon);
            SetField(card, "nameText", nameTMP);
            SetField(card, "costText", costTMP);
            SetField(card, "button", btn);
            SetField(card, "disabledOverlay", disabled);
            SetField(card, "selectedHighlight", selected);

            return card;
        }

        static void EnsureSceneSystem<T>(string objName) where T : Component
        {
            var existing = GameObject.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null) return;
            var go = GameObject.Find(objName);
            if (go == null) go = new GameObject(objName);
            if (go.GetComponent<T>() == null) go.AddComponent<T>();
        }
    }
}
#endif
