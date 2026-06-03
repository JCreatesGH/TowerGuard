#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Generates colored placeholder PNG sprites used during Phase 1 bring-up.
    /// Menu: Tools > Create Placeholder Sprites.
    /// </summary>
    public static class CreatePlaceholderSprites
    {
        private const string EnemiesPath = "Assets/Sprites/Enemies";
        private const string TowersPath = "Assets/Sprites/Towers";

        [MenuItem("Tools/Create Placeholder Sprites")]
        public static void Create()
        {
            EnsureFolder(EnemiesPath);
            EnsureFolder(TowersPath);

            // ----- Enemies (circles) -----
            WriteCircle(EnemiesPath, "Enemy_Basic", 64, Hex("#E53935"));
            WriteCircle(EnemiesPath, "Enemy_Fast",  64, Hex("#FF6D00"));
            WriteCircle(EnemiesPath, "Enemy_Tank",  64, Hex("#7B1FA2"));
            WriteCircle(EnemiesPath, "Enemy_Boss",  128, Hex("#4A148C"));

            // ----- Towers (squares) -----
            WriteSquare(TowersPath, "Tower_Basic",  64, Hex("#1565C0"));
            WriteSquare(TowersPath, "Tower_Sniper", 64, Hex("#00838F"));
            WriteSquare(TowersPath, "Tower_Slow",   64, Hex("#2E7D32"));
            WriteSquare(TowersPath, "Tower_Area",   64, Hex("#F57F17"));

            // ----- Projectiles (saved under Towers per spec) -----
            WriteCircle(TowersPath, "Projectile_Bullet", 16, Color.white);
            WriteRect(TowersPath,   "Projectile_Laser", 8, 32, Hex("#00E5FF"));
            WriteCircle(TowersPath, "Projectile_AOE",   32, Hex("#FFEB3B"));

            AssetDatabase.Refresh();

            // Second pass: tweak import settings so every PNG becomes a Sprite (Single, 100 PPU).
            ApplySpriteImportSettings(EnemiesPath);
            ApplySpriteImportSettings(TowersPath);

            Debug.Log("[CreatePlaceholderSprites] Placeholder sprites created under " + EnemiesPath + " and " + TowersPath);
        }

        // ----- File I/O helpers -----

        private static void EnsureFolder(string assetsRelativePath)
        {
            if (AssetDatabase.IsValidFolder(assetsRelativePath)) return;

            var parts = assetsRelativePath.Split('/');
            string current = parts[0]; // "Assets"
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

        private static void WritePng(string folder, string name, Texture2D tex)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", folder, name + ".png");
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ApplySpriteImportSettings(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        // ----- Shape painters -----

        private static void WriteCircle(string folder, string name, int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f;
            float cx = r - 0.5f;
            float cy = r - 0.5f;
            var transparent = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, d <= r - 0.5f ? color : transparent);
                }
            }
            tex.Apply();
            WritePng(folder, name, tex);
        }

        private static void WriteSquare(string folder, string name, int size, Color color)
        {
            WriteRect(folder, name, size, size, color);
        }

        private static void WriteRect(string folder, string name, int width, int height, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            WritePng(folder, name, tex);
        }

        // ----- Utility -----

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }
    }
}
#endif
