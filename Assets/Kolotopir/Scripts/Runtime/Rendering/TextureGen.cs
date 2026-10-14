using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Texturas procedurales (sin recursos externos): capas del terreno, nubes, partículas e interfaz.</summary>
    public static class TextureGen
    {
        // ------------------------------------------------------------------ Ruido periódico (texturas que repiten sin costuras)

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int ax = ((x0 % period) + period) % period, ay = ((y0 % period) + period) % period;
            int bx = (ax + 1) % period, by = (ay + 1) % period;
            float v00 = Hash(ax, ay, seed), v10 = Hash(bx, ay, seed), v01 = Hash(ax, by, seed), v11 = Hash(bx, by, seed);
            return Mathf.Lerp(Mathf.Lerp(v00, v10, fx), Mathf.Lerp(v01, v11, fx), fy);
        }

        /// <summary>fBm periódico en [0,1] para coordenadas u,v en [0,1).</summary>
        public static float TileFbm(float u, float v, int basePeriod, int octaves, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(u * period, v * period, period, seed + o * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                period *= 2;
            }
            return sum / norm;
        }

        private static Texture2D NewTex(int size, bool mip, TextureWrapMode wrap, string name)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, mip) { name = name, wrapMode = wrap, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            return t;
        }

        // ------------------------------------------------------------------ Capas del terreno

        public enum GroundKind { Hierba, Pradera, Tierra, Roca }

        public static Texture2D Ground(GroundKind kind, int size = 256)
        {
            var tex = NewTex(size, true, TextureWrapMode.Repeat, "Ground_" + kind);
            var px = new Color32[size * size];
            var rng = new OromRandom((int)kind * 97 + 13);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float n = TileFbm(u, v, 8, 4, 11 + (int)kind);
                    float fine = TileFbm(u, v, 64, 2, 77 + (int)kind);
                    Color c;
                    switch (kind)
                    {
                        case GroundKind.Hierba:
                            c = Color.Lerp(new Color(0.27f, 0.4f, 0.16f), new Color(0.42f, 0.55f, 0.23f), n);
                            c = Color.Lerp(c, new Color(0.5f, 0.52f, 0.26f), Mathf.Clamp01(fine - 0.55f) * 1.5f);
                            break;
                        case GroundKind.Pradera:
                            c = Color.Lerp(new Color(0.32f, 0.5f, 0.18f), new Color(0.5f, 0.66f, 0.26f), n);
                            c = Color.Lerp(c, new Color(0.58f, 0.66f, 0.3f), Mathf.Clamp01(fine - 0.6f) * 2f);
                            break;
                        case GroundKind.Tierra:
                            c = Color.Lerp(new Color(0.42f, 0.32f, 0.21f), new Color(0.6f, 0.48f, 0.33f), n);
                            c = Color.Lerp(c, new Color(0.35f, 0.27f, 0.2f), Mathf.Clamp01(0.45f - fine) * 1.6f);
                            break;
                        default:
                            float strata = Mathf.Abs(Mathf.Sin((v * 6f + n * 1.5f) * Mathf.PI));
                            c = Color.Lerp(new Color(0.36f, 0.34f, 0.32f), new Color(0.6f, 0.57f, 0.52f), n * 0.7f + strata * 0.3f);
                            c = Color.Lerp(c, new Color(0.25f, 0.24f, 0.22f), Mathf.Clamp01(0.3f - fine) * 2f);
                            break;
                    }
                    px[y * size + x] = c;
                }
            }
            // Detalles: flores en la pradera, guijarros en la tierra.
            if (kind == GroundKind.Pradera || kind == GroundKind.Tierra || kind == GroundKind.Hierba)
            {
                int dots = kind == GroundKind.Pradera ? 900 : 350;
                Color32[] palette = kind == GroundKind.Pradera
                    ? new Color32[] { new Color32(240, 220, 110, 255), new Color32(245, 245, 235, 255), new Color32(170, 120, 210, 255), new Color32(230, 140, 160, 255) }
                    : kind == GroundKind.Tierra
                        ? new Color32[] { new Color32(130, 120, 108, 255), new Color32(95, 82, 70, 255) }
                        : new Color32[] { new Color32(95, 120, 50, 255), new Color32(60, 85, 35, 255) };
                for (int i = 0; i < dots; i++)
                {
                    int cx = rng.Range(0, size), cy = rng.Range(0, size);
                    var col = palette[rng.Range(0, palette.Length)];
                    int r = rng.Chance(0.3f) ? 2 : 1;
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (dx * dx + dy * dy > r * r) continue;
                            int xx = (cx + dx + size) % size, yy = (cy + dy + size) % size;
                            px[yy * size + xx] = col;
                        }
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        // ------------------------------------------------------------------ Efectos

        public static Texture2D SoftCircle(int size = 64, float hardness = 0.0f)
        {
            var tex = NewTex(size, false, TextureWrapMode.Clamp, "SoftCircle");
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.Lerp(a * a, a > 0.02f ? 1f : 0f, hardness);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        public static Texture2D Clouds(int size = 256)
        {
            var tex = NewTex(size, true, TextureWrapMode.Repeat, "Clouds");
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float n = TileFbm(u, v, 4, 5, 5);
                    float d = Mathf.SmoothStep(0.35f, 0.75f, n);
                    float shade = 0.82f + 0.18f * TileFbm(u, v, 8, 3, 9);
                    px[y * size + x] = new Color(shade, shade, shade * 1.02f, 0.25f + d * 0.75f);
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        public static Texture2D Footprint()
        {
            const int w = 32, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Footprint" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h * 2f - 1f;
                    float heel = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx * 1.6f + (ny + 0.45f) * (ny + 0.45f) * 4f));
                    float toe = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx * 1.2f + (ny - 0.35f) * (ny - 0.35f) * 2.2f));
                    float a = Mathf.Clamp01(Mathf.Max(heel, toe) * 3f);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        // ------------------------------------------------------------------ Interfaz

        private static Sprite _rounded, _roundedSmall, _circle, _vignette, _gradient;

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null) _rounded = RoundedSprite(48, 14);
                return _rounded;
            }
        }

        public static Sprite RoundedSmall
        {
            get
            {
                if (_roundedSmall == null) _roundedSmall = RoundedSprite(24, 6);
                return _roundedSmall;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    var t = SoftCircle(64, 1f);
                    _circle = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
                }
                return _circle;
            }
        }

        public static Sprite Vignette
        {
            get
            {
                if (_vignette == null)
                {
                    const int s = 256;
                    var t = NewTex(s, false, TextureWrapMode.Clamp, "Vignette");
                    var px = new Color32[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                            px[y * s + x] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0.35f, 1f, d));
                        }
                    t.SetPixels32(px);
                    t.Apply(false);
                    _vignette = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
                }
                return _vignette;
            }
        }

        /// <summary>Máscara circular para el catalejo: transparente en el centro, negra fuera.</summary>
        public static Sprite ScopeMask()
        {
            const int s = 256;
            var t = NewTex(s, false, TextureWrapMode.Clamp, "Scope");
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * s + x] = new Color(0f, 0f, 0f, Mathf.SmoothStep(0.86f, 0.95f, d));
                }
            t.SetPixels32(px);
            t.Apply(false);
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite VerticalGradient
        {
            get
            {
                if (_gradient == null)
                {
                    var t = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Gradient" };
                    var px = new Color32[4 * 64];
                    for (int y = 0; y < 64; y++)
                        for (int x = 0; x < 4; x++)
                            px[y * 4 + x] = new Color(1f, 1f, 1f, y / 63f);
                    t.SetPixels32(px);
                    t.Apply(false);
                    _gradient = Sprite.Create(t, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
                }
                return _gradient;
            }
        }

        private static Sprite RoundedSprite(int size, int radius)
        {
            var t = NewTex(size, false, TextureWrapMode.Clamp, "Rounded");
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels32(px);
            t.Apply(false);
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
        }

        /// <summary>Icono simple de objeto: forma por categoría, coloreada con el color del objeto.</summary>
        public static Sprite ItemIcon(ItemDef def)
        {
            const int s = 64;
            var t = NewTex(s, false, TextureWrapMode.Clamp, "Icon_" + def.Id);
            var px = new Color32[s * s];
            Color baseCol = K.Hex(def.Color);
            Color dark = K.Mul(baseCol, 0.55f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                    float a = IconShape(def, u, v);
                    float edge = IconShape(def, u * 1.12f, v * 1.12f);
                    Color c = Color.Lerp(dark, baseCol, Mathf.Clamp01(0.6f + v * 0.5f));
                    if (edge < 0.5f && a >= 0.5f) c = dark;
                    px[y * s + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(a));
                }
            t.SetPixels32(px);
            t.Apply(false);
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float IconShape(ItemDef def, float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            switch (def.Category)
            {
                case ItemCategory.Arma:
                {
                    // hoja diagonal + empuñadura
                    float d = Mathf.Abs(u - v) / 1.4142f;
                    float along = (u + v) / 1.4142f;
                    bool blade = d < 0.1f && along > -0.4f && along < 0.85f;
                    bool guard = Mathf.Abs(along + 0.4f) < 0.06f && d < 0.3f;
                    bool grip = d < 0.07f && along <= -0.4f && along > -0.8f;
                    return blade || guard || grip ? 1f : 0f;
                }
                case ItemCategory.Armadura:
                    return (Mathf.Abs(u) < 0.6f - Mathf.Max(0f, v) * 0.25f && v > -0.75f && v < 0.7f) ? 1f : 0f;
                case ItemCategory.Accesorio:
                    return (r < 0.55f && r > 0.25f) || (r < 0.18f) ? 1f : (Mathf.Abs(u) < 0.05f && v > 0.5f && v < 0.85f ? 1f : 0f);
                case ItemCategory.Consumible:
                    return def.IsFood ? (r < 0.62f ? 1f : 0f) : ((Mathf.Abs(u) < 0.2f && Mathf.Abs(v) < 0.65f) || (Mathf.Abs(v) < 0.2f && Mathf.Abs(u) < 0.65f) ? 1f : 0f);
                case ItemCategory.Material:
                    return Mathf.Abs(u) + Mathf.Abs(v) < 0.75f ? 1f : 0f;
                case ItemCategory.Herramienta:
                    return (Mathf.Abs(u) < 0.62f && Mathf.Abs(v) < 0.45f) ? 1f : 0f;
                default:
                    return (Mathf.Abs(u) < 0.5f && Mathf.Abs(v) < 0.65f) ? 1f : 0f;
            }
        }
    }
}
