#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TowerGuard.Core;
using TowerGuard.Enemies;
using TowerGuard.Towers;
using TowerGuard.Utils;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 2 asset + scene bring-up. Running "Run All Phase 2 Setup" creates every
    /// ScriptableObject, prefab, colored-tile asset, and scene object required for the
    /// Phase 2 verify step. Each step is idempotent — re-running safely overwrites / reuses.
    /// </summary>
    public static class TowerGuardPhase2Setup
    {
        // ----- Paths -----
        const string TilesFolder = "Assets/Tilemaps/Tiles";
        const string EnemyDataFolder = "Assets/ScriptableObjects/Enemies";
        const string TowerDataFolder = "Assets/ScriptableObjects/Towers";
        const string WaveDataFolder = "Assets/ScriptableObjects/Waves";
        const string EnemyPrefabFolder = "Assets/Prefabs/Enemies";
        const string TowerPrefabFolder = "Assets/Prefabs/Towers";
        const string ProjectilePrefabFolder = "Assets/Prefabs/Projectiles";
        const string Level01ScenePath = "Assets/Scenes/Level_01.unity";

        // Standard palette.
        static readonly Color GrassColor = new Color32(0x4C, 0xAF, 0x50, 0xFF);
        static readonly Color PathColor = new Color32(0x79, 0x55, 0x48, 0xFF);
        static readonly Color RockColor = new Color32(0x60, 0x7D, 0x8B, 0xFF);
        static readonly Color WaterColor = new Color32(0x15, 0x65, 0xC0, 0xFF);
        static readonly Color StartFlagColor = new Color32(0x4C, 0xAF, 0x50, 0xFF);
        static readonly Color EndFlagColor = new Color32(0xE5, 0x39, 0x35, 0xFF);

        // ----- Grid / path layout (22x14 cells, origin shifted so (0,0) cell = world (-11,-7)) -----
        const int GridWidth = 22;
        const int GridHeight = 14;
        static readonly Vector3 GridWorldOrigin = new Vector3(-11f, -7f, 0f);

        // 8 waypoints, 5 turns.
        static readonly Vector2Int[] WaypointCells = new Vector2Int[]
        {
            new Vector2Int(0, 7),
            new Vector2Int(4, 7),
            new Vector2Int(4, 2),
            new Vector2Int(10, 2),
            new Vector2Int(10, 11),
            new Vector2Int(17, 11),
            new Vector2Int(17, 5),
            new Vector2Int(21, 5),
        };

        [MenuItem("Tools/TowerGuard/Run All Phase 2 Setup", priority = 100)]
        public static void RunAll()
        {
            EnsureTag("Enemy");
            CreateTileAssets();
            CreateEnemyDataAndPrefabs();
            CreateProjectilePrefabs();
            CreateTowerDataAndPrefabs();
            CreateWaveData();
            BuildLevel01();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase2Setup] RunAll: complete.");
        }

        [MenuItem("Tools/TowerGuard/01 Ensure Enemy Tag", priority = 120)]
        public static void Step01_EnsureTag() => EnsureTag("Enemy");

        [MenuItem("Tools/TowerGuard/02 Create Tile Assets", priority = 121)]
        public static void Step02_CreateTiles() { CreateTileAssets(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/03 Create Enemy Data and Prefabs", priority = 122)]
        public static void Step03_Enemies() { CreateEnemyDataAndPrefabs(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/04 Create Projectile Prefabs", priority = 123)]
        public static void Step04_Projectiles() { CreateProjectilePrefabs(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/05 Create Tower Data and Prefabs", priority = 124)]
        public static void Step05_Towers() { CreateTowerDataAndPrefabs(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/06 Create Wave Data", priority = 125)]
        public static void Step06_Waves() { CreateWaveData(); AssetDatabase.SaveAssets(); }

        [MenuItem("Tools/TowerGuard/07 Build Level_01", priority = 126)]
        public static void Step07_Level() { BuildLevel01(); }

        // ============================================================================
        // Tag registration
        // ============================================================================
        static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[Phase2Setup] Added tag '{tag}'.");
        }

        // ============================================================================
        // Folder helpers
        // ============================================================================
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ============================================================================
        // Tile (colored) assets
        // ============================================================================
        static TileBase grassTile, pathTile, rockTile, waterTile;

        static void CreateTileAssets()
        {
            EnsureFolder(TilesFolder);
            grassTile = CreateOrGetColorTile("Grass", GrassColor);
            pathTile = CreateOrGetColorTile("Path", PathColor);
            rockTile = CreateOrGetColorTile("Rock", RockColor);
            waterTile = CreateOrGetColorTile("Water", WaterColor);
            Debug.Log("[Phase2Setup] Tile assets ready.");
        }

        static TileBase CreateOrGetColorTile(string name, Color color)
        {
            string tilePath = $"{TilesFolder}/{name}.asset";
            Tile existingTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            Sprite sprite = CreateOrGetColorSprite(name, color);
            if (existingTile != null)
            {
                existingTile.sprite = sprite;
                EditorUtility.SetDirty(existingTile);
                return existingTile;
            }
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, tilePath);
            return tile;
        }

        static Sprite CreateOrGetColorSprite(string name, Color color)
        {
            string texPath = $"{TilesFolder}/{name}.png";
            if (!File.Exists(texPath))
            {
                Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            }
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        }

        // ============================================================================
        // Enemy data + prefabs
        // ============================================================================
        struct EnemyDef
        {
            public string prefabName;
            public string enemyName;
            public int maxHP;
            public float speed;
            public int armor;
            public int soft;
            public int hardChance;
            public float scale;
            public string placeholderSpritePath;
        }

        static readonly EnemyDef[] EnemyDefs = new EnemyDef[]
        {
            new EnemyDef{ prefabName = "Enemy_Basic", enemyName = "Basic Enemy", maxHP = 60,   speed = 2.0f, armor = 0,  soft = 10,  hardChance = 0,  scale = 1f, placeholderSpritePath = "Assets/Sprites/Enemies/Enemy_Basic.png" },
            new EnemyDef{ prefabName = "Enemy_Fast",  enemyName = "Fast Enemy",  maxHP = 30,   speed = 4.5f, armor = 0,  soft = 15,  hardChance = 5,  scale = 1f, placeholderSpritePath = "Assets/Sprites/Enemies/Enemy_Fast.png" },
            new EnemyDef{ prefabName = "Enemy_Tank",  enemyName = "Tank Enemy",  maxHP = 300,  speed = 1.0f, armor = 8,  soft = 30,  hardChance = 15, scale = 1f, placeholderSpritePath = "Assets/Sprites/Enemies/Enemy_Tank.png" },
            new EnemyDef{ prefabName = "Enemy_Boss",  enemyName = "Boss Enemy",  maxHP = 1000, speed = 0.8f, armor = 15, soft = 100, hardChance = 50, scale = 2f, placeholderSpritePath = "Assets/Sprites/Enemies/Enemy_Boss.png" },
        };

        static Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();

        static void CreateEnemyDataAndPrefabs()
        {
            EnsureFolder(EnemyDataFolder);
            EnsureFolder(EnemyPrefabFolder);
            enemyPrefabs.Clear();

            foreach (var def in EnemyDefs)
            {
                // EnemyData SO
                string dataPath = $"{EnemyDataFolder}/{def.prefabName}_Data.asset";
                EnemyData dataAsset = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
                if (dataAsset == null)
                {
                    dataAsset = ScriptableObject.CreateInstance<EnemyData>();
                    AssetDatabase.CreateAsset(dataAsset, dataPath);
                }
                dataAsset.enemyName = def.enemyName;
                dataAsset.maxHP = def.maxHP;
                dataAsset.speed = def.speed;
                dataAsset.armor = def.armor;
                dataAsset.softCurrencyReward = def.soft;
                dataAsset.hardCurrencyChance = def.hardChance;
                EditorUtility.SetDirty(dataAsset);

                // Prefab
                string prefabPath = $"{EnemyPrefabFolder}/{def.prefabName}.prefab";
                GameObject go = new GameObject(def.prefabName);
                go.tag = "Enemy";
                go.transform.localScale = new Vector3(def.scale, def.scale, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.placeholderSpritePath);
                sr.sortingOrder = 2;

                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
                col.isTrigger = false;

                var enemy = go.AddComponent<EnemyBase>();
                SetPrivateField(enemy, "data", dataAsset);

                // World-space HP bar: Canvas (scale 0.01) + Slider.
                GameObject canvasGO = new GameObject("HPBar");
                canvasGO.transform.SetParent(go.transform, false);
                canvasGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 10;
                canvasGO.AddComponent<CanvasScaler>();

                // Slider root
                GameObject sliderGO = new GameObject("Slider");
                sliderGO.transform.SetParent(canvasGO.transform, false);
                var rect = sliderGO.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(80, 12);
                var slider = sliderGO.AddComponent<Slider>();
                slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
                slider.transition = Selectable.Transition.None;
                slider.interactable = false;

                // Background
                GameObject bgGO = new GameObject("Background");
                bgGO.transform.SetParent(sliderGO.transform, false);
                var bgRect = bgGO.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);

                // Fill area + Fill
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderGO.transform, false);
                var faRect = fillArea.AddComponent<RectTransform>();
                faRect.anchorMin = new Vector2(0f, 0f); faRect.anchorMax = new Vector2(1f, 1f);
                faRect.offsetMin = new Vector2(1, 1); faRect.offsetMax = new Vector2(-1, -1);

                GameObject fillGO = new GameObject("Fill");
                fillGO.transform.SetParent(fillArea.transform, false);
                var fRect = fillGO.AddComponent<RectTransform>();
                fRect.anchorMin = new Vector2(0f, 0f); fRect.anchorMax = new Vector2(1f, 1f);
                fRect.offsetMin = Vector2.zero; fRect.offsetMax = Vector2.zero;
                var fImg = fillGO.AddComponent<Image>();
                fImg.color = new Color(0.95f, 0.2f, 0.2f, 0.95f);

                slider.fillRect = fRect;
                SetPrivateField(enemy, "hpBar", slider);
                SetPrivateField(enemy, "hpBarRoot", canvasGO.transform);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                enemyPrefabs[def.prefabName] = prefab;
            }
            Debug.Log("[Phase2Setup] Enemy data + prefabs ready.");
        }

        // ============================================================================
        // Projectile prefabs
        // ============================================================================
        static Dictionary<string, GameObject> projectilePrefabs = new Dictionary<string, GameObject>();

        static void CreateProjectilePrefabs()
        {
            EnsureFolder(ProjectilePrefabFolder);
            projectilePrefabs.Clear();

            projectilePrefabs["Projectile_Bullet"] = BuildProjectilePrefab(
                "Projectile_Bullet", "Assets/Sprites/Towers/Projectile_Bullet.png", typeof(ProjectileBase));
            projectilePrefabs["Projectile_Laser"] = BuildProjectilePrefab(
                "Projectile_Laser", "Assets/Sprites/Towers/Projectile_Laser.png", typeof(ProjectileBase));
            projectilePrefabs["Projectile_Slow"] = BuildProjectilePrefab(
                "Projectile_Slow", "Assets/Sprites/Towers/Projectile_Bullet.png", typeof(SlowProjectile));
            projectilePrefabs["Projectile_AOE"] = BuildProjectilePrefab(
                "Projectile_AOE", "Assets/Sprites/Towers/Projectile_AOE.png", typeof(AOEProjectile));

            Debug.Log("[Phase2Setup] Projectile prefabs ready.");
        }

        static GameObject BuildProjectilePrefab(string name, string spritePath, System.Type scriptType)
        {
            string prefabPath = $"{ProjectilePrefabFolder}/{name}.prefab";
            GameObject go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            sr.sortingOrder = 4;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.isTrigger = true;

            go.AddComponent(scriptType);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ============================================================================
        // Tower data + prefabs
        // ============================================================================
        struct TowerDef
        {
            public string prefabName;
            public string towerName;
            public int cost, upgradeCost;
            public float dmg, upgDmg, range, upgRange, rate, upgRate, projSpeed;
            public string spritePath;
            public string projectileKey;
            public string description;
        }

        static readonly TowerDef[] TowerDefs = new TowerDef[]
        {
            new TowerDef{ prefabName = "Tower_Basic",  towerName = "Basic",  cost = 50,  upgradeCost = 40, dmg = 10, upgDmg = 18, range = 3f, upgRange = 3.5f, rate = 1.0f, upgRate = 1.3f, projSpeed = 10f, spritePath = "Assets/Sprites/Towers/Tower_Basic.png", projectileKey = "Projectile_Bullet", description = "Balanced single-target tower." },
            new TowerDef{ prefabName = "Tower_Sniper", towerName = "Sniper", cost = 100, upgradeCost = 70, dmg = 40, upgDmg = 70, range = 6f, upgRange = 8f,   rate = 0.4f, upgRate = 0.55f,projSpeed = 16f, spritePath = "Assets/Sprites/Towers/Tower_Sniper.png", projectileKey = "Projectile_Laser", description = "Long-range, high-damage." },
            new TowerDef{ prefabName = "Tower_Slow",   towerName = "Slow",   cost = 75,  upgradeCost = 50, dmg = 5,  upgDmg = 8,  range = 2.5f, upgRange = 3f,  rate = 1.5f, upgRate = 1.8f, projSpeed = 8f,  spritePath = "Assets/Sprites/Towers/Tower_Slow.png",   projectileKey = "Projectile_Slow",   description = "Slows enemies on hit." },
            new TowerDef{ prefabName = "Tower_Area",   towerName = "Area",   cost = 125, upgradeCost = 90, dmg = 20, upgDmg = 35, range = 2f, upgRange = 2.5f, rate = 0.8f, upgRate = 1.0f, projSpeed = 8f,  spritePath = "Assets/Sprites/Towers/Tower_Area.png",   projectileKey = "Projectile_AOE",    description = "Splash damage." },
        };

        static Dictionary<string, TowerData> towerDataAssets = new Dictionary<string, TowerData>();
        static Dictionary<string, GameObject> towerPrefabs = new Dictionary<string, GameObject>();

        static void CreateTowerDataAndPrefabs()
        {
            EnsureFolder(TowerDataFolder);
            EnsureFolder(TowerPrefabFolder);
            towerDataAssets.Clear();
            towerPrefabs.Clear();

            foreach (var def in TowerDefs)
            {
                string dataPath = $"{TowerDataFolder}/{def.prefabName}_Data.asset";
                TowerData dataAsset = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
                if (dataAsset == null)
                {
                    dataAsset = ScriptableObject.CreateInstance<TowerData>();
                    AssetDatabase.CreateAsset(dataAsset, dataPath);
                }
                dataAsset.towerName = def.towerName;
                dataAsset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(def.spritePath);
                dataAsset.cost = def.cost;
                dataAsset.upgradeCost = def.upgradeCost;
                dataAsset.description = def.description;
                dataAsset.damage = def.dmg;
                dataAsset.range = def.range;
                dataAsset.fireRate = def.rate;
                dataAsset.projectileSpeed = def.projSpeed;
                dataAsset.upgradedDamage = def.upgDmg;
                dataAsset.upgradedRange = def.upgRange;
                dataAsset.upgradedFireRate = def.upgRate;
                projectilePrefabs.TryGetValue(def.projectileKey, out GameObject proj);
                if (proj == null) proj = AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjectilePrefabFolder}/{def.projectileKey}.prefab");
                dataAsset.projectilePrefab = proj;
                EditorUtility.SetDirty(dataAsset);
                towerDataAssets[def.prefabName] = dataAsset;

                // Tower prefab
                string prefabPath = $"{TowerPrefabFolder}/{def.prefabName}.prefab";
                GameObject go = new GameObject(def.prefabName);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dataAsset.icon;
                sr.sortingOrder = 3;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.9f, 0.9f);
                var tower = go.AddComponent<TowerBase>();
                SetPrivateField(tower, "data", dataAsset);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                towerPrefabs[def.prefabName] = prefab;
            }
            Debug.Log("[Phase2Setup] Tower data + prefabs ready.");
        }

        // ============================================================================
        // Wave data
        // ============================================================================
        struct WaveEntry { public string enemyPrefab; public int count; }
        struct WaveDef
        {
            public string name;
            public WaveEntry[] entries;
            public float spawnInterval;
            public float delay;
        }

        static readonly WaveDef[] WaveDefs = new WaveDef[]
        {
            new WaveDef{ name = "Wave_01", spawnInterval = 2.0f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 5 } } },
            new WaveDef{ name = "Wave_02", spawnInterval = 1.8f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 8 } } },
            new WaveDef{ name = "Wave_03", spawnInterval = 1.5f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 12 } } },
            new WaveDef{ name = "Wave_04", spawnInterval = 1.5f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 8 }, new WaveEntry{ enemyPrefab = "Enemy_Fast", count = 5 } } },
            new WaveDef{ name = "Wave_05", spawnInterval = 1.2f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 6 }, new WaveEntry{ enemyPrefab = "Enemy_Fast", count = 8 } } },
            new WaveDef{ name = "Wave_06", spawnInterval = 1.5f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 10 }, new WaveEntry{ enemyPrefab = "Enemy_Fast", count = 6 }, new WaveEntry{ enemyPrefab = "Enemy_Tank", count = 3 } } },
            new WaveDef{ name = "Wave_07", spawnInterval = 1.3f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 5 }, new WaveEntry{ enemyPrefab = "Enemy_Fast", count = 10 }, new WaveEntry{ enemyPrefab = "Enemy_Tank", count = 5 } } },
            new WaveDef{ name = "Wave_08", spawnInterval = 2.5f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Tank", count = 15 } } },
            new WaveDef{ name = "Wave_09", spawnInterval = 0.7f, delay = 2f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Fast", count = 25 } } },
            new WaveDef{ name = "Wave_10", spawnInterval = 2.0f, delay = 3f, entries = new[]{ new WaveEntry{ enemyPrefab = "Enemy_Basic", count = 8 }, new WaveEntry{ enemyPrefab = "Enemy_Tank", count = 2 }, new WaveEntry{ enemyPrefab = "Enemy_Boss", count = 1 } } },
        };

        static List<WaveData> waveAssets = new List<WaveData>();

        static void CreateWaveData()
        {
            EnsureFolder(WaveDataFolder);
            waveAssets.Clear();

            foreach (var def in WaveDefs)
            {
                string path = $"{WaveDataFolder}/{def.name}.asset";
                WaveData wd = AssetDatabase.LoadAssetAtPath<WaveData>(path);
                if (wd == null)
                {
                    wd = ScriptableObject.CreateInstance<WaveData>();
                    AssetDatabase.CreateAsset(wd, path);
                }
                wd.waveName = def.name;
                wd.spawnInterval = def.spawnInterval;
                wd.delayBeforeWave = def.delay;
                wd.enemies = new List<WaveData.EnemySpawnEntry>();
                foreach (var e in def.entries)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/{e.enemyPrefab}.prefab");
                    wd.enemies.Add(new WaveData.EnemySpawnEntry { enemyPrefab = prefab, count = e.count });
                }
                EditorUtility.SetDirty(wd);
                waveAssets.Add(wd);
            }
            Debug.Log("[Phase2Setup] Wave data ready.");
        }

        // ============================================================================
        // Level_01 build
        // ============================================================================
        static void BuildLevel01()
        {
            // Make sure tiles/prefabs are loaded into the local caches.
            if (grassTile == null) CreateTileAssets();
            if (enemyPrefabs == null || enemyPrefabs.Count == 0) LoadEnemyPrefabsFromDisk();
            if (projectilePrefabs == null || projectilePrefabs.Count == 0) LoadProjectilePrefabsFromDisk();
            if (waveAssets == null || waveAssets.Count == 0) LoadWavesFromDisk();

            var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

            // Bump camera ortho size so the 22x14 grid fits in the editor Game view during verify.
            Camera mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (mainCam != null)
            {
                mainCam.orthographicSize = 8.5f;
            }

            // Find scene objects.
            GameObject gridGO = GameObject.Find("Grid");
            if (gridGO == null)
            {
                Debug.LogError("[Phase2Setup] No 'Grid' in Level_01. Run Phase 1's '02 Create Phase 1 Scenes' first.");
                return;
            }
            Grid grid = gridGO.GetComponent<Grid>();
            if (grid == null) grid = gridGO.AddComponent<Grid>();
            gridGO.transform.position = GridWorldOrigin;

            Tilemap tmGround = FindOrCreateTilemap(gridGO, "Tilemap_Ground", 0);
            Tilemap tmPath = FindOrCreateTilemap(gridGO, "Tilemap_Path", 1);
            Tilemap tmObstacles = FindOrCreateTilemap(gridGO, "Tilemap_Obstacles", 2);

            // Clear existing tiles so re-running doesn't stack changes.
            tmGround.ClearAllTiles();
            tmPath.ClearAllTiles();
            tmObstacles.ClearAllTiles();

            // Determine path cells from waypoints.
            HashSet<Vector2Int> pathCells = BuildPathCellSet();

            // Fill grass + path + outer borders.
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    bool isBorder = (x == 0 || x == GridWidth - 1 || y == 0 || y == GridHeight - 1);
                    bool isPath = pathCells.Contains(new Vector2Int(x, y));

                    if (isPath)
                    {
                        tmPath.SetTile(cell, pathTile);
                    }
                    else if (isBorder)
                    {
                        // Water on left/right, rocks on top/bottom.
                        bool sideBorder = (x == 0 || x == GridWidth - 1);
                        tmObstacles.SetTile(cell, sideBorder ? waterTile : rockTile);
                    }
                    else
                    {
                        tmGround.SetTile(cell, grassTile);
                    }
                }
            }

            // PathManager waypoints.
            PathManager pathManager = GameObject.FindFirstObjectByType<PathManager>();
            if (pathManager == null)
            {
                var pmGO = new GameObject("PathManager");
                pathManager = pmGO.AddComponent<PathManager>();
            }
            // Clean any pre-existing children named Waypoint_*.
            List<Transform> existing = new List<Transform>();
            for (int i = 0; i < pathManager.transform.childCount; i++)
            {
                existing.Add(pathManager.transform.GetChild(i));
            }
            foreach (var t in existing)
            {
                if (t.name.StartsWith("Waypoint_")) Object.DestroyImmediate(t.gameObject);
            }

            Transform[] waypointTransforms = new Transform[WaypointCells.Length];
            for (int i = 0; i < WaypointCells.Length; i++)
            {
                Vector2Int c = WaypointCells[i];
                var wpGO = new GameObject($"Waypoint_{i:00}");
                wpGO.transform.SetParent(pathManager.transform, true);
                wpGO.transform.position = CellToWorldCenter(c);
                waypointTransforms[i] = wpGO.transform;
            }
            SetPrivateField(pathManager, "waypoints", waypointTransforms);

            // Flags at start + end.
            CreateFlag("StartFlag", waypointTransforms[0].position, StartFlagColor, pathManager.transform);
            CreateFlag("EndFlag", waypointTransforms[waypointTransforms.Length - 1].position, EndFlagColor, pathManager.transform);

            // PoolManager
            GameObject poolGO = GameObject.Find("PoolManager");
            if (poolGO == null)
            {
                poolGO = new GameObject("PoolManager");
            }
            PoolManager pm = poolGO.GetComponent<PoolManager>() ?? poolGO.AddComponent<PoolManager>();
            // Prefab refs.
            SetPrivateField(pm, "enemiesPrefab", enemyPrefabs["Enemy_Basic"]);
            SetPrivateField(pm, "projectilesPrefab", projectilePrefabs["Projectile_Bullet"]);
            SetPrivateField(pm, "effectsPrefab", null);
            var extraPools = new List<PoolManager.PoolSpec>
            {
                new PoolManager.PoolSpec{ poolName = "Enemy_Enemy_Basic", prefab = enemyPrefabs["Enemy_Basic"], size = 20 },
                new PoolManager.PoolSpec{ poolName = "Enemy_Enemy_Fast",  prefab = enemyPrefabs["Enemy_Fast"],  size = 20 },
                new PoolManager.PoolSpec{ poolName = "Enemy_Enemy_Tank",  prefab = enemyPrefabs["Enemy_Tank"],  size = 10 },
                new PoolManager.PoolSpec{ poolName = "Enemy_Enemy_Boss",  prefab = enemyPrefabs["Enemy_Boss"],  size = 3 },
                new PoolManager.PoolSpec{ poolName = "Projectile_Projectile_Bullet", prefab = projectilePrefabs["Projectile_Bullet"], size = 30 },
                new PoolManager.PoolSpec{ poolName = "Projectile_Projectile_Laser",  prefab = projectilePrefabs["Projectile_Laser"],  size = 15 },
                new PoolManager.PoolSpec{ poolName = "Projectile_Projectile_Slow",   prefab = projectilePrefabs["Projectile_Slow"],   size = 15 },
                new PoolManager.PoolSpec{ poolName = "Projectile_Projectile_AOE",    prefab = projectilePrefabs["Projectile_AOE"],    size = 15 },
            };
            SetPrivateField(pm, "extraPools", extraPools);

            // TowerPlacement
            GameObject tpGO = GameObject.Find("TowerPlacement");
            if (tpGO == null) tpGO = new GameObject("TowerPlacement");
            TowerPlacement tp = tpGO.GetComponent<TowerPlacement>() ?? tpGO.AddComponent<TowerPlacement>();
            SetPrivateField(tp, "mainCamera", mainCam);
            SetPrivateField(tp, "towerGrid", grid);
            SetPrivateField(tp, "buildableTilemap", tmGround);
            SetPrivateField(tp, "buildableTile", grassTile);
            SetPrivateField(tp, "obstacleTilemap", tmObstacles);

            // CameraShake + SpeedController
            GameObject fxGO = GameObject.Find("GameFeel");
            if (fxGO == null) fxGO = new GameObject("GameFeel");
            if (fxGO.GetComponent<CameraShake>() == null) fxGO.AddComponent<CameraShake>();
            if (fxGO.GetComponent<SpeedController>() == null) fxGO.AddComponent<SpeedController>();

            // DebugHUD — OnGUI panel for verify. Wire TowerData refs so tower buttons work.
            GameObject hudGO = GameObject.Find("DebugHUD");
            if (hudGO == null) hudGO = new GameObject("DebugHUD");
            DebugHUD hud = hudGO.GetComponent<DebugHUD>() ?? hudGO.AddComponent<DebugHUD>();
            if (towerDataAssets == null || towerDataAssets.Count == 0)
            {
                towerDataAssets = new Dictionary<string, TowerData>();
                foreach (var def in TowerDefs)
                {
                    var td = AssetDatabase.LoadAssetAtPath<TowerData>($"{TowerDataFolder}/{def.prefabName}_Data.asset");
                    if (td != null) towerDataAssets[def.prefabName] = td;
                }
            }
            SetPrivateField(hud, "basic", towerDataAssets.TryGetValue("Tower_Basic", out var tdb) ? tdb : null);
            SetPrivateField(hud, "sniper", towerDataAssets.TryGetValue("Tower_Sniper", out var tds) ? tds : null);
            SetPrivateField(hud, "slow", towerDataAssets.TryGetValue("Tower_Slow", out var tdl) ? tdl : null);
            SetPrivateField(hud, "area", towerDataAssets.TryGetValue("Tower_Area", out var tda) ? tda : null);

            // WaveManager wiring
            WaveManager wm = GameObject.FindFirstObjectByType<WaveManager>();
            if (wm == null)
            {
                var wmGO = new GameObject("WaveManager");
                wm = wmGO.AddComponent<WaveManager>();
            }
            SetPrivateField(wm, "waveDataList", new List<WaveData>(waveAssets));
            SetPrivateField(wm, "pathManager", pathManager);

            // Auto-placed debug starter tower in a known grass cell so the Play test shows firing without UI.
            // We don't spawn a live Tower here — Step 11 places one via TowerPlacement at runtime (or user clicks in Scene).

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase2Setup] Level_01 tilemap + waypoints + wiring saved.");
        }

        static Tilemap FindOrCreateTilemap(GameObject gridGO, string name, int sortingOrder)
        {
            Transform t = gridGO.transform.Find(name);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(gridGO.transform, false);
            }
            Tilemap tm = go.GetComponent<Tilemap>() ?? go.AddComponent<Tilemap>();
            TilemapRenderer renderer = go.GetComponent<TilemapRenderer>() ?? go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tm;
        }

        static HashSet<Vector2Int> BuildPathCellSet()
        {
            var set = new HashSet<Vector2Int>();
            for (int i = 0; i < WaypointCells.Length - 1; i++)
            {
                Vector2Int a = WaypointCells[i];
                Vector2Int b = WaypointCells[i + 1];
                int dx = System.Math.Sign(b.x - a.x);
                int dy = System.Math.Sign(b.y - a.y);
                Vector2Int cur = a;
                set.Add(cur);
                while (cur != b)
                {
                    if (cur.x != b.x) cur.x += dx;
                    else if (cur.y != b.y) cur.y += dy;
                    set.Add(cur);
                }
            }
            return set;
        }

        static Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return GridWorldOrigin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        static void CreateFlag(string name, Vector3 pos, Color color, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = pos + new Vector3(0f, 0.2f, 0f);
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateOrGetColorSprite(name == "StartFlag" ? "FlagGreen" : "FlagRed", color);
            sr.sortingOrder = 5;
        }

        static void LoadEnemyPrefabsFromDisk()
        {
            enemyPrefabs.Clear();
            foreach (var def in EnemyDefs)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/{def.prefabName}.prefab");
                if (p != null) enemyPrefabs[def.prefabName] = p;
            }
        }

        static void LoadProjectilePrefabsFromDisk()
        {
            projectilePrefabs.Clear();
            string[] names = { "Projectile_Bullet", "Projectile_Laser", "Projectile_Slow", "Projectile_AOE" };
            foreach (var n in names)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjectilePrefabFolder}/{n}.prefab");
                if (p != null) projectilePrefabs[n] = p;
            }
        }

        static void LoadWavesFromDisk()
        {
            waveAssets.Clear();
            foreach (var def in WaveDefs)
            {
                var w = AssetDatabase.LoadAssetAtPath<WaveData>($"{WaveDataFolder}/{def.name}.asset");
                if (w != null) waveAssets.Add(w);
            }
        }

        // ============================================================================
        // Private-field assignment via SerializedObject (safe on prefab / scene objects).
        // ============================================================================
        static void SetPrivateField(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Phase2Setup] Could not find serialized field '{fieldName}' on {target.GetType().Name}.");
                return;
            }
            if (value is Object unityObj)
            {
                prop.objectReferenceValue = unityObj;
            }
            else if (value is System.Collections.IList list)
            {
                if (prop.propertyType == SerializedPropertyType.Generic && prop.isArray)
                {
                    prop.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++)
                    {
                        SerializedProperty elem = prop.GetArrayElementAtIndex(i);
                        object item = list[i];
                        AssignToProperty(elem, item);
                    }
                }
            }
            else if (value == null)
            {
                prop.objectReferenceValue = null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignToProperty(SerializedProperty prop, object value)
        {
            if (value == null)
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference) prop.objectReferenceValue = null;
                return;
            }
            if (value is Object uo) { prop.objectReferenceValue = uo; return; }
            if (value is PoolManager.PoolSpec spec)
            {
                prop.FindPropertyRelative("poolName").stringValue = spec.poolName;
                prop.FindPropertyRelative("prefab").objectReferenceValue = spec.prefab;
                prop.FindPropertyRelative("size").intValue = spec.size;
            }
        }
    }
}
#endif
