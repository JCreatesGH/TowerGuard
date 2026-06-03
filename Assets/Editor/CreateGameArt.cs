#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 5 — premium procedural sprite generator.
    /// Each Make* method composes a sprite from multiple layers (shadow, silhouette,
    /// internal fills, specular highlights, outline glow) at high canvas density and
    /// anti-aliased edges. The result is recognizable, on-palette pixel art that's a
    /// significant step up from the geometric-primitive pass that originally shipped.
    /// Menu: Tools > TowerGuard > Create All Game Art.
    /// </summary>
    public static class CreateGameArt
    {
        // ---- folders ----
        private const string EnemyFolder    = "Assets/Sprites/Enemies";
        private const string TowerFolder    = "Assets/Sprites/Towers";
        private const string TilemapFolder  = "Assets/Sprites/Tilemap";
        private const string UIFolder       = "Assets/Sprites/UI";
        private const string BgFolder       = "Assets/Sprites/Background";

        // ---- canvas sizes ----
        private const int ENEMY = 128;
        private const int BOSS  = 256;
        private const int TOWER = 128;
        private const int PROJ  = 32;
        private const int LASER_W = 12, LASER_H = 48;
        private const int UI_PANEL = 192;
        private const int UI_BTN_W = 256, UI_BTN_H = 72;
        private const int UI_ICON = 64;
        private const int TILE = 64;
        private const int FLAG = 64;
        private const int CROWN = 48;
        private const int POWERNODE = 96;
        private const int ARROW = 32;
        private const int BG_M_W = 1024, BG_M_H = 256;
        private const int BG_T_W = 1024, BG_T_H = 128;

        // ---- palette helpers ----
        private static readonly Color CTransparent = new Color(0, 0, 0, 0);
        private static Color RGB(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
        private static Color RGBA(byte r, byte g, byte b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);

        [MenuItem("Tools/TowerGuard/Create All Game Art", priority = 410)]
        public static void RunAll()
        {
            EnsureFolder(EnemyFolder);
            EnsureFolder(TowerFolder);
            EnsureFolder(TilemapFolder);
            EnsureFolder(UIFolder);
            EnsureFolder(BgFolder);

            // Enemies
            WriteSprite($"{EnemyFolder}/Enemy_Basic.png", MakeEnemyBasic());
            WriteSprite($"{EnemyFolder}/Enemy_Fast.png",  MakeEnemyFast());
            WriteSprite($"{EnemyFolder}/Enemy_Tank.png",  MakeEnemyTank());
            WriteSprite($"{EnemyFolder}/Enemy_Boss.png",  MakeEnemyBoss());

            // Towers (base + upgraded variants)
            WriteSprite($"{TowerFolder}/Tower_Basic.png",           MakeTowerBasic(false));
            WriteSprite($"{TowerFolder}/Tower_Sniper.png",          MakeTowerSniper(false));
            WriteSprite($"{TowerFolder}/Tower_Slow.png",            MakeTowerSlow(false));
            WriteSprite($"{TowerFolder}/Tower_Area.png",            MakeTowerArea(false));
            WriteSprite($"{TowerFolder}/Tower_Basic_Upgraded.png",  MakeTowerBasic(true));
            WriteSprite($"{TowerFolder}/Tower_Sniper_Upgraded.png", MakeTowerSniper(true));
            WriteSprite($"{TowerFolder}/Tower_Slow_Upgraded.png",   MakeTowerSlow(true));
            WriteSprite($"{TowerFolder}/Tower_Area_Upgraded.png",   MakeTowerArea(true));

            // Projectiles
            WriteSprite($"{TowerFolder}/Projectile_Bullet.png", MakeProjectileBullet());
            WriteSprite($"{TowerFolder}/Projectile_Laser.png",  MakeProjectileLaser());
            WriteSprite($"{TowerFolder}/Projectile_Slow.png",   MakeProjectileSlow());
            WriteSprite($"{TowerFolder}/Projectile_AOE.png",    MakeProjectileAOE());

            // Tilemap
            WriteSprite($"{TilemapFolder}/Tile_Grass.png",   MakeTileGrass());
            WriteSprite($"{TilemapFolder}/Tile_Path.png",    MakeTilePath());
            WriteSprite($"{TilemapFolder}/Tile_Rock.png",    MakeTileRock());
            WriteSprite($"{TilemapFolder}/Tile_Water_0.png", MakeTileWater(0));
            WriteSprite($"{TilemapFolder}/Tile_Water_1.png", MakeTileWater(1));
            WriteSprite($"{TilemapFolder}/Tile_Water_2.png", MakeTileWater(2));
            WriteSprite($"{TilemapFolder}/StartFlag.png",    MakeFlag(true));
            WriteSprite($"{TilemapFolder}/EndFlag.png",      MakeFlag(false));

            // UI
            WriteSprite($"{UIFolder}/Panel_Dark.png",  MakePanel(RGB(0x16, 0x21, 0x3E), RGB(0x1E, 0x3A, 0x5F)), is9Slice: true);
            WriteSprite($"{UIFolder}/Panel_Gold.png",  MakePanel(RGB(0x16, 0x21, 0x3E), RGB(0xF5, 0xA6, 0x23)), is9Slice: true);
            WriteSprite($"{UIFolder}/Button_Blue.png", MakeButton(RGB(0x0F, 0x34, 0x60), RGB(0x15, 0x65, 0xC0), RGB(0x0A, 0x1F, 0x40)), is9Slice: true);
            WriteSprite($"{UIFolder}/Button_Red.png",  MakeButton(RGB(0xC6, 0x28, 0x28), RGB(0xE5, 0x39, 0x35), RGB(0x80, 0x10, 0x10)), is9Slice: true);
            WriteSprite($"{UIFolder}/Button_Gold.png", MakeButton(RGB(0xE6, 0x51, 0x00), RGB(0xFF, 0x6D, 0x00), RGB(0xA0, 0x33, 0x00)), is9Slice: true);
            WriteSprite($"{UIFolder}/Coin_Icon.png",   MakeCoinIcon());
            WriteSprite($"{UIFolder}/Gem_Icon.png",    MakeGemIcon());
            WriteSprite($"{UIFolder}/Heart_Icon.png",  MakeHeartIcon());
            WriteSprite($"{UIFolder}/Star_Full.png",   MakeStar(true));
            WriteSprite($"{UIFolder}/Star_Empty.png",  MakeStar(false));
            WriteSprite($"{UIFolder}/Crown.png",       MakeCrown());
            WriteSprite($"{UIFolder}/PowerNode.png",   MakePowerNode());
            WriteSprite($"{UIFolder}/Arrow.png",       MakeArrow());

            // Background parallax
            WriteSprite($"{BgFolder}/Bg_Mountains.png", MakeBgMountains());
            WriteSprite($"{BgFolder}/Bg_Trees.png",     MakeBgTrees());

            AssetDatabase.Refresh();
            ApplySpriteImports();
            Debug.Log("[CreateGameArt] Premium sprites generated.");
        }

        // ====================================================================
        // Drawing primitives
        // ====================================================================
        private static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear; // bilinear at the bigger canvas reads softer/cleaner in-engine
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = CTransparent;
            t.SetPixels(px);
            return t;
        }

        private static void Set(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            t.SetPixel(x, y, c);
        }

        private static void Blend(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            Color e = t.GetPixel(x, y);
            float a = c.a;
            float oa = e.a;
            float outA = a + oa * (1f - a);
            if (outA <= 0f) { t.SetPixel(x, y, CTransparent); return; }
            t.SetPixel(x, y, new Color(
                (c.r * a + e.r * oa * (1f - a)) / outA,
                (c.g * a + e.g * oa * (1f - a)) / outA,
                (c.b * a + e.b * oa * (1f - a)) / outA,
                Mathf.Min(1f, outA)));
        }

        private static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));
        private static Color Brighten(Color c, float k) => new Color(
            Mathf.Clamp01(c.r + k), Mathf.Clamp01(c.g + k), Mathf.Clamp01(c.b + k), c.a);
        private static Color Darken(Color c, float k) => Brighten(c, -k);

        private static void FillCircle(Texture2D t, float cx, float cy, float r, Color fill)
        {
            int minX = Mathf.FloorToInt(cx - r - 1), maxX = Mathf.CeilToInt(cx + r + 1);
            int minY = Mathf.FloorToInt(cy - r - 1), maxY = Mathf.CeilToInt(cy + r + 1);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (d <= r - 0.5f) Blend(t, x, y, fill);
                else if (d < r + 0.5f)
                {
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    Blend(t, x, y, new Color(fill.r, fill.g, fill.b, fill.a * a));
                }
            }
        }

        private static void FillEllipse(Texture2D t, float cx, float cy, float rx, float ry, Color fill)
        {
            int minX = Mathf.FloorToInt(cx - rx - 1), maxX = Mathf.CeilToInt(cx + rx + 1);
            int minY = Mathf.FloorToInt(cy - ry - 1), maxY = Mathf.CeilToInt(cy + ry + 1);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 0.95f) Blend(t, x, y, fill);
                else if (d < 1.05f)
                {
                    float a = Mathf.Clamp01((1.05f - d) / 0.1f);
                    Blend(t, x, y, new Color(fill.r, fill.g, fill.b, fill.a * a));
                }
            }
        }

        private static void StrokeCircle(Texture2D t, float cx, float cy, float r, float thickness, Color color)
        {
            int minX = Mathf.FloorToInt(cx - r - thickness - 1), maxX = Mathf.CeilToInt(cx + r + thickness + 1);
            int minY = Mathf.FloorToInt(cy - r - thickness - 1), maxY = Mathf.CeilToInt(cy + r + thickness + 1);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float diff = Mathf.Abs(d - r);
                if (diff <= thickness * 0.5f) Blend(t, x, y, color);
                else if (diff <= thickness * 0.5f + 0.5f)
                {
                    float a = (thickness * 0.5f + 0.5f - diff);
                    Blend(t, x, y, new Color(color.r, color.g, color.b, color.a * Mathf.Clamp01(a)));
                }
            }
        }

        private static void FillRect(Texture2D t, int x0, int y0, int w, int h, Color fill)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                Blend(t, x0 + x, y0 + y, fill);
        }

        private static void FillRoundedRect(Texture2D t, int x0, int y0, int w, int h, int radius, Color fill)
        {
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inCorner = false; Vector2 corner = Vector2.zero;
                if (x < radius && y < radius) { inCorner = true; corner = new Vector2(radius, radius); }
                else if (x >= w - radius && y < radius) { inCorner = true; corner = new Vector2(w - radius - 1, radius); }
                else if (x < radius && y >= h - radius) { inCorner = true; corner = new Vector2(radius, h - radius - 1); }
                else if (x >= w - radius && y >= h - radius) { inCorner = true; corner = new Vector2(w - radius - 1, h - radius - 1); }

                if (inCorner)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), corner);
                    if (d > radius + 0.5f) continue;
                    float alpha = d <= radius - 0.5f ? 1f : Mathf.Clamp01(radius + 0.5f - d);
                    Blend(t, x0 + x, y0 + y, new Color(fill.r, fill.g, fill.b, fill.a * alpha));
                }
                else
                {
                    Blend(t, x0 + x, y0 + y, fill);
                }
            }
        }

        private static void StrokeRoundedRect(Texture2D t, int x0, int y0, int w, int h, int radius, int thickness, Color color)
        {
            // Outer fill - inner fill = stroke ring.
            // Cheap approach: scan the rounded rect band pixel-by-pixel.
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = SignedDistanceRoundedRect(x + 0.5f, y + 0.5f, w, h, radius);
                if (d <= 0 && d >= -thickness)
                {
                    Blend(t, x0 + x, y0 + y, color);
                }
            }
        }

        // Negative inside, positive outside.
        private static float SignedDistanceRoundedRect(float px, float py, int w, int h, int r)
        {
            float qx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
            float qy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0);
            return outside + inside - r;
        }

        private static void FillTriangle(Texture2D t, Vector2 a, Vector2 b, Vector2 c, Color fill)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (PointInTri(p, a, b, c)) Blend(t, x, y, fill);
            }
        }

        private static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(neg && pos);
        }
        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        private static void FillPolygon(Texture2D t, Vector2[] pts, Color fill)
        {
            float minX = pts[0].x, maxX = pts[0].x, minY = pts[0].y, maxY = pts[0].y;
            for (int i = 1; i < pts.Length; i++)
            {
                if (pts[i].x < minX) minX = pts[i].x;
                if (pts[i].x > maxX) maxX = pts[i].x;
                if (pts[i].y < minY) minY = pts[i].y;
                if (pts[i].y > maxY) maxY = pts[i].y;
            }
            int x0 = Mathf.FloorToInt(minX), x1 = Mathf.CeilToInt(maxX);
            int y0 = Mathf.FloorToInt(minY), y1 = Mathf.CeilToInt(maxY);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (PointInPoly(new Vector2(x + 0.5f, y + 0.5f), pts)) Blend(t, x, y, fill);
            }
        }

        private static bool PointInPoly(Vector2 p, Vector2[] pts)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                if (((pts[i].y > p.y) != (pts[j].y > p.y)) &&
                    (p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static void DrawLineThick(Texture2D t, Vector2 a, Vector2 b, float thickness, Color color)
        {
            float len = (b - a).magnitude;
            if (len < 0.001f) { FillCircle(t, a.x, a.y, thickness * 0.5f, color); return; }
            int steps = Mathf.CeilToInt(len * 2);
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                Vector2 p = Vector2.Lerp(a, b, u);
                FillCircle(t, p.x, p.y, thickness * 0.5f, color);
            }
        }

        private static void VerticalGradient(Texture2D t, int x0, int y0, int w, int h, Color top, Color bot)
        {
            for (int y = 0; y < h; y++)
            {
                float u = (float)y / Mathf.Max(1, h - 1);
                Color c = Color.Lerp(bot, top, u);
                for (int x = 0; x < w; x++) Blend(t, x0 + x, y0 + y, c);
            }
        }

        private static void HorizontalGradient(Texture2D t, int x0, int y0, int w, int h, Color left, Color right)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / Mathf.Max(1, w - 1);
                Color c = Color.Lerp(left, right, u);
                for (int y = 0; y < h; y++) Blend(t, x0 + x, y0 + y, c);
            }
        }

        // Add a soft halo around the existing silhouette (alpha > threshold).
        private static void AddOuterGlow(Texture2D t, Color glowColor, int passes)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels();
            for (int pass = 0; pass < passes; pass++)
            {
                var prev = (Color[])src.Clone();
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (prev[idx].a > 0.05f) continue;
                    float maxN = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (prev[ny * w + nx].a > maxN) maxN = prev[ny * w + nx].a;
                    }
                    if (maxN > 0.05f)
                    {
                        float a = (1f - (float)pass / Mathf.Max(1, passes)) * 0.6f * glowColor.a * maxN;
                        // Blend over existing pixel.
                        Color e = src[idx];
                        Color glow = new Color(glowColor.r, glowColor.g, glowColor.b, a);
                        float oa = glow.a + e.a * (1f - glow.a);
                        if (oa > 0f)
                        {
                            src[idx] = new Color(
                                (glow.r * glow.a + e.r * e.a * (1f - glow.a)) / oa,
                                (glow.g * glow.a + e.g * e.a * (1f - glow.a)) / oa,
                                (glow.b * glow.a + e.b * e.a * (1f - glow.a)) / oa,
                                Mathf.Min(1f, oa));
                        }
                    }
                }
            }
            t.SetPixels(src);
        }

        // Soft drop shadow under the silhouette.
        private static void AddDropShadow(Texture2D t, int dx, int dy, int blur, Color shadow)
        {
            int w = t.width, h = t.height;
            var src = t.GetPixels();
            var dst = new Color[w * h];
            for (int i = 0; i < dst.Length; i++) dst[i] = CTransparent;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = x - dx, sy = y - dy;
                if (sx < 0 || sy < 0 || sx >= w || sy >= h) continue;
                float a = src[sy * w + sx].a;
                if (a > 0.05f) dst[y * w + x] = new Color(shadow.r, shadow.g, shadow.b, shadow.a * a);
            }
            // Blur passes (box blur)
            for (int pass = 0; pass < blur; pass++)
            {
                var prev = (Color[])dst.Clone();
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = 0f; int count = 0;
                    for (int by = -1; by <= 1; by++)
                    for (int bx = -1; bx <= 1; bx++)
                    {
                        int nx = x + bx, ny = y + by;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        a += prev[ny * w + nx].a; count++;
                    }
                    if (count > 0) dst[y * w + x] = new Color(shadow.r, shadow.g, shadow.b, a / count);
                }
            }
            // Composite shadow under existing pixels.
            for (int i = 0; i < dst.Length; i++)
            {
                if (dst[i].a < 0.01f) continue;
                Color s = dst[i];
                Color e = src[i];
                float oa = e.a + s.a * (1f - e.a);
                if (oa > 0f)
                {
                    src[i] = new Color(
                        (e.r * e.a + s.r * s.a * (1f - e.a)) / oa,
                        (e.g * e.a + s.g * s.a * (1f - e.a)) / oa,
                        (e.b * e.a + s.b * s.a * (1f - e.a)) / oa,
                        Mathf.Min(1f, oa));
                }
            }
            t.SetPixels(src);
        }

        // ====================================================================
        // Enemies — multi-layered character renders
        // ====================================================================
        private static Texture2D MakeEnemyBasic()
        {
            // Goblin Runner — hunched humanoid, dark green body, glowing orange eyes.
            var t = NewTex(ENEMY, ENEMY);
            int cx = ENEMY / 2;
            Color body  = RGB(0x1B, 0x5E, 0x20);
            Color bodyL = RGB(0x2E, 0x7D, 0x32);
            Color bodyD = RGB(0x0F, 0x3A, 0x14);
            Color skin  = RGB(0x33, 0x69, 0x33);
            Color eye   = RGB(0xFF, 0x6D, 0x00);
            Color thorn = RGB(0x4E, 0x34, 0x2E);
            Color outline = RGB(0x06, 0x1A, 0x07);

            // Ground shadow ellipse.
            FillEllipse(t, cx, 18, 26, 8, RGBA(0, 0, 0, 0.55f));

            // Legs (splayed, mid-stride)
            FillEllipse(t, cx - 14, 28, 6, 18, body);
            FillEllipse(t, cx + 14, 28, 6, 18, body);
            // Knee highlights
            FillEllipse(t, cx - 14, 30, 4, 6, bodyL);
            FillEllipse(t, cx + 14, 30, 4, 6, bodyL);
            // Feet (darker)
            FillEllipse(t, cx - 16, 22, 7, 5, bodyD);
            FillEllipse(t, cx + 16, 22, 7, 5, bodyD);

            // Body — pear-shaped torso, hunched forward
            FillEllipse(t, cx, 56, 22, 26, body);
            // Belly highlight
            FillEllipse(t, cx + 4, 50, 12, 14, bodyL);
            // Spine shadow
            FillEllipse(t, cx - 8, 60, 6, 18, bodyD);

            // Arms hanging at sides
            FillEllipse(t, cx - 22, 56, 6, 14, body);
            FillEllipse(t, cx + 22, 56, 6, 14, body);
            // Hands / claws
            FillCircle(t, cx - 22, 44, 5f, bodyD);
            FillCircle(t, cx + 22, 44, 5f, bodyD);
            // Tiny claw spikes
            FillTriangle(t, new Vector2(cx - 25, 41), new Vector2(cx - 21, 41), new Vector2(cx - 23, 36), thorn);
            FillTriangle(t, new Vector2(cx + 21, 41), new Vector2(cx + 25, 41), new Vector2(cx + 23, 36), thorn);

            // Head — round with a slight forward tilt
            FillCircle(t, cx + 2, 92, 18f, body);
            // Face/jaw region (lighter)
            FillEllipse(t, cx + 4, 86, 11, 8, skin);
            // Brow shadow
            FillEllipse(t, cx + 2, 96, 14, 5, bodyD);

            // Crown of thorns — 6 small spikes around top of head
            for (int i = 0; i < 6; i++)
            {
                float ang = Mathf.PI * (0.15f + 0.7f * i / 5f);
                float bx = cx + 2 + Mathf.Cos(ang) * 17f;
                float by = 92 + Mathf.Sin(ang) * 17f;
                float tx = cx + 2 + Mathf.Cos(ang) * 24f;
                float ty = 92 + Mathf.Sin(ang) * 24f;
                FillTriangle(t,
                    new Vector2(bx - 2, by),
                    new Vector2(bx + 2, by),
                    new Vector2(tx, ty),
                    thorn);
            }

            // Glowing orange eyes — two dots with halo
            FillCircle(t, cx - 4, 90, 4f, RGBA(0xFF, 0x6D, 0x00, 0.6f));
            FillCircle(t, cx + 8, 90, 4f, RGBA(0xFF, 0x6D, 0x00, 0.6f));
            FillCircle(t, cx - 4, 90, 2f, eye);
            FillCircle(t, cx + 8, 90, 2f, eye);
            // Eye shine
            Set(t, (int)cx - 5, 91, Color.white);
            Set(t, (int)cx + 7, 91, Color.white);

            // Mouth — small jagged line
            FillRect(t, cx - 2, 80, 8, 2, RGB(0x40, 0x20, 0x10));
            FillTriangle(t, new Vector2(cx - 1, 80), new Vector2(cx + 1, 80), new Vector2(cx, 78), RGB(0x60, 0x30, 0x18));

            // Outline darken (silhouette ring)
            AddDropShadow(t, 0, -2, 0, RGBA(0, 0, 0, 0.0f)); // no-op for now, kept for symmetry
            AddOuterGlow(t, RGBA(0x76, 0xFF, 0x03, 0.85f), 3);
            return t;
        }

        private static Texture2D MakeEnemyFast()
        {
            // Shadow Wraith — wispy ghost with motion-blur trails and bright magenta core.
            var t = NewTex(ENEMY, ENEMY);
            int cx = ENEMY / 2;
            Color core  = RGB(0xE0, 0x40, 0xFB);
            Color body  = RGB(0x1A, 0x00, 0x33);
            Color bodyM = RGB(0x36, 0x12, 0x5C);
            Color trail = RGBA(0x00, 0xE5, 0xFF, 0.45f);

            // 3 trailing streaks behind (left of body)
            for (int i = 0; i < 3; i++)
            {
                int sx = cx - 20 - i * 10;
                int sy = 60 - i * 4;
                FillEllipse(t, sx, sy, 12 - i * 2, 4 + i, new Color(trail.r, trail.g, trail.b, trail.a * (1f - i * 0.25f)));
            }

            // Wispy tapered body — multiple stacked ellipses, narrow at bottom
            for (int i = 0; i < 14; i++)
            {
                float u = i / 13f;
                float yy = Mathf.Lerp(20, 96, u);
                float rx = Mathf.Lerp(4, 22, u);
                float ry = Mathf.Lerp(2, 6, u);
                Color c = Color.Lerp(body, bodyM, u);
                FillEllipse(t, cx, yy, rx, ry, c);
            }

            // Torn lower edge — small dark triangles hanging
            for (int i = -3; i <= 3; i++)
            {
                float bx = cx + i * 5;
                FillTriangle(t,
                    new Vector2(bx - 2, 22),
                    new Vector2(bx + 2, 22),
                    new Vector2(bx, 14),
                    body);
            }

            // Suggestion of head — slightly wider, brighter top
            FillCircle(t, cx, 100, 14, bodyM);
            FillCircle(t, cx + 2, 102, 6, RGB(0x55, 0x1F, 0x88));

            // Bright magenta core orb, glowing
            FillCircle(t, cx, 78, 11, RGBA(0xE0, 0x40, 0xFB, 0.5f));
            FillCircle(t, cx, 78, 7, core);
            FillCircle(t, cx - 1, 80, 2, Color.white);

            // Two white-purple eyes
            FillCircle(t, cx - 4, 102, 2, RGB(0xFF, 0xFF, 0xFF));
            FillCircle(t, cx + 4, 102, 2, RGB(0xFF, 0xFF, 0xFF));

            // Cyan rim glow
            AddOuterGlow(t, RGBA(0x00, 0xE5, 0xFF, 0.9f), 4);
            return t;
        }

        private static Texture2D MakeEnemyTank()
        {
            // Iron Golem — broad armored figure with rivets and red eye-slits.
            var t = NewTex(ENEMY, ENEMY);
            int cx = ENEMY / 2;
            Color iron     = RGB(0x37, 0x47, 0x4F);
            Color ironL    = RGB(0x54, 0x6E, 0x7A);
            Color ironD    = RGB(0x1C, 0x27, 0x2C);
            Color ironVL   = RGB(0x78, 0x90, 0x9C);
            Color rivet    = RGB(0xB0, 0xBE, 0xC5);
            Color eyeSlit  = RGB(0xFF, 0x17, 0x44);
            Color shadow   = RGBA(0, 0, 0, 0.55f);
            Color outline  = RGB(0x10, 0x18, 0x1C);

            // Ground shadow
            FillEllipse(t, cx, 18, 36, 9, shadow);

            // Legs — squat, wide
            FillRoundedRect(t, cx - 24, 16, 16, 22, 4, iron);
            FillRoundedRect(t, cx + 8,  16, 16, 22, 4, iron);
            // Leg highlights
            FillRoundedRect(t, cx - 22, 30, 6, 8, 2, ironL);
            FillRoundedRect(t, cx + 10, 30, 6, 8, 2, ironL);
            // Feet
            FillRoundedRect(t, cx - 28, 12, 22, 8, 3, ironD);
            FillRoundedRect(t, cx + 6,  12, 22, 8, 3, ironD);

            // Torso — broad rectangle with rounded top
            FillRoundedRect(t, cx - 28, 38, 56, 50, 6, iron);
            // Chest plate (lighter)
            FillRoundedRect(t, cx - 20, 50, 40, 30, 4, ironL);
            // Inner chest highlight
            FillRoundedRect(t, cx - 16, 60, 32, 16, 3, ironVL);
            // Center groove (darker)
            FillRect(t, cx - 1, 50, 2, 30, ironD);

            // Shoulder pauldrons
            FillRoundedRect(t, cx - 36, 78, 14, 14, 3, ironL);
            FillRoundedRect(t, cx + 22, 78, 14, 14, 3, ironL);
            FillRoundedRect(t, cx - 34, 86, 8, 5, 2, ironVL);
            FillRoundedRect(t, cx + 26, 86, 8, 5, 2, ironVL);

            // Arms hanging
            FillRoundedRect(t, cx - 38, 50, 10, 28, 3, iron);
            FillRoundedRect(t, cx + 28, 50, 10, 28, 3, iron);
            // Big square fists
            FillRoundedRect(t, cx - 42, 38, 14, 14, 3, ironD);
            FillRoundedRect(t, cx + 28, 38, 14, 14, 3, ironD);
            // Knuckle rivets
            FillCircle(t, cx - 38, 44, 1.2f, rivet);
            FillCircle(t, cx - 35, 47, 1.2f, rivet);
            FillCircle(t, cx + 32, 44, 1.2f, rivet);
            FillCircle(t, cx + 35, 47, 1.2f, rivet);

            // Head — square helmet
            FillRoundedRect(t, cx - 16, 88, 32, 26, 4, iron);
            // Helmet top highlight
            FillRoundedRect(t, cx - 14, 106, 28, 6, 3, ironL);
            // Eye slits
            FillRoundedRect(t, cx - 10, 96, 7, 3, 1, RGBA(0xFF, 0x17, 0x44, 0.6f));
            FillRoundedRect(t, cx + 3,  96, 7, 3, 1, RGBA(0xFF, 0x17, 0x44, 0.6f));
            FillRect(t, cx - 9, 97, 6, 2, eyeSlit);
            FillRect(t, cx + 4, 97, 6, 2, eyeSlit);
            // Mouth grille
            for (int i = 0; i < 6; i++) FillRect(t, cx - 8 + i * 3, 90, 1, 4, ironD);

            // Rivets across chest
            int[] rxs = { cx - 16, cx - 8, cx + 8, cx + 16 };
            int[] rys = { 60, 78 };
            foreach (int rx in rxs) foreach (int ry in rys) {
                FillCircle(t, rx, ry, 1.5f, rivet);
                Set(t, rx, ry + 1, Color.white);
            }

            AddOuterGlow(t, RGBA(0xFF, 0x17, 0x44, 0.85f), 3);
            return t;
        }

        private static Texture2D MakeEnemyBoss()
        {
            // The Dread Colossus — massive bipedal creature with horns and arcane chest rune.
            int N = BOSS;
            var t = NewTex(N, N);
            int cx = N / 2;

            Color black   = RGB(0x0D, 0x0D, 0x0D);
            Color charcoal= RGB(0x1A, 0x1A, 0x20);
            Color hornD   = RGB(0x4A, 0x14, 0x8C);
            Color hornL   = RGB(0xCE, 0x93, 0xD8);
            Color rune    = RGB(0xE0, 0x40, 0xFB);
            Color eyeWhite= Color.white;
            Color shadow  = RGBA(0, 0, 0, 0.6f);

            // Ground shadow
            FillEllipse(t, cx, 28, 70, 14, shadow);

            // Aura halos (outer dark mist)
            for (int r = 110; r > 70; r -= 6)
            {
                FillCircle(t, cx, 130, r, RGBA(0x4A, 0x14, 0x8C, 0.06f));
            }

            // Legs — heavy columns
            FillRoundedRect(t, cx - 50, 28, 32, 50, 8, charcoal);
            FillRoundedRect(t, cx + 18, 28, 32, 50, 8, charcoal);
            FillRoundedRect(t, cx - 46, 56, 8, 16, 3, RGB(0x2A, 0x2A, 0x35)); // leg highlights
            FillRoundedRect(t, cx + 22, 56, 8, 16, 3, RGB(0x2A, 0x2A, 0x35));
            // Feet — flared bases with claws
            FillRoundedRect(t, cx - 56, 22, 44, 14, 6, black);
            FillRoundedRect(t, cx + 12, 22, 44, 14, 6, black);
            for (int i = 0; i < 3; i++)
            {
                FillTriangle(t,
                    new Vector2(cx - 55 + i * 14, 22),
                    new Vector2(cx - 49 + i * 14, 22),
                    new Vector2(cx - 52 + i * 14, 14),
                    hornD);
                FillTriangle(t,
                    new Vector2(cx + 13 + i * 14, 22),
                    new Vector2(cx + 19 + i * 14, 22),
                    new Vector2(cx + 16 + i * 14, 14),
                    hornD);
            }

            // Massive torso
            FillRoundedRect(t, cx - 60, 78, 120, 100, 14, black);
            // Chest highlight
            FillRoundedRect(t, cx - 40, 110, 80, 50, 8, charcoal);
            // Spine / center seam
            FillRect(t, cx - 2, 78, 4, 100, RGB(0x05, 0x05, 0x08));

            // Arms — huge with shoulder pauldrons
            FillRoundedRect(t, cx - 84, 100, 30, 70, 10, charcoal);
            FillRoundedRect(t, cx + 54, 100, 30, 70, 10, charcoal);
            FillRoundedRect(t, cx - 88, 158, 38, 24, 8, black); // pauldron L
            FillRoundedRect(t, cx + 50, 158, 38, 24, 8, black); // pauldron R
            // Pauldron spikes
            for (int i = 0; i < 3; i++)
            {
                FillTriangle(t,
                    new Vector2(cx - 80 + i * 12, 182),
                    new Vector2(cx - 74 + i * 12, 182),
                    new Vector2(cx - 77 + i * 12, 192),
                    hornD);
                FillTriangle(t,
                    new Vector2(cx + 56 + i * 12, 182),
                    new Vector2(cx + 62 + i * 12, 182),
                    new Vector2(cx + 59 + i * 12, 192),
                    hornD);
            }
            // Fists (huge)
            FillRoundedRect(t, cx - 90, 80, 38, 28, 6, RGB(0x05, 0x05, 0x05));
            FillRoundedRect(t, cx + 52, 80, 38, 28, 6, RGB(0x05, 0x05, 0x05));

            // Glowing arcane chest rune (pentagram shape via triangles + center circle)
            int rcx = cx, rcy = 130;
            FillCircle(t, rcx, rcy, 22, RGBA(0xE0, 0x40, 0xFB, 0.4f));
            FillCircle(t, rcx, rcy, 14, RGBA(0xE0, 0x40, 0xFB, 0.7f));
            // 5-point star
            float runeR = 16f;
            Vector2[] star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? runeR : runeR * 0.45f;
                star[i] = new Vector2(rcx + Mathf.Cos(a) * r, rcy + Mathf.Sin(a) * r);
            }
            FillPolygon(t, star, rune);
            FillCircle(t, rcx, rcy, 4, Color.white);

            // Head — looming, jaw-jutting
            FillRoundedRect(t, cx - 36, 178, 72, 60, 10, black);
            // Brow ridge
            FillRoundedRect(t, cx - 32, 220, 64, 8, 4, charcoal);
            // White-hot eyes
            FillCircle(t, cx - 14, 210, 8, RGBA(0xE0, 0x40, 0xFB, 0.6f));
            FillCircle(t, cx + 14, 210, 8, RGBA(0xE0, 0x40, 0xFB, 0.6f));
            FillCircle(t, cx - 14, 210, 5, eyeWhite);
            FillCircle(t, cx + 14, 210, 5, eyeWhite);
            FillCircle(t, cx - 13, 211, 1.5f, RGB(0xFF, 0xFF, 0xFF));
            FillCircle(t, cx + 15, 211, 1.5f, RGB(0xFF, 0xFF, 0xFF));
            // Mouth — fanged
            FillRect(t, cx - 18, 188, 36, 6, RGB(0x05, 0x05, 0x05));
            for (int i = 0; i < 5; i++)
            {
                FillTriangle(t,
                    new Vector2(cx - 16 + i * 8, 194),
                    new Vector2(cx - 12 + i * 8, 194),
                    new Vector2(cx - 14 + i * 8, 188),
                    Color.white);
            }

            // Two big curved horns
            for (int i = 0; i < 30; i++)
            {
                float u = i / 29f;
                float xL = cx - 28 - i * 1.2f;
                float yL = 234 + i * 1.4f;
                float xR = cx + 28 + i * 1.2f;
                float yR = 234 + i * 1.4f;
                float w = Mathf.Lerp(8, 1, u);
                FillCircle(t, xL, yL, w, Color.Lerp(hornD, hornL, u * u));
                FillCircle(t, xR, yR, w, Color.Lerp(hornD, hornL, u * u));
            }

            // Heavy purple outer glow
            AddOuterGlow(t, RGBA(0xE0, 0x40, 0xFB, 0.95f), 6);
            return t;
        }

        // ====================================================================
        // Towers — 3/4 perspective with glowing accents
        // ====================================================================
        private static Texture2D MakeTowerBasic(bool upgraded)
        {
            var t = NewTex(TOWER, TOWER);
            int cx = TOWER / 2;
            Color stoneT = RGB(0x55, 0x6E, 0x7A);
            Color stoneM = RGB(0x37, 0x47, 0x4F);
            Color stoneD = RGB(0x1C, 0x26, 0x2C);
            Color wood   = RGB(0x4E, 0x34, 0x2E);
            Color woodL  = RGB(0x6D, 0x4C, 0x41);
            Color band   = RGB(0x90, 0xA4, 0xAE);
            Color glow   = RGB(0x44, 0x8A, 0xFF);
            Color gold   = RGB(0xFF, 0xD7, 0x00);

            // Ground shadow
            FillEllipse(t, cx, 14, 36, 9, RGBA(0, 0, 0, 0.5f));

            // Stone platform — squat trapezoid (3/4 perspective)
            FillPolygon(t, new[] {
                new Vector2(cx - 38, 14), new Vector2(cx + 38, 14),
                new Vector2(cx + 32, 38), new Vector2(cx - 32, 38)
            }, stoneM);
            // Top face highlight
            FillPolygon(t, new[] {
                new Vector2(cx - 30, 36), new Vector2(cx + 30, 36),
                new Vector2(cx + 26, 42), new Vector2(cx - 26, 42)
            }, stoneT);
            // Front edge shadow
            FillPolygon(t, new[] {
                new Vector2(cx - 38, 14), new Vector2(cx - 32, 38),
                new Vector2(cx - 32, 22), new Vector2(cx - 38, 14)
            }, stoneD);

            // Two vertical wooden beams
            FillRoundedRect(t, cx - 12, 42, 8, 60, 2, wood);
            FillRoundedRect(t, cx + 4,  42, 8, 60, 2, wood);
            FillRect(t, cx - 11, 42, 2, 60, woodL);
            FillRect(t, cx + 5,  42, 2, 60, woodL);

            // Metal bands wrapping the beams
            for (int by = 60; by <= 92; by += 14)
            {
                FillRoundedRect(t, cx - 14, by, 28, 4, 1, band);
                Set(t, cx - 13, by + 3, Color.white);
                Set(t, cx + 12, by + 3, Color.white);
            }

            // Glowing blue orb at the top
            FillCircle(t, cx, 108, 10, RGBA(0x44, 0x8A, 0xFF, 0.6f));
            FillCircle(t, cx, 108, 7, glow);
            FillCircle(t, cx - 2, 110, 2.5f, Color.white);
            // Crystal rays (4 short lines)
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI / 4f;
                Vector2 p0 = new Vector2(cx + Mathf.Cos(a) * 9, 108 + Mathf.Sin(a) * 9);
                Vector2 p1 = new Vector2(cx + Mathf.Cos(a) * 14, 108 + Mathf.Sin(a) * 14);
                DrawLineThick(t, p0, p1, 1.5f, RGBA(0x44, 0x8A, 0xFF, 0.9f));
            }

            if (upgraded)
            {
                // Gold trim along the platform top
                FillRect(t, cx - 30, 42, 60, 2, gold);
                FillRect(t, cx - 26, 36, 52, 2, gold);
                AddOuterGlow(t, RGBA(0x44, 0x8A, 0xFF, 0.95f), 4);
            }
            else
            {
                AddOuterGlow(t, RGBA(0x44, 0x8A, 0xFF, 0.7f), 2);
            }
            return t;
        }

        private static Texture2D MakeTowerSniper(bool upgraded)
        {
            var t = NewTex(TOWER, TOWER);
            int cx = TOWER / 2;
            Color hex   = RGB(0x21, 0x21, 0x21);
            Color hexL  = RGB(0x40, 0x40, 0x40);
            Color obsid = RGB(0x1A, 0x23, 0x7E);
            Color obsiL = RGB(0x32, 0x42, 0xB0);
            Color crystal = RGB(0x00, 0xE5, 0xFF);
            Color rune  = RGB(0x00, 0xBC, 0xD4);
            Color gold  = RGB(0xFF, 0xD7, 0x00);

            FillEllipse(t, cx, 12, 32, 7, RGBA(0, 0, 0, 0.5f));

            // Hex platform (top-down view)
            Vector2[] hex1 = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                hex1[i] = new Vector2(cx + Mathf.Cos(a) * 32, 26 + Mathf.Sin(a) * 14);
            }
            FillPolygon(t, hex1, hex);
            // Inner hex highlight
            Vector2[] hex2 = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                hex2[i] = new Vector2(cx + Mathf.Cos(a) * 24, 26 + Mathf.Sin(a) * 10);
            }
            FillPolygon(t, hex2, hexL);

            // Tall narrow spire — triangle
            FillPolygon(t, new[] {
                new Vector2(cx - 14, 30), new Vector2(cx + 14, 30),
                new Vector2(cx, 110)
            }, obsid);
            // Spine highlight
            FillPolygon(t, new[] {
                new Vector2(cx - 4, 30), new Vector2(cx, 30),
                new Vector2(cx, 108)
            }, obsiL);

            // Cyan rune marks
            for (int i = 0; i < 2; i++)
            {
                int ry = 50 + i * 22;
                FillRect(t, cx - 6, ry, 12, 2, rune);
                FillRect(t, cx - 2, ry - 4, 4, 10, rune);
            }

            // Crystal at the tip with extending glow rays
            FillCircle(t, cx, 110, 12, RGBA(0x00, 0xE5, 0xFF, 0.55f));
            // Diamond crystal
            FillPolygon(t, new[] {
                new Vector2(cx - 6, 110), new Vector2(cx, 122),
                new Vector2(cx + 6, 110), new Vector2(cx, 100)
            }, crystal);
            FillTriangle(t, new Vector2(cx - 4, 110), new Vector2(cx, 118), new Vector2(cx, 110), Color.white);
            // Rays
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector2 p0 = new Vector2(cx + Mathf.Cos(a) * 14, 110 + Mathf.Sin(a) * 14);
                Vector2 p1 = new Vector2(cx + Mathf.Cos(a) * 22, 110 + Mathf.Sin(a) * 22);
                DrawLineThick(t, p0, p1, 1.4f, RGBA(0x00, 0xE5, 0xFF, 0.85f));
            }

            if (upgraded)
            {
                FillRect(t, cx - 30, 26, 60, 2, gold);
                AddOuterGlow(t, RGBA(0x00, 0xE5, 0xFF, 0.95f), 5);
            }
            else AddOuterGlow(t, RGBA(0x00, 0xE5, 0xFF, 0.75f), 3);
            return t;
        }

        private static Texture2D MakeTowerSlow(bool upgraded)
        {
            var t = NewTex(TOWER, TOWER);
            int cx = TOWER / 2;
            Color iceB  = RGB(0x78, 0x90, 0x9C);
            Color iceL  = RGB(0xCF, 0xD8, 0xDC);
            Color dome  = RGB(0xB3, 0xE5, 0xFC);
            Color domeL = RGB(0xE1, 0xF5, 0xFE);
            Color glow  = RGB(0x80, 0xDE, 0xEA);
            Color gold  = RGB(0xFF, 0xD7, 0x00);

            FillEllipse(t, cx, 14, 38, 8, RGBA(0, 0, 0, 0.5f));

            // Round icy platform
            FillEllipse(t, cx, 28, 40, 12, iceB);
            FillEllipse(t, cx, 30, 36, 8, iceL);

            // Icicles hanging from edge
            for (int i = -2; i <= 2; i += 2)
            {
                int ix = cx + i * 14;
                FillTriangle(t,
                    new Vector2(ix - 3, 22),
                    new Vector2(ix + 3, 22),
                    new Vector2(ix, 6),
                    domeL);
                Set(t, ix, 9, Color.white);
            }

            // Dome — bright pale-blue, rounded
            FillEllipse(t, cx, 64, 30, 30, dome);
            FillEllipse(t, cx - 8, 78, 14, 10, domeL);
            // Dome rim band
            FillRect(t, cx - 30, 40, 60, 4, iceB);

            // Snowflake on dome face (6-spoke)
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector2 p0 = new Vector2(cx, 64);
                Vector2 p1 = new Vector2(cx + Mathf.Cos(a) * 16, 64 + Mathf.Sin(a) * 16);
                DrawLineThick(t, p0, p1, 1.6f, Color.white);
                // Mid branches
                Vector2 m = Vector2.Lerp(p0, p1, 0.6f);
                Vector2 b1 = m + new Vector2(Mathf.Cos(a + Mathf.PI / 3f), Mathf.Sin(a + Mathf.PI / 3f)) * 4f;
                Vector2 b2 = m + new Vector2(Mathf.Cos(a - Mathf.PI / 3f), Mathf.Sin(a - Mathf.PI / 3f)) * 4f;
                DrawLineThick(t, m, b1, 1.2f, Color.white);
                DrawLineThick(t, m, b2, 1.2f, Color.white);
            }
            FillCircle(t, cx, 64, 2.5f, Color.white);

            // Top finial
            FillCircle(t, cx, 96, 5, dome);
            FillCircle(t, cx, 96, 2, Color.white);

            if (upgraded)
            {
                FillRect(t, cx - 28, 42, 56, 2, gold);
                AddOuterGlow(t, RGBA(0x80, 0xDE, 0xEA, 0.95f), 5);
            }
            else AddOuterGlow(t, RGBA(0x80, 0xDE, 0xEA, 0.75f), 3);
            return t;
        }

        private static Texture2D MakeTowerArea(bool upgraded)
        {
            var t = NewTex(TOWER, TOWER);
            int cx = TOWER / 2;
            Color stone = RGB(0x3E, 0x27, 0x23);
            Color stoneL= RGB(0x5D, 0x40, 0x37);
            Color iron  = RGB(0x4E, 0x34, 0x2E);
            Color ironL = RGB(0x6D, 0x4C, 0x41);
            Color flame = RGB(0xFF, 0x6D, 0x00);
            Color flameH= RGB(0xFF, 0xC1, 0x07);
            Color gold  = RGB(0xFF, 0xD7, 0x00);

            FillEllipse(t, cx, 14, 40, 10, RGBA(0, 0, 0, 0.5f));

            // Heavy stone platform — wider than tall
            FillPolygon(t, new[] {
                new Vector2(cx - 44, 14), new Vector2(cx + 44, 14),
                new Vector2(cx + 36, 40), new Vector2(cx - 36, 40)
            }, stone);
            FillPolygon(t, new[] {
                new Vector2(cx - 34, 38), new Vector2(cx + 34, 38),
                new Vector2(cx + 30, 44), new Vector2(cx - 30, 44)
            }, stoneL);

            // Stubby barrel — thick cylinder
            FillRoundedRect(t, cx - 22, 44, 44, 50, 6, iron);
            // Barrel light side
            FillRoundedRect(t, cx + 8, 50, 12, 38, 4, ironL);
            // Barrel rim
            FillRect(t, cx - 22, 92, 44, 4, RGB(0x21, 0x16, 0x10));

            // Glowing molten mouth
            FillEllipse(t, cx, 96, 16, 6, RGBA(0xFF, 0x6D, 0x00, 0.5f));
            FillEllipse(t, cx, 96, 12, 4, flame);
            FillEllipse(t, cx, 95, 6, 2, flameH);

            // Heat shimmer above
            for (int i = -1; i <= 1; i++)
            {
                int sx = cx + i * 12;
                int baseY = 100;
                for (int sy = 0; sy < 16; sy += 2)
                {
                    int wob = (sy / 2 % 2 == 0) ? 0 : 1;
                    Set(t, sx + wob, baseY + sy, RGBA(0xFF, 0x8F, 0x00, 0.7f));
                    Set(t, sx + wob + 1, baseY + sy + 1, RGBA(0xFF, 0x8F, 0x00, 0.5f));
                }
            }

            if (upgraded)
            {
                FillRect(t, cx - 38, 38, 76, 2, gold);
                AddOuterGlow(t, RGBA(0xFF, 0x6D, 0x00, 0.95f), 5);
            }
            else AddOuterGlow(t, RGBA(0xFF, 0x6D, 0x00, 0.75f), 3);
            return t;
        }

        // ====================================================================
        // Projectiles
        // ====================================================================
        private static Texture2D MakeProjectileBullet()
        {
            var t = NewTex(PROJ, PROJ);
            float c = PROJ * 0.5f;
            // Outer glow
            FillCircle(t, c, c, 14, RGBA(0x44, 0x8A, 0xFF, 0.25f));
            FillCircle(t, c, c, 11, RGBA(0x82, 0xB1, 0xFF, 0.45f));
            // Body
            FillEllipse(t, c, c, 7, 7, RGB(0x44, 0x8A, 0xFF));
            // Hot core
            FillCircle(t, c, c, 4, Color.white);
            // Tail streak
            for (int i = 0; i < 6; i++)
                FillCircle(t, c - i * 0.8f, c, 2.5f - i * 0.3f, RGBA(0x82, 0xB1, 0xFF, 0.4f - i * 0.05f));
            return t;
        }

        private static Texture2D MakeProjectileLaser()
        {
            var t = NewTex(LASER_W, LASER_H);
            int cx = LASER_W / 2;
            // Glow band
            for (int y = 4; y < LASER_H - 4; y++)
            {
                Set(t, cx - 2, y, RGBA(0x00, 0xE5, 0xFF, 0.5f));
                Set(t, cx + 2, y, RGBA(0x00, 0xE5, 0xFF, 0.5f));
                Set(t, cx - 1, y, RGB(0x00, 0xE5, 0xFF));
                Set(t, cx + 1, y, RGB(0x00, 0xE5, 0xFF));
                Set(t, cx,     y, Color.white);
            }
            // Tapered ends
            for (int i = 0; i < 4; i++)
            {
                FillCircle(t, cx, 2 + i, 1.5f - i * 0.3f, RGBA(0x00, 0xE5, 0xFF, 1f - i * 0.2f));
                FillCircle(t, cx, LASER_H - 3 - i, 1.5f - i * 0.3f, RGBA(0x00, 0xE5, 0xFF, 1f - i * 0.2f));
            }
            return t;
        }

        private static Texture2D MakeProjectileSlow()
        {
            var t = NewTex(PROJ, PROJ);
            float c = PROJ * 0.5f;
            FillCircle(t, c, c, 14, RGBA(0xB3, 0xE5, 0xFC, 0.25f));
            // 6-pointed snowflake
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector2 p0 = new Vector2(c, c);
                Vector2 p1 = new Vector2(c + Mathf.Cos(a) * 11, c + Mathf.Sin(a) * 11);
                DrawLineThick(t, p0, p1, 2f, RGB(0xB3, 0xE5, 0xFC));
                // Mid branches
                Vector2 mid = Vector2.Lerp(p0, p1, 0.55f);
                Vector2 b1 = mid + new Vector2(Mathf.Cos(a + Mathf.PI/3), Mathf.Sin(a + Mathf.PI/3)) * 3.5f;
                Vector2 b2 = mid + new Vector2(Mathf.Cos(a - Mathf.PI/3), Mathf.Sin(a - Mathf.PI/3)) * 3.5f;
                DrawLineThick(t, mid, b1, 1.4f, RGB(0xE1, 0xF5, 0xFE));
                DrawLineThick(t, mid, b2, 1.4f, RGB(0xE1, 0xF5, 0xFE));
            }
            FillCircle(t, c, c, 3, Color.white);
            // Sparkles
            Set(t, 4, 4, Color.white); Set(t, PROJ - 5, 4, Color.white);
            Set(t, 4, PROJ - 5, Color.white); Set(t, PROJ - 5, PROJ - 5, Color.white);
            return t;
        }

        private static Texture2D MakeProjectileAOE()
        {
            var t = NewTex(PROJ, PROJ);
            float c = PROJ * 0.5f;
            // Outer glow
            FillCircle(t, c, c, 14, RGBA(0xFF, 0x6D, 0x00, 0.3f));
            // Rocky body
            FillCircle(t, c, c, 11, RGB(0x3E, 0x27, 0x23));
            FillCircle(t, c - 2, c + 1, 9, RGB(0x5D, 0x40, 0x37));
            // Bright cracks
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 0.6f + 0.5f;
                Vector2 p0 = new Vector2(c + Mathf.Cos(a) * 7, c + Mathf.Sin(a) * 7);
                Vector2 p1 = new Vector2(c - Mathf.Cos(a) * 7, c - Mathf.Sin(a) * 7);
                DrawLineThick(t, p0, p1, 1.5f, RGB(0xFF, 0x6D, 0x00));
            }
            // Hot core spots
            FillCircle(t, c - 3, c, 1.5f, RGB(0xFF, 0xC1, 0x07));
            FillCircle(t, c + 3, c + 2, 1f, RGB(0xFF, 0xC1, 0x07));
            return t;
        }

        // ====================================================================
        // Tilemap
        // ====================================================================
        private static Texture2D MakeTileGrass()
        {
            var t = NewTex(TILE, TILE);
            VerticalGradient(t, 0, 0, TILE, TILE, RGB(0x14, 0x22, 0x14), RGB(0x1F, 0x32, 0x1F));
            var rng = new System.Random(11);
            // Soft tufts
            for (int i = 0; i < 70; i++)
            {
                int x = rng.Next(0, TILE), y = rng.Next(0, TILE);
                Set(t, x, y, RGBA(0x2E, 0x7D, 0x32, 0.85f));
                if (rng.NextDouble() < 0.3) Set(t, x + 1, y, RGB(0x4C, 0xAF, 0x50));
            }
            // Rare flower specks
            for (int i = 0; i < 5; i++)
            {
                int x = rng.Next(2, TILE - 2), y = rng.Next(2, TILE - 2);
                Set(t, x, y, RGB(0xF5, 0xA6, 0x23));
            }
            return t;
        }

        private static Texture2D MakeTilePath()
        {
            var t = NewTex(TILE, TILE);
            // Base brown
            FillRect(t, 0, 0, TILE, TILE, RGB(0x2D, 0x25, 0x20));
            // 3x3 cobblestone pattern
            for (int gy = 0; gy < 3; gy++)
            for (int gx = 0; gx < 3; gx++)
            {
                int x0 = gx * 21 + 1;
                int y0 = gy * 21 + 1;
                int w = 19, h = 19;
                FillRoundedRect(t, x0, y0, w, h, 4, RGB(0x3E, 0x27, 0x23));
                // Top-left highlight
                FillRoundedRect(t, x0 + 1, y0 + 12, w - 4, 5, 2, RGB(0x52, 0x35, 0x2C));
                // Inner shadow
                FillRect(t, x0 + 2, y0 + 2, w - 4, 2, RGB(0x1A, 0x12, 0x10));
            }
            // Wear scratches
            var rng = new System.Random(5);
            for (int i = 0; i < 12; i++)
            {
                int x = rng.Next(0, TILE), y = rng.Next(0, TILE);
                Set(t, x, y, RGB(0x52, 0x35, 0x2C));
            }
            return t;
        }

        private static Texture2D MakeTileRock()
        {
            var t = NewTex(TILE, TILE);
            VerticalGradient(t, 0, 0, TILE, TILE, RGB(0x14, 0x1E, 0x22), RGB(0x26, 0x32, 0x38));
            // Boulder
            FillEllipse(t, TILE / 2f, TILE / 2f + 2, 22, 18, RGB(0x45, 0x5A, 0x64));
            FillEllipse(t, TILE / 2f - 4, TILE / 2f + 6, 12, 8, RGB(0x78, 0x90, 0x9C));
            // Cracks
            DrawLineThick(t, new Vector2(20, 36), new Vector2(28, 30), 1f, RGB(0x12, 0x1A, 0x1E));
            DrawLineThick(t, new Vector2(40, 30), new Vector2(46, 26), 1f, RGB(0x12, 0x1A, 0x1E));
            // Moss
            FillCircle(t, 18, 22, 2.5f, RGB(0x1B, 0x5E, 0x20));
            FillCircle(t, 48, 24, 2f, RGB(0x1B, 0x5E, 0x20));
            return t;
        }

        private static Texture2D MakeTileWater(int frame)
        {
            var t = NewTex(TILE, TILE);
            VerticalGradient(t, 0, 0, TILE, TILE, RGB(0x06, 0x12, 0x1F), RGB(0x10, 0x22, 0x36));
            int offset = frame * 8;
            for (int r = 0; r < 4; r++)
            {
                int cxc = (12 + offset + r * 19) % TILE;
                int cyc = (20 + offset / 2 + r * 14) % TILE;
                StrokeCircle(t, cxc, cyc, 8 + r, 1.4f, RGBA(0x15, 0x65, 0xC0, 0.6f));
            }
            // Specular dot highlights
            var rng = new System.Random(frame * 17 + 3);
            for (int i = 0; i < 4; i++)
            {
                int x = rng.Next(0, TILE), y = rng.Next(0, TILE);
                Set(t, x, y, RGBA(255, 255, 255, 0.3f));
            }
            return t;
        }

        private static Texture2D MakeFlag(bool start)
        {
            var t = NewTex(FLAG, FLAG);
            int cx = FLAG / 2;
            // Pole
            FillRect(t, cx - 1, 8, 3, 48, RGB(0x5D, 0x40, 0x37));
            // Pole top knob
            FillCircle(t, cx + 1, 56, 3, RGB(0xF5, 0xA6, 0x23));
            // Triangular flag
            Color flagC = start ? RGB(0x76, 0xFF, 0x03) : RGB(0xFF, 0x17, 0x44);
            Color flagD = start ? RGB(0x33, 0x69, 0x33) : RGB(0x88, 0x10, 0x21);
            FillTriangle(t,
                new Vector2(cx + 2, 36),
                new Vector2(cx + 2, 56),
                new Vector2(cx + 28, 46),
                flagC);
            // Edge shadow
            FillTriangle(t,
                new Vector2(cx + 22, 47),
                new Vector2(cx + 28, 46),
                new Vector2(cx + 22, 45),
                flagD);
            // Symbol — small white S or skull dot
            FillCircle(t, cx + 13, 46, 2.5f, Color.white);
            if (start)
            {
                FillRect(t, cx + 11, 45, 5, 1, flagC);
                FillRect(t, cx + 11, 47, 5, 1, flagC);
            }
            else
            {
                Set(t, cx + 12, 47, Color.black);
                Set(t, cx + 14, 47, Color.black);
            }
            // Ground shadow
            FillEllipse(t, cx, 6, 10, 3, RGBA(0, 0, 0, 0.4f));
            return t;
        }

        // ====================================================================
        // UI
        // ====================================================================
        private static Texture2D MakePanel(Color fill, Color border)
        {
            var t = NewTex(UI_PANEL, UI_PANEL);
            // Base rounded rect with subtle vertical gradient
            int r = 14;
            // Fill body with gradient
            for (int y = 0; y < UI_PANEL; y++)
            {
                float u = y / (float)(UI_PANEL - 1);
                Color c = Color.Lerp(Darken(fill, 0.04f), Brighten(fill, 0.05f), u);
                for (int x = 0; x < UI_PANEL; x++)
                {
                    float d = SignedDistanceRoundedRect(x + 0.5f, y + 0.5f, UI_PANEL, UI_PANEL, r);
                    if (d <= -0.5f) Blend(t, x, y, c);
                    else if (d <= 0f) Blend(t, x, y, new Color(c.r, c.g, c.b, 0.5f));
                }
            }
            // Inner border ring
            StrokeRoundedRect(t, 0, 0, UI_PANEL, UI_PANEL, r, 2, border);
            // Inner highlight (1px inside the border)
            StrokeRoundedRect(t, 3, 3, UI_PANEL - 6, UI_PANEL - 6, r - 3, 1, RGBA(255, 255, 255, 0.08f));
            return t;
        }

        private static Texture2D MakeButton(Color fill, Color topHi, Color botLo)
        {
            var t = NewTex(UI_BTN_W, UI_BTN_H);
            int r = 12;
            // Multi-stop vertical gradient: shadow → fill → highlight band
            for (int y = 0; y < UI_BTN_H; y++)
            {
                float u = y / (float)(UI_BTN_H - 1);
                Color c;
                if (u < 0.15f) c = Color.Lerp(Darken(fill, 0.15f), fill, u / 0.15f);
                else if (u > 0.85f) c = Color.Lerp(fill, topHi, (u - 0.85f) / 0.15f);
                else c = Color.Lerp(fill, Brighten(fill, 0.05f), (u - 0.15f) / 0.7f);
                for (int x = 0; x < UI_BTN_W; x++)
                {
                    float d = SignedDistanceRoundedRect(x + 0.5f, y + 0.5f, UI_BTN_W, UI_BTN_H, r);
                    if (d <= -0.5f) Blend(t, x, y, c);
                    else if (d <= 0f) Blend(t, x, y, new Color(c.r, c.g, c.b, 0.5f));
                }
            }
            // Crisp top highlight line
            for (int x = r; x < UI_BTN_W - r; x++) Blend(t, x, UI_BTN_H - 3, RGBA(255, 255, 255, 0.3f));
            // Crisp bottom shadow line
            for (int x = r; x < UI_BTN_W - r; x++) Blend(t, x, 1, new Color(botLo.r, botLo.g, botLo.b, 0.6f));
            // Slight outer glow
            AddDropShadow(t, 0, -2, 1, RGBA(0, 0, 0, 0.35f));
            return t;
        }

        private static Texture2D MakeCoinIcon()
        {
            var t = NewTex(UI_ICON, UI_ICON);
            float c = UI_ICON * 0.5f;
            // Outer rim ring
            FillCircle(t, c, c, 30, RGB(0xC9, 0x84, 0x16));
            // Body
            FillCircle(t, c, c, 26, RGB(0xFF, 0xD7, 0x00));
            // Inner face
            FillCircle(t, c, c, 21, RGB(0xFF, 0xF1, 0x76));
            // $ symbol
            FillRect(t, (int)c - 1, (int)c - 14, 3, 28, RGB(0xC9, 0x84, 0x16));
            FillRect(t, (int)c - 8, (int)c - 11, 16, 3, RGB(0xC9, 0x84, 0x16));
            FillRect(t, (int)c - 8, (int)c + 8,  16, 3, RGB(0xC9, 0x84, 0x16));
            // Highlight gleam upper-left
            FillEllipse(t, c - 8, c + 8, 6, 4, RGBA(255, 255, 255, 0.7f));
            // Outer glow
            AddOuterGlow(t, RGBA(0xFF, 0xD7, 0x00, 0.8f), 2);
            return t;
        }

        private static Texture2D MakeGemIcon()
        {
            var t = NewTex(UI_ICON, UI_ICON);
            int c = UI_ICON / 2;
            Color cy   = RGB(0x00, 0xE5, 0xFF);
            Color cyD  = RGB(0x00, 0x97, 0xA7);
            Color cyL  = RGB(0xB2, 0xEB, 0xF2);

            // Diamond shape via 4 triangles
            FillPolygon(t, new[] {
                new Vector2(c, c + 22),
                new Vector2(c + 18, c + 6),
                new Vector2(c, c - 22),
                new Vector2(c - 18, c + 6)
            }, cy);
            // Top-left facet (lighter)
            FillPolygon(t, new[] {
                new Vector2(c, c - 22),
                new Vector2(c, c + 6),
                new Vector2(c - 18, c + 6)
            }, cyL);
            // Bottom-right facet (darker)
            FillPolygon(t, new[] {
                new Vector2(c, c - 22),
                new Vector2(c + 18, c + 6),
                new Vector2(c, c + 6)
            }, Brighten(cy, -0.05f));
            // Bottom facet
            FillPolygon(t, new[] {
                new Vector2(c, c + 22),
                new Vector2(c + 18, c + 6),
                new Vector2(c, c + 6)
            }, cyD);
            FillPolygon(t, new[] {
                new Vector2(c, c + 22),
                new Vector2(c, c + 6),
                new Vector2(c - 18, c + 6)
            }, Brighten(cyD, 0.05f));
            // Specular gleam
            FillRect(t, c - 8, c, 4, 2, Color.white);
            Set(t, c - 4, c + 2, Color.white);
            // Glow halo
            AddOuterGlow(t, RGBA(0x00, 0xE5, 0xFF, 0.85f), 3);
            return t;
        }

        private static Texture2D MakeHeartIcon()
        {
            var t = NewTex(UI_ICON, UI_ICON);
            float c = UI_ICON / 2f;
            Color red  = RGB(0xFF, 0x17, 0x44);
            Color redL = RGB(0xFF, 0x52, 0x52);
            Color redD = RGB(0xC2, 0x18, 0x5B);
            // Two lobes
            FillCircle(t, c - 10, c + 8, 11, red);
            FillCircle(t, c + 10, c + 8, 11, red);
            // Bottom triangle point
            FillTriangle(t,
                new Vector2(c - 18, c + 4),
                new Vector2(c + 18, c + 4),
                new Vector2(c, c - 22),
                red);
            // Highlight on upper-left lobe
            FillCircle(t, c - 13, c + 12, 4, redL);
            // Bottom shadow tip
            FillTriangle(t,
                new Vector2(c - 4, c - 18),
                new Vector2(c + 4, c - 18),
                new Vector2(c, c - 22),
                redD);
            AddOuterGlow(t, RGBA(0xFF, 0x17, 0x44, 0.8f), 2);
            return t;
        }

        private static Texture2D MakeStar(bool full)
        {
            var t = NewTex(64, 64);
            float cx = 32, cy = 32;
            Color fill = full ? RGB(0xFF, 0xD7, 0x00) : RGB(0x42, 0x42, 0x42);
            Color rim  = full ? RGB(0xC9, 0x84, 0x16) : RGB(0x21, 0x21, 0x21);
            Color hi   = full ? RGB(0xFF, 0xF1, 0x76) : RGB(0x42, 0x42, 0x42);

            // 5-point star vertices
            Vector2[] pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? 26 : 12;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            FillPolygon(t, pts, fill);
            // Inner highlight (smaller star)
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? 18 : 8;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            if (full) FillPolygon(t, pts, hi);
            // Specular dot
            if (full) FillCircle(t, cx - 6, cy + 6, 2.5f, Color.white);
            // Outline
            for (int pass = 0; pass < 1; pass++) AddOuterGlow(t, full ? RGBA(0xFF, 0xD7, 0x00, 0.95f) : RGBA(0x21, 0x21, 0x21, 0.3f), 2);
            return t;
        }

        private static Texture2D MakeCrown()
        {
            var t = NewTex(CROWN, CROWN);
            int cx = CROWN / 2;
            Color gold  = RGB(0xFF, 0xD7, 0x00);
            Color goldD = RGB(0xC9, 0x84, 0x16);
            Color goldL = RGB(0xFF, 0xF1, 0x76);
            // Base band
            FillRoundedRect(t, cx - 18, 14, 36, 10, 2, gold);
            FillRoundedRect(t, cx - 16, 16, 32, 4, 2, goldL);
            // 3 spikes
            FillTriangle(t, new Vector2(cx - 16, 24), new Vector2(cx - 6, 24), new Vector2(cx - 11, 36), gold);
            FillTriangle(t, new Vector2(cx - 5, 24),  new Vector2(cx + 5, 24), new Vector2(cx, 40), gold);
            FillTriangle(t, new Vector2(cx + 6, 24),  new Vector2(cx + 16, 24),new Vector2(cx + 11, 36), gold);
            // Spike tips with gem dots
            FillCircle(t, cx - 11, 33, 2, RGB(0xE9, 0x45, 0x60)); // ruby
            FillCircle(t, cx, 37, 2, RGB(0x00, 0xE5, 0xFF));      // gem
            FillCircle(t, cx + 11, 33, 2, RGB(0x76, 0xFF, 0x03)); // emerald
            // Outline ring
            StrokeRoundedRect(t, cx - 18, 14, 36, 10, 2, 1, goldD);
            AddOuterGlow(t, RGBA(0xFF, 0xD7, 0x00, 0.9f), 3);
            return t;
        }

        private static Texture2D MakePowerNode()
        {
            var t = NewTex(POWERNODE, POWERNODE);
            int cx = POWERNODE / 2, cy = POWERNODE / 2;
            // Hex platform
            Vector2[] hex = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                hex[i] = new Vector2(cx + Mathf.Cos(a) * 36, cy + Mathf.Sin(a) * 36);
            }
            FillPolygon(t, hex, RGBA(0x7C, 0x4D, 0xFF, 0.85f));
            // Inner hex (brighter)
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                hex[i] = new Vector2(cx + Mathf.Cos(a) * 26, cy + Mathf.Sin(a) * 26);
            }
            FillPolygon(t, hex, RGBA(0xB3, 0x88, 0xFF, 0.8f));
            // Center rune
            FillCircle(t, cx, cy, 6, Color.white);
            FillCircle(t, cx, cy, 3, RGB(0x7C, 0x4D, 0xFF));
            AddOuterGlow(t, RGBA(0x7C, 0x4D, 0xFF, 0.95f), 5);
            return t;
        }

        private static Texture2D MakeArrow()
        {
            var t = NewTex(ARROW, ARROW);
            int c = ARROW / 2;
            // Chevron shape
            for (int i = 0; i < 12; i++)
            {
                Set(t, c + i, c + i, RGBA(0xF5, 0xA6, 0x23, 0.7f));
                Set(t, c + i, c - i, RGBA(0xF5, 0xA6, 0x23, 0.7f));
                Set(t, c + i + 1, c + i, RGBA(0xF5, 0xA6, 0x23, 0.7f));
                Set(t, c + i + 1, c - i, RGBA(0xF5, 0xA6, 0x23, 0.7f));
                Set(t, c + i + 2, c + i, RGBA(0xF5, 0xA6, 0x23, 0.5f));
                Set(t, c + i + 2, c - i, RGBA(0xF5, 0xA6, 0x23, 0.5f));
            }
            return t;
        }

        // ====================================================================
        // Background parallax
        // ====================================================================
        private static Texture2D MakeBgMountains()
        {
            var t = NewTex(BG_M_W, BG_M_H);
            // Deep night gradient
            VerticalGradient(t, 0, 0, BG_M_W, BG_M_H, RGB(0x06, 0x06, 0x10), RGB(0x0F, 0x0F, 0x1F));
            // Stars
            var rng = new System.Random(42);
            for (int i = 0; i < 200; i++)
            {
                int x = rng.Next(0, BG_M_W);
                int y = rng.Next(BG_M_H / 2, BG_M_H);
                Set(t, x, y, RGBA(255, 255, 255, 0.5f + (float)rng.NextDouble() * 0.5f));
            }
            // Far back ridge
            int prevH = 0;
            int x0 = 0;
            while (x0 < BG_M_W)
            {
                int peak = 70 + rng.Next(0, 50);
                int width = 60 + rng.Next(0, 80);
                for (int i = 0; i < width; i++)
                {
                    float u = i / (float)width;
                    int h = (int)Mathf.Lerp(prevH, peak, Mathf.SmoothStep(0, 1, u));
                    if (i > width / 2) h = (int)Mathf.Lerp(peak, prevH, Mathf.SmoothStep(0, 1, (i - width / 2f) / (width / 2f)));
                    for (int y = 0; y < h; y++) Blend(t, x0 + i, y, RGB(0x0D, 0x0D, 0x1A));
                }
                x0 += width;
                prevH = 30 + rng.Next(0, 20);
            }
            // Front ridge (slightly taller, slightly lighter)
            x0 = 0;
            while (x0 < BG_M_W)
            {
                int peak = 50 + rng.Next(0, 30);
                int width = 40 + rng.Next(0, 60);
                for (int i = 0; i < width; i++)
                {
                    float u = i / (float)width;
                    int h = (int)Mathf.Lerp(0, peak, Mathf.SmoothStep(0, 1, u));
                    if (i > width / 2) h = (int)Mathf.Lerp(peak, 0, Mathf.SmoothStep(0, 1, (i - width / 2f) / (width / 2f)));
                    for (int y = 0; y < h; y++) Blend(t, x0 + i, y, RGB(0x12, 0x12, 0x22));
                }
                x0 += width;
            }
            return t;
        }

        private static Texture2D MakeBgTrees()
        {
            var t = NewTex(BG_T_W, BG_T_H);
            VerticalGradient(t, 0, 0, BG_T_W, BG_T_H, RGB(0x08, 0x08, 0x14), RGB(0x12, 0x12, 0x22));
            var rng = new System.Random(7);
            // Tree trunks + canopies
            int x = 0;
            while (x < BG_T_W)
            {
                int trunkH = 14 + rng.Next(0, 14);
                int canopyR = 12 + rng.Next(0, 18);
                int trunkX = x + canopyR / 2;
                FillRect(t, trunkX, 0, 3, trunkH, RGB(0x0D, 0x0D, 0x18));
                FillCircle(t, trunkX + 1, trunkH + canopyR - 4, canopyR, RGB(0x11, 0x11, 0x22));
                FillCircle(t, trunkX - 4, trunkH + canopyR - 8, canopyR - 4, RGB(0x14, 0x14, 0x26));
                x += canopyR + rng.Next(0, 12);
            }
            return t;
        }

        // ====================================================================
        // I/O
        // ====================================================================
        private static readonly List<string> WrittenPaths = new List<string>();
        private static readonly List<bool>   Written9Slice = new List<bool>();

        private static void WriteSprite(string projectRelativePath, Texture2D tex, bool is9Slice = false)
        {
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, projectRelativePath);
            File.WriteAllBytes(abs, png);
            Object.DestroyImmediate(tex);
            WrittenPaths.Add(projectRelativePath);
            Written9Slice.Add(is9Slice);
        }

        private static void ApplySpriteImports()
        {
            for (int i = 0; i < WrittenPaths.Count; i++)
            {
                var path = WrittenPaths[i];
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 64f;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Bilinear; // smoother on the larger canvases
                imp.mipmapEnabled = false;
                if (Written9Slice[i])
                {
                    imp.spriteBorder = new Vector4(16, 16, 16, 16);
                }
                imp.SaveAndReimport();
            }
            WrittenPaths.Clear();
            Written9Slice.Clear();
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
