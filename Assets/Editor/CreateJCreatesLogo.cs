#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 5 — generates a stylized JCreates wordmark PNG into Assets/Sprites/UI/.
    /// Letters are pixel-blitted from a tiny 5x7 ASCII font with a soft outer glow.
    /// 800x400 transparent canvas, "J" in gold (#F5A623, ~160px tall) on the left,
    /// "Creates" in white (~80px tall) right of it, gold underline beneath.
    /// </summary>
    public static class CreateJCreatesLogo
    {
        private const string OutPath = "Assets/Sprites/UI/JCreates_Logo.png";
        private const int W = 800;
        private const int H = 400;

        private static readonly Color Gold       = new Color32(0xF5, 0xA6, 0x23, 0xFF);
        private static readonly Color White      = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color GlowGold30 = new Color32(0xF5, 0xA6, 0x23, 0x4D); // 30% alpha

        [MenuItem("Tools/TowerGuard/Create JCreates Logo", priority = 405)]
        public static void RunAll()
        {
            EnsureFolder("Assets/Sprites/UI");
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            FillTransparent(tex);

            // Glow pass: paint each letter twice — once offset by 1px in 8 directions at 30% alpha,
            // then the main color on top. This creates a 1px outer halo.
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            // === "J" — large, ~160 px tall, left-center ===
            int jHeight = 160;
            int jPx = jHeight / 7; // pixel size for the 5x7 font letter so it fills jHeight
            int jX = 60;
            int jY = (H / 2) - (jHeight / 2);

            for (int o = 0; o < 8; o++) DrawLetter(tex, 'J', jX + dx[o], jY + dy[o], jPx, GlowGold30);
            DrawLetter(tex, 'J', jX, jY, jPx, Gold);

            // === "Creates" — ~80 px tall, right-of-J, vertically centered ===
            int cHeight = 80;
            int cPx = cHeight / 7;
            int cX = jX + 5 * jPx + 60; // right of J + gutter
            int cY = (H / 2) - (cHeight / 2);
            string word = "Creates";
            int spacing = cPx; // 1 pixel gap between letters in 5x7 grid
            int penX = cX;
            for (int li = 0; li < word.Length; li++)
            {
                char c = word[li];
                for (int o = 0; o < 8; o++) DrawLetter(tex, c, penX + dx[o], cY + dy[o], cPx, GlowGold30);
                DrawLetter(tex, c, penX, cY, cPx, White);
                penX += 5 * cPx + spacing;
            }

            // === Gold underline 2px tall, 600px wide, centered horizontally ===
            int underlineY = (H / 2) - (jHeight / 2) - 24;
            int underlineW = 660;
            int underlineX = (W - underlineW) / 2;
            for (int x = 0; x < underlineW; x++)
            {
                tex.SetPixel(underlineX + x, underlineY,     Gold);
                tex.SetPixel(underlineX + x, underlineY + 1, Gold);
            }

            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutPath);
            File.WriteAllBytes(abs, png);
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();

            // Re-import as Sprite so the splash scene can use it directly.
            var imp = (TextureImporter)AssetImporter.GetAtPath(OutPath);
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            Debug.Log($"[CreateJCreatesLogo] Wrote {OutPath} ({W}x{H} PNG).");
        }

        // ============================================================
        // Tiny 5x7 ASCII font — only the letters we actually need.
        // 1 = pixel ON, 0 = pixel OFF. Rows are top→bottom.
        // ============================================================
        private static readonly System.Collections.Generic.Dictionary<char, byte[]> Font5x7 =
            new System.Collections.Generic.Dictionary<char, byte[]>
        {
            {'J', new byte[] {
                0b00111,
                0b00010,
                0b00010,
                0b00010,
                0b10010,
                0b10010,
                0b01100,
            }},
            {'C', new byte[] {
                0b01110,
                0b10001,
                0b10000,
                0b10000,
                0b10000,
                0b10001,
                0b01110,
            }},
            {'r', new byte[] {
                0b00000,
                0b00000,
                0b10110,
                0b11001,
                0b10000,
                0b10000,
                0b10000,
            }},
            {'e', new byte[] {
                0b00000,
                0b00000,
                0b01110,
                0b10001,
                0b11111,
                0b10000,
                0b01110,
            }},
            {'a', new byte[] {
                0b00000,
                0b00000,
                0b01110,
                0b00001,
                0b01111,
                0b10001,
                0b01111,
            }},
            {'t', new byte[] {
                0b00100,
                0b11111,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00011,
            }},
            {'s', new byte[] {
                0b00000,
                0b00000,
                0b01111,
                0b10000,
                0b01110,
                0b00001,
                0b11110,
            }},
        };

        private static void DrawLetter(Texture2D tex, char c, int x, int y, int px, Color color)
        {
            if (!Font5x7.TryGetValue(c, out byte[] glyph)) return;
            // Draw 5 columns x 7 rows scaled by `px`. Bit 4 is leftmost.
            for (int row = 0; row < 7; row++)
            {
                byte rowBits = glyph[row];
                for (int col = 0; col < 5; col++)
                {
                    bool on = (rowBits & (1 << (4 - col))) != 0;
                    if (!on) continue;
                    int x0 = x + col * px;
                    int y0 = y + (6 - row) * px; // flip Y so row 0 is top
                    for (int dy = 0; dy < px; dy++)
                    for (int dx = 0; dx < px; dx++)
                    {
                        int px2 = x0 + dx;
                        int py2 = y0 + dy;
                        if (px2 < 0 || py2 < 0 || px2 >= W || py2 >= H) continue;
                        // Alpha-blend over whatever's there so glow + main letter combine.
                        Color existing = tex.GetPixel(px2, py2);
                        float a = color.a;
                        Color blend = new Color(
                            color.r * a + existing.r * (1f - a),
                            color.g * a + existing.g * (1f - a),
                            color.b * a + existing.b * (1f - a),
                            Mathf.Min(1f, existing.a + a));
                        tex.SetPixel(px2, py2, blend);
                    }
                }
            }
        }

        private static void FillTransparent(Texture2D tex)
        {
            var pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);
            tex.SetPixels(pixels);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
