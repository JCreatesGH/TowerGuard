#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TowerGuard.Core;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// One-click Phase 1 bring-up. Runs the Unity-Editor-only steps that couldn't be done
    /// through the file system alone: configures Player Settings for iOS, builds the three
    /// Phase 1 scenes in Assets/Scenes, and registers them in EditorBuildSettings.
    ///
    /// Every step is idempotent — safe to re-run. Each step is exposed as its own menu item
    /// under Tools/TowerGuard so you can re-run just one piece if something needs fixing.
    /// </summary>
    public static class TowerGuardPhase1Setup
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string MainMenuPath = ScenesDir + "/MainMenu.unity";
        private const string LevelSelectPath = ScenesDir + "/LevelSelect.unity";
        private const string Level01Path = ScenesDir + "/Level_01.unity";

        // #1A1A2E — the Phase 1 camera background color.
        private static readonly Color BgColor = new Color32(0x1A, 0x1A, 0x2E, 0xFF);

        // =============================================================================
        // Top-level: run everything.
        // =============================================================================

        [MenuItem("Tools/TowerGuard/Run All Phase 1 Setup", priority = 0)]
        public static void RunAll()
        {
            ConfigurePlayerSettings();
            BuildPhase1Scenes();
            RegisterScenesInBuildSettings();
            TrySwitchToIOS();
            Debug.Log("[TowerGuardPhase1Setup] Phase 1 setup complete. " +
                      "Remaining manual steps: import TMP Essentials popup (if it appears), " +
                      "import LeanTween from the Asset Store, and press Play on Level_01.");
        }

        // =============================================================================
        // Step 3: Player Settings (iOS bundle id, version, orientation, IL2CPP, etc.)
        // =============================================================================

        [MenuItem("Tools/TowerGuard/01 Configure Player Settings for iOS", priority = 10)]
        public static void ConfigurePlayerSettings()
        {
            // Cross-platform identity
            PlayerSettings.companyName = "TowerGuard";
            PlayerSettings.productName = "Tower Guard";
            PlayerSettings.bundleVersion = "1.0.0";

            // Default orientation — portrait only (all platforms honor this field)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // iOS-specific fields. Using NamedBuildTarget API so this works even when
            // iOS isn't the active platform (as long as iOS Build Support is installed).
            var iosNBT = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(iosNBT, "com.towerguard.game");
            PlayerSettings.SetScriptingBackend(iosNBT, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(iosNBT, ManagedStrippingLevel.Medium);
            PlayerSettings.SetArchitecture(iosNBT, (int)AppleMobileArchitecture.ARM64);

            // PlayerSettings.iOS.* properties are available whenever iOS Build Support is
            // installed, even when iOS isn't the active target. Wrap in try/catch so the
            // rest of Player Settings still applies if the module isn't installed yet.
            try
            {
                PlayerSettings.iOS.buildNumber = "1";
                PlayerSettings.iOS.targetOSVersionString = "16.0";
                PlayerSettings.iOS.requiresFullScreen = true;
                // Unity 6 replaced PlayerSettings.iOS.allowHTTPDownload with the
                // cross-platform InsecureHttpOption enum on PlayerSettings itself.
                PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TowerGuardPhase1Setup] PlayerSettings.iOS.* is unavailable (iOS module not installed?): " + e.Message);
            }

            // Quality: set iOS default quality level to "Medium" if that level exists.
            SetQualityLevelForIOS("Medium");

            // Persist settings so they survive an Editor restart.
            AssetDatabase.SaveAssets();
            Debug.Log("[TowerGuardPhase1Setup] Player Settings configured.");
        }

        private static void SetQualityLevelForIOS(string levelName)
        {
            string[] names = QualitySettings.names;
            int idx = System.Array.IndexOf(names, levelName);
            if (idx < 0)
            {
                Debug.LogWarning("[TowerGuardPhase1Setup] Quality level '" + levelName + "' not found; skipping.");
                return;
            }

            // PlayerSettings doesn't expose a public API to set per-platform default quality
            // at edit time in every Unity version. The stable approach is to set the current
            // level — Unity persists the last active quality level for each platform on first
            // open after a switch. For iOS specifically, Quality > iOS column > row checkmark
            // is what Players actually use; leave that for the Editor Quality window to pick up.
            QualitySettings.SetQualityLevel(idx, applyExpensiveChanges: false);
        }

        // =============================================================================
        // Step 3 (platform switch)
        // =============================================================================

        [MenuItem("Tools/TowerGuard/03 Switch Active Platform to iOS", priority = 30)]
        public static void TrySwitchToIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                Debug.Log("[TowerGuardPhase1Setup] Already on iOS target.");
                return;
            }

            bool switched = EditorUserBuildSettings.SwitchActiveBuildTargetAsync(
                BuildTargetGroup.iOS, BuildTarget.iOS);
            if (!switched)
            {
                Debug.LogWarning(
                    "[TowerGuardPhase1Setup] Switch to iOS failed or is queued. " +
                    "This usually means iOS Build Support isn't installed yet. " +
                    "Install it from Unity Hub > Installs > ... > Add Modules > iOS Build Support, " +
                    "then re-run this menu item.");
            }
        }

        // =============================================================================
        // Step 6: Build the three scenes
        // =============================================================================

        [MenuItem("Tools/TowerGuard/02 Create Phase 1 Scenes", priority = 20)]
        public static void BuildPhase1Scenes()
        {
            // Save the currently open scene first so we don't lose anything.
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            EnsureFolder(ScenesDir);

            BuildMainMenu();
            BuildLevelSelect();
            BuildLevel01();

            AssetDatabase.Refresh();
            Debug.Log("[TowerGuardPhase1Setup] MainMenu, LevelSelect, Level_01 created in " + ScenesDir);
        }

        private static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddMainCamera(scene);

            // Managers that need to persist from MainMenu onward.
            CreateGameObjectWith<GameManager>("GameManager", scene);
            CreateGameObjectWith<AudioManager>("AudioManager", scene);

            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void BuildLevelSelect()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddMainCamera(scene);
            // No managers — GameManager + AudioManager persist via DontDestroyOnLoad.
            EditorSceneManager.SaveScene(scene, LevelSelectPath);
        }

        private static void BuildLevel01()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (orthographic, size 6, #1A1A2E background)
            var cameraGO = AddMainCamera(scene);
            var cam = cameraGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;

            // Managers.
            CreateGameObjectWith<GameManager>("GameManager", scene);
            CreateGameObjectWith<WaveManager>("WaveManager", scene);
            CreateGameObjectWith<AudioManager>("AudioManager", scene);
            CreateGameObjectWith<PathManager>("PathManager", scene);

            // UICanvas — Screen Space Overlay, Scale With Screen Size, 390x844, match 0.5
            var uiCanvasGO = new GameObject("UICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(uiCanvasGO, scene);
            var canvas = uiCanvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = uiCanvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem so UI actually receives input. Use the new Input System's UI
            // module — the project template sets Active Input Handling to "Input System
            // Package", so StandaloneInputModule would throw on every tick.
            var esGO = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(esGO, scene);

            // Grid with three child Tilemaps.
            var gridGO = new GameObject("Grid", typeof(Grid));
            SceneManager.MoveGameObjectToScene(gridGO, scene);
            MakeTilemapChild(gridGO.transform, "Tilemap_Ground");
            MakeTilemapChild(gridGO.transform, "Tilemap_Path");
            MakeTilemapChild(gridGO.transform, "Tilemap_Obstacles");

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, Level01Path);
        }

        private static GameObject AddMainCamera(Scene scene)
        {
            var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraGO, scene);
            cameraGO.tag = "MainCamera";

            var cam = cameraGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgColor;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cameraGO.transform.position = new Vector3(0f, 0f, -10f);

            return cameraGO;
        }

        private static GameObject CreateGameObjectWith<T>(string name, Scene scene) where T : Component
        {
            var go = new GameObject(name, typeof(T));
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        private static void MakeTilemapChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
        }

        // =============================================================================
        // Build Settings — add the three scenes to File > Build Settings > Scenes In Build.
        // =============================================================================

        [MenuItem("Tools/TowerGuard/04 Register Scenes in Build Settings", priority = 40)]
        public static void RegisterScenesInBuildSettings()
        {
            string[] paths = { MainMenuPath, LevelSelectPath, Level01Path };
            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Deduplicate and keep any user-added scenes that aren't one of ours.
            var orderedScenes = new List<EditorBuildSettingsScene>();
            foreach (string p in paths)
            {
                if (!File.Exists(p))
                {
                    Debug.LogWarning("[TowerGuardPhase1Setup] Scene file missing, skipping Build Settings entry: " + p);
                    continue;
                }
                orderedScenes.Add(new EditorBuildSettingsScene(p, true));
            }

            foreach (var scene in existing)
            {
                if (System.Array.IndexOf(paths, scene.path) < 0)
                {
                    orderedScenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = orderedScenes.ToArray();
            Debug.Log("[TowerGuardPhase1Setup] Build Settings scene list updated: MainMenu (0), LevelSelect (1), Level_01 (2).");
        }

        // =============================================================================
        // Helpers
        // =============================================================================

        private static void EnsureFolder(string assetsRelativePath)
        {
            if (AssetDatabase.IsValidFolder(assetsRelativePath)) return;
            var parts = assetsRelativePath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
