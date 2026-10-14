using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Tipos de superficie con textura de detalle y relieve (mapa de normales).</summary>
    public enum DetailKind { Generic, Stone, Plaster, Wood, Thatch, Tiles, Bark, BirchBark, Foliage, Fabric, Skin, Hair, Leather, Hide }

    /// <summary>
    /// Texturas procedurales de alta calidad: capas del terreno (albedo + normales), detalles de material
    /// (madera, piedra, paja, tela...), tarjetas de hojas y de hierba con transparencia recortada y ondas del agua.
    /// Los píxeles se calculan en un hilo de fondo con ruido propio (sin API de Unity) y se suben a la GPU después.
    /// </summary>
    public static class SurfaceTextures
    {
        public const int GroundSize = 512;
        public const int DetailSize = 256;

        public sealed class Detail
        {
            public Texture2D Albedo, Normal;
            /// <summary>Metros que ocupa una repetición de la textura.</summary>
            public float Tile = 1f;
            public float Bump = 1f;
            /// <summary>Compensa el oscurecimiento medio de la textura para respetar el color del material.</summary>
            public float Compensation = 1f;
        }

        public static readonly Texture2D[] GroundAlbedo = new Texture2D[4];
        public static readonly Texture2D[] GroundNormal = new Texture2D[4];
        public static readonly Color[] GroundAverage = new Color[4];
        public static Texture2D LeafCard, NeedleCard, GrassCard, WaterNormal;
        public static readonly Texture2D[] FlowerCards = new Texture2D[4];
        public static bool Ready { get; private set; }

        private static readonly Dictionary<DetailKind, Detail> Details = new Dictionary<DetailKind, Detail>();

        // Datos calculados en segundo plano
        private sealed class Raw
        {
            public int W, H;
            public Color32[] Pixels;
            public bool Linear;
            public string Name;
            public float Tile, Bump, Compensation;
            public Color Average;
        }

        // ------------------------------------------------------------------ Inicialización

        public static IEnumerator Init()
        {
            if (Ready) yield break;
            var jobs = new List<KeyValuePair<string, Func<Raw[]>>>
            {
                Job("suelo_0", () => Ground(0)), Job("suelo_1", () => Ground(1)), Job("suelo_2", () => Ground(2)), Job("suelo_3", () => Ground(3)),
                Job("hojas", () => new[] { LeafCardRaw() }), Job("agujas", () => new[] { NeedleCardRaw() }), Job("hierba", () => new[] { GrassCardRaw() }),
                Job("flores", FlowerRaws), Job("agua", () => new[] { WaterRaw() }),
            };
            foreach (DetailKind k in Enum.GetValues(typeof(DetailKind)))
            {
                var kind = k;
                jobs.Add(Job("detalle_" + kind, () => DetailRaw(kind)));
            }

            var results = new Raw[jobs.Count][];
            var task = Task.Run(() => Parallel.For(0, jobs.Count, i => results[i] = jobs[i].Value()));
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null)
            {
                Debug.LogWarning("KOLOTOPIR: no se pudieron generar las texturas detalladas: " + task.Exception.GetBaseException().Message);
                yield break;
            }

            for (int j = 0; j < jobs.Count; j++)
            {
                string key = jobs[j].Key;
                var raws = results[j];
                if (key.StartsWith("suelo_"))
                {
                    int i = key[6] - '0';
                    GroundAlbedo[i] = Upload(raws[0]);
                    GroundNormal[i] = Upload(raws[1]);
                    GroundAverage[i] = raws[0].Average;
                }
                else if (key == "hojas") LeafCard = Upload(raws[0]);
                else if (key == "agujas") NeedleCard = Upload(raws[0]);
                else if (key == "hierba") GrassCard = Upload(raws[0]);
                else if (key == "agua") WaterNormal = Upload(raws[0]);
                else if (key == "flores") for (int f = 0; f < raws.Length && f < 4; f++) FlowerCards[f] = Upload(raws[f]);
                else if (key.StartsWith("detalle_"))
                {
                    var kind = (DetailKind)Enum.Parse(typeof(DetailKind), key.Substring(8));
                    Details[kind] = new Detail { Albedo = Upload(raws[0]), Normal = Upload(raws[1]), Tile = raws[0].Tile, Bump = raws[0].Bump, Compensation = raws[0].Compensation };
                }
                if (j % 3 == 2) yield return null;
            }
            Ready = true;
        }

        private static KeyValuePair<string, Func<Raw[]>> Job(string key, Func<Raw[]> f) => new KeyValuePair<string, Func<Raw[]>>(key, f);

        private static Texture2D Upload(Raw r)
        {
            var t = new Texture2D(r.W, r.H, TextureFormat.RGBA32, true, r.Linear)
            {
                name = r.Name, wrapMode = r.Name.StartsWith("Tarjeta") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 8,
            };
            t.SetPixels32(r.Pixels);
            t.Apply(true, true);
            return t;
        }

        /// <summary>Detalle de un material; si las texturas aún no existen, se generan en el momento.</summary>
        public static Detail Get(DetailKind kind)
        {
            if (Details.TryGetValue(kind, out var d)) return d;
            try
            {
                var raws = DetailRaw(kind);
                d = new Detail { Albedo = Upload(raws[0]), Normal = Upload(raws[1]), Tile = raws[0].Tile, Bump = raws[0].Bump, Compensation = raws[0].Compensation };
                Details[kind] = d;
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("KOLOTOPIR: textura de detalle " + kind + ": " + e.Message);
                return null;
            }
        }

        // ------------------------------------------------------------------ Ruido periódico (puro C#, apto para hilos)

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static int Wrap(int i, int n) => ((i % n) + n) % n;

        private static float VNoise(float x, float y, int period, int seed)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = Smooth(x - x0), fy = Smooth(y - y0);
            int ax = Wrap(x0, period), ay = Wrap(y0, period), bx = Wrap(x0 + 1, period), by = Wrap(y0 + 1, period);
            return Lerp(Lerp(Hash(ax, ay, seed), Hash(bx, ay, seed), fx), Lerp(Hash(ax, by, seed), Hash(bx, by, seed), fx), fy);
        }

        /// <summary>fBm periódico en [0,1]; u,v en [0,1).</summary>
        private static float Fbm(float u, float v, int period, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int p = period;
            for (int o = 0; o < octaves; o++)
            {
                sum += VNoise(u * p, v * p, p, seed + o * 131) * amp;
                norm += amp;
                amp *= gain;
                p *= 2;
            }
            return sum / norm;
        }

        /// <summary>fBm con periodos distintos en cada eje (vetas alargadas que siguen repitiendo sin costuras).</summary>
        private static float FbmXY(float u, float v, int periodX, int periodY, int octaves, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int px = periodX, py = periodY;
            for (int o = 0; o < octaves; o++)
            {
                float x = u * px, y = v * py;
                int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
                float fx = Smooth(x - x0), fy = Smooth(y - y0);
                int ax = Wrap(x0, px), ay = Wrap(y0, py), bx = Wrap(x0 + 1, px), by = Wrap(y0 + 1, py);
                int sd = seed + o * 131;
                float n = Lerp(Lerp(Hash(ax, ay, sd), Hash(bx, ay, sd), fx), Lerp(Hash(ax, by, sd), Hash(bx, by, sd), fx), fy);
                sum += n * amp;
                norm += amp;
                amp *= 0.5f;
                px *= 2; py *= 2;
            }
            return sum / norm;
        }

        /// <summary>Ruido "crestado": líneas finas donde el ruido cruza 0,5 (grietas, vetas).</summary>
        private static float Ridged(float u, float v, int period, int octaves, int seed)
        {
            float n = Fbm(u, v, period, octaves, seed);
            return 1f - Math.Abs(n * 2f - 1f);
        }

        /// <summary>Ruido celular periódico: distancia al punto más cercano (f1), al segundo (f2) e id de la celda.</summary>
        private static void Worley(float u, float v, int cells, int seed, out float f1, out float f2, out int id)
        {
            float x = u * cells, y = v * cells;
            int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y);
            f1 = 9f; f2 = 9f; id = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = cx + dx, gy = cy + dy;
                    int wx = Wrap(gx, cells), wy = Wrap(gy, cells);
                    float px = gx + Hash(wx, wy, seed), py = gy + Hash(wx, wy, seed + 7);
                    float ddx = px - x, ddy = py - y;
                    float d = (float)Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (d < f1) { f2 = f1; f1 = d; id = wy * cells + wx; }
                    else if (d < f2) f2 = d;
                }
        }

        // ------------------------------------------------------------------ Lienzo con altura

        private sealed class Canvas
        {
            public readonly int N;
            public readonly Color[] C;
            public readonly float[] Hgt;
            public readonly float[] A;

            public Canvas(int n, bool alpha = false)
            {
                N = n;
                C = new Color[n * n];
                Hgt = new float[n * n];
                if (alpha) A = new float[n * n];
            }

            public int Idx(int x, int y) => Wrap(y, N) * N + Wrap(x, N);

            /// <summary>Disco suave (con envoltura) que solo pinta si queda por encima de lo que hay.</summary>
            public void Dab(float cx, float cy, float r, Color col, float h, bool wrap = true)
            {
                int x0 = (int)Math.Floor(cx - r), x1 = (int)Math.Ceiling(cx + r);
                int y0 = (int)Math.Floor(cy - r), y1 = (int)Math.Ceiling(cy + r);
                float r2 = r * r;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy > r2 + 0.25f) continue;
                        if (!wrap && (x < 0 || y < 0 || x >= N || y >= N)) continue;
                        int i = Idx(x, y);
                        if (h < Hgt[i] - 0.08f) continue;
                        C[i] = col;
                        Hgt[i] = Math.Max(Hgt[i], h);
                        if (A != null) A[i] = 1f;
                    }
            }

            /// <summary>Trazo que se estrecha (brizna, aguja, ramita).</summary>
            public void Stroke(float x0, float y0, float x1, float y1, float w0, float w1, Color c0, Color c1, float h0, float h1, bool wrap = true)
            {
                float len = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                int steps = Math.Max(2, (int)(len * 1.6f));
                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    Dab(Lerp(x0, x1, t), Lerp(y0, y1, t), Math.Max(0.45f, Lerp(w0, w1, t)), Color.Lerp(c0, c1, t), Lerp(h0, h1, t), wrap);
                }
            }

            public Raw ToRaw(string name, float aoStrength = 0f)
            {
                var px = new Color32[N * N];
                Color sum = Color.black;
                for (int i = 0; i < px.Length; i++)
                {
                    var c = C[i];
                    if (aoStrength > 0f) c *= 1f - aoStrength + aoStrength * Clamp01(Hgt[i] * 1.2f);
                    c.a = A != null ? A[i] : 1f;
                    px[i] = c;
                    sum += c;
                }
                return new Raw { W = N, H = N, Pixels = px, Name = name, Average = sum / px.Length };
            }

            public Raw NormalRaw(string name, float strength)
            {
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float dx = (Hgt[Idx(x + 1, y)] - Hgt[Idx(x - 1, y)]) * strength;
                        float dy = (Hgt[Idx(x, y + 1)] - Hgt[Idx(x, y - 1)]) * strength;
                        float inv = 1f / (float)Math.Sqrt(dx * dx + dy * dy + 1f);
                        px[y * N + x] = new Color(-dx * inv * 0.5f + 0.5f, -dy * inv * 0.5f + 0.5f, inv * 0.5f + 0.5f, 1f);
                    }
                return new Raw { W = N, H = N, Pixels = px, Linear = true, Name = name };
            }
        }

        // ------------------------------------------------------------------ Terreno

        private static Raw[] Ground(int kind)
        {
            Canvas c;
            float normal;
            switch (kind)
            {
                case 0: c = GrassGround(false); normal = 2.2f; break;
                case 1: c = GrassGround(true); normal = 2.2f; break;
                case 2: c = DirtGround(); normal = 3.2f; break;
                default: c = RockGround(); normal = 5f; break;
            }
            string n = "Suelo_" + kind;
            return new[] { c.ToRaw(n, kind <= 1 ? 0.45f : 0.25f), c.NormalRaw(n + "_N", normal) };
        }

        /// <summary>Hierba vista desde arriba: suelo oscuro, miles de briznas en todas direcciones, matas y tréboles.</summary>
        private static Canvas GrassGround(bool meadow)
        {
            int N = GroundSize;
            var c = new Canvas(N);
            int seed = meadow ? 300 : 200;
            var rng = new OromRandom(seed);
            var soilDark = new Color(0.16f, 0.13f, 0.08f);
            var soil = new Color(0.27f, 0.22f, 0.13f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = Fbm(u, v, 8, 5, seed);
                    int i = y * N + x;
                    c.C[i] = Color.Lerp(soilDark, soil, n);
                    c.Hgt[i] = n * 0.15f;
                }
            Color[] greens = meadow
                ? new[] { new Color(0.34f, 0.5f, 0.14f), new Color(0.46f, 0.6f, 0.18f), new Color(0.56f, 0.64f, 0.22f), new Color(0.4f, 0.55f, 0.2f) }
                : new[] { new Color(0.2f, 0.34f, 0.1f), new Color(0.28f, 0.44f, 0.12f), new Color(0.36f, 0.5f, 0.16f), new Color(0.24f, 0.4f, 0.14f) };
            var straw = new Color(0.66f, 0.6f, 0.34f);
            int blades = N * N / (meadow ? 20 : 18);
            for (int b = 0; b < blades; b++)
            {
                float x = rng.Value() * N, y = rng.Value() * N;
                float patch = Fbm(x / N, y / N, 6, 3, seed + 9);
                if (rng.Value() > 0.45f + patch * 0.8f) continue;
                Color col = Color.Lerp(greens[rng.Range(0, greens.Length)], greens[rng.Range(0, greens.Length)], rng.Value());
                col *= 0.8f + patch * 0.45f;
                if (rng.Value() < (meadow ? 0.2f : 0.06f)) col = Color.Lerp(straw, col, rng.Value() * 0.4f);
                float a = rng.Value() * Mathf.PI * 2f;
                float len = rng.Range(6f, meadow ? 16f : 13f);
                float w = rng.Range(0.7f, 1.5f);
                c.Stroke(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, w, 0.3f, col * 0.55f, col * 1.15f, 0.3f, 1f);
            }
            // Tréboles y hojas redondeadas
            int clovers = meadow ? 90 : 160;
            for (int i = 0; i < clovers; i++)
            {
                float x = rng.Value() * N, y = rng.Value() * N;
                var col = new Color(0.2f, 0.36f, 0.12f) * rng.Range(0.8f, 1.15f);
                for (int k = 0; k < 3; k++)
                {
                    float a = k * 2.094f + rng.Value();
                    c.Dab(x + Mathf.Cos(a) * 2.2f, y + Mathf.Sin(a) * 2.2f, 2.1f, col, 0.75f);
                }
            }
            if (meadow)
            {
                Color[] petals = { new Color(0.97f, 0.95f, 0.88f), new Color(0.98f, 0.86f, 0.3f), new Color(0.7f, 0.52f, 0.9f), new Color(0.95f, 0.6f, 0.7f) };
                for (int i = 0; i < 420; i++)
                {
                    float x = rng.Value() * N, y = rng.Value() * N;
                    var pc = petals[rng.Range(0, petals.Length)];
                    float r = rng.Range(1.2f, 2.2f);
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k * 1.2566f + rng.Value() * 0.3f;
                        c.Dab(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r, r * 0.6f, pc * rng.Range(0.85f, 1f), 1.1f);
                    }
                    c.Dab(x, y, r * 0.45f, new Color(0.85f, 0.65f, 0.15f), 1.15f);
                }
            }
            return c;
        }

        /// <summary>Tierra compactada: tonos moteados, humedad, grietas finas, guijarros semienterrados, gravilla y ramitas.</summary>
        private static Canvas DirtGround()
        {
            int N = GroundSize;
            var c = new Canvas(N);
            var rng = new OromRandom(400);
            var dark = new Color(0.27f, 0.19f, 0.12f);
            var light = new Color(0.52f, 0.41f, 0.28f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = Fbm(u, v, 4, 6, 401);
                    float mottling = Fbm(u, v, 24, 3, 402);
                    float grain = Fbm(u, v, 96, 2, 409);
                    float damp = Clamp01((Fbm(u, v, 3, 3, 403) - 0.55f) * 3f);
                    var col = Color.Lerp(dark, light, n * 0.65f + mottling * 0.25f + grain * 0.1f);
                    col = Color.Lerp(col, col * 0.72f, damp);
                    float h = n * 0.35f + mottling * 0.12f + grain * 0.08f;
                    float crack = Ridged(u, v, 5, 3, 404) * Clamp01((Fbm(u, v, 2, 2, 410) - 0.45f) * 4f);
                    if (crack > 0.95f)
                    {
                        float k = (crack - 0.95f) / 0.05f;
                        col *= 1f - 0.28f * k;
                        h -= 0.18f * k;
                    }
                    // Guijarros semienterrados, de tonos terrosos y tamaños variados
                    Worley(u, v, 40, 405, out float f1, out float f2, out int id);
                    if (Hash(id, 1, 406) < 0.32f)
                    {
                        float r = 0.1f + Hash(id, 2, 406) * Hash(id, 5, 406) * 0.32f;
                        float edge = f1 + (Fbm(u, v, 64, 2, 407) - 0.5f) * 0.1f;
                        if (edge < r)
                        {
                            float dome = (float)Math.Sqrt(1f - (edge / r) * (edge / r));
                            float tone = 0.3f + Hash(id, 3, 406) * 0.26f;
                            var stone = new Color(tone * 1.02f, tone * 0.94f, tone * 0.84f);
                            if (Hash(id, 4, 406) < 0.4f) stone = Color.Lerp(stone, new Color(0.46f, 0.34f, 0.24f), 0.6f);
                            stone = Color.Lerp(stone, col, 0.25f);
                            col = Color.Lerp(stone * 0.8f, stone * 1.08f, dome);
                            h = Math.Max(h, 0.4f + dome * 0.45f);
                        }
                        else if (edge < r + 0.05f) col *= 0.86f;
                    }
                    // Gravilla fina
                    float g = Hash(x, y, 408);
                    if (g < 0.02f) col = Color.Lerp(col, new Color(0.5f, 0.46f, 0.4f), 0.5f);
                    else if (g > 0.975f) col *= 0.78f;
                    int i = y * N + x;
                    c.C[i] = col;
                    c.Hgt[i] = h;
                }
            for (int i = 0; i < 22; i++)
            {
                float x = rng.Value() * N, y = rng.Value() * N, a = rng.Value() * Mathf.PI * 2f, len = rng.Range(12f, 30f);
                var tw = new Color(0.3f, 0.22f, 0.14f);
                c.Stroke(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, 1.1f, 0.6f, tw, tw * 1.2f, 0.7f, 0.7f);
            }
            for (int i = 0; i < 260; i++)
            {
                float x = rng.Value() * N, y = rng.Value() * N, a = rng.Value() * Mathf.PI * 2f, len = rng.Range(5f, 10f);
                var gc = new Color(0.4f, 0.44f, 0.2f) * rng.Range(0.7f, 1.1f);
                c.Stroke(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, 0.8f, 0.3f, gc * 0.6f, gc, 0.5f, 0.8f);
            }
            return c;
        }

        /// <summary>Roca: estratos deformados, grano, fracturas irregulares, motas de mica y líquenes.</summary>
        private static Canvas RockGround()
        {
            int N = GroundSize;
            var c = new Canvas(N);
            var rockDark = new Color(0.29f, 0.28f, 0.26f);
            var rockLight = new Color(0.63f, 0.6f, 0.55f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float wu = u + (Fbm(u, v, 4, 3, 501) - 0.5f) * 0.16f;
                    float wv = v + (Fbm(u, v, 4, 3, 502) - 0.5f) * 0.16f;
                    float bands = 0.5f + 0.5f * (float)Math.Sin((wv * 7f + Fbm(wu, wv, 3, 2, 503) * 0.8f) * Math.PI * 2f);
                    float bandTone = Hash(Wrap((int)Math.Floor(wv * 14f), 14), 0, 504);
                    float grain = Fbm(u, v, 16, 5, 505);
                    float big = Fbm(u, v, 2, 3, 513);
                    float col01 = 0.4f * grain + 0.25f * bands + 0.15f * bandTone + 0.2f * big;
                    var col = Color.Lerp(rockDark, rockLight, col01);
                    col = Color.Lerp(col, new Color(0.55f, 0.48f, 0.4f), Fbm(u, v, 3, 2, 506) * 0.4f);
                    float h = grain * 0.45f + bands * 0.3f + big * 0.25f;
                    // Fracturas: solo algunas aristas de las celdas, finas e irregulares
                    float wx = u + (Fbm(u, v, 8, 2, 514) - 0.5f) * 0.05f, wy = v + (Fbm(u, v, 8, 2, 515) - 0.5f) * 0.05f;
                    Worley(wx, wy, 4, 507, out float f1, out float f2, out int cid);
                    float mask = Clamp01((Fbm(u, v, 4, 2, 516) - 0.4f) * 3f);
                    float edge = f2 - f1;
                    if (edge < 0.035f && mask > 0f)
                    {
                        float k = (1f - edge / 0.035f) * mask;
                        col *= 1f - 0.4f * k;
                        h -= 0.35f * k;
                    }
                    float s = Hash(x, y, 509);
                    if (s < 0.03f) col *= 0.72f;
                    else if (s > 0.988f) col = Color.Lerp(col, Color.white, 0.35f);
                    float lichen = Fbm(u, v, 6, 4, 510);
                    if (lichen > 0.64f && Hash(x / 2, y / 2, 511) < (lichen - 0.64f) * 4f)
                    {
                        var lc = Hash(x / 3, y / 3, 512) < 0.85f ? new Color(0.62f, 0.64f, 0.45f) : new Color(0.78f, 0.55f, 0.27f);
                        col = Color.Lerp(col, lc, 0.75f);
                        h += 0.06f;
                    }
                    int i = y * N + x;
                    c.C[i] = col;
                    c.Hgt[i] = h;
                }
            return c;
        }

        // ------------------------------------------------------------------ Detalles de material (escala de grises que se tiñe)

        private static Raw[] DetailRaw(DetailKind kind)
        {
            int N = DetailSize;
            var c = new Canvas(N);
            int seed = 1000 + (int)kind * 37;
            var rng = new OromRandom(seed);
            float tile = 1.5f, bump = 1f;

            void Fill(Func<float, float, int, int, (float val, float h)> f)
            {
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        var r = f(x / (float)N, y / (float)N, x, y);
                        float g = Clamp01(r.val);
                        c.C[y * N + x] = new Color(g, g, g);
                        c.Hgt[y * N + x] = r.h;
                    }
            }

            switch (kind)
            {
                case DetailKind.Stone:
                    tile = 1.6f; bump = 3.5f;
                    Fill((u, v, x, y) =>
                    {
                        float n = Fbm(u, v, 4, 6, seed);
                        float g = Fbm(u, v, 32, 2, seed + 1);
                        float wu = u + (Fbm(u, v, 8, 2, seed + 4) - 0.5f) * 0.06f, wv = v + (Fbm(u, v, 8, 2, seed + 5) - 0.5f) * 0.06f;
                        Worley(wu, wv, 3, seed + 2, out float f1, out float f2, out _);
                        float mask = Clamp01((Fbm(u, v, 3, 2, seed + 6) - 0.42f) * 3f);
                        float crack = Clamp01(1f - (f2 - f1) / 0.03f) * mask;
                        float s = Hash(x, y, seed + 3);
                        float speck = s < 0.04f ? -0.1f : s > 0.985f ? 0.08f : 0f;
                        return (0.7f + n * 0.24f + g * 0.08f - crack * 0.22f + speck, n * 0.6f + g * 0.25f - crack * 0.35f);
                    });
                    break;
                case DetailKind.Plaster:
                    tile = 2.2f; bump = 1.2f;
                    Fill((u, v, x, y) =>
                    {
                        float blot = Fbm(u, v, 3, 4, seed);
                        float fine = Fbm(u, v, 48, 2, seed + 1);
                        return (0.84f + blot * 0.1f + fine * 0.06f, fine * 0.5f + blot * 0.2f);
                    });
                    break;
                case DetailKind.Wood:
                    tile = 1.6f; bump = 2.2f;
                    Fill((u, v, x, y) =>
                    {
                        // Tablones verticales (en v) de 1/4 de la textura, con vetas y algún nudo.
                        int plank = (int)(u * 4f);
                        float pu = u * 4f - plank;
                        float offset = Hash(plank, 0, seed) * 7f;
                        float warp = Fbm(u * 4f, v + offset, 2, 3, seed + 1) * 0.6f;
                        float grain = 0.5f + 0.5f * (float)Math.Sin((pu * 9f + warp * 3f + Fbm(u, v, 64, 1, seed + 2) * 0.4f) * Math.PI * 2f);
                        float fine = Fbm(u * 16f, v * 2f, 2, 3, seed + 3);
                        float tone = 0.78f + Hash(plank, 1, seed) * 0.16f;
                        float seam = pu < 0.03f || pu > 0.97f ? 1f : 0f;
                        float val = tone + grain * 0.08f + fine * 0.06f - seam * 0.35f;
                        float h = grain * 0.25f + fine * 0.2f - seam * 0.6f;
                        // Nudo
                        float kx = (plank + 0.5f) / 4f, ky = Hash(plank, 2, seed);
                        float dx = (u - kx) * 4f, dy = (v - ky);
                        if (dy > 0.5f) dy -= 1f; else if (dy < -0.5f) dy += 1f;
                        float kd = (float)Math.Sqrt(dx * dx * 4f + dy * dy * 40f);
                        if (Hash(plank, 3, seed) < 0.6f && kd < 0.25f) { val -= (0.25f - kd) * 1.2f; h -= (0.25f - kd); }
                        return (val, h);
                    });
                    break;
                case DetailKind.Thatch:
                    tile = 1.1f; bump = 3f;
                    Fill((u, v, x, y) =>
                    {
                        // Haces de paja superpuestos en hileras (como tejas de paja).
                        // Borde inferior de cada hilera irregular (mechones que cuelgan a distinta altura)
                        float ragged = (FbmXY(u, v, 48, 4, 2, seed + 2) - 0.5f) * 0.5f;
                        float row = v * 5f + ragged;
                        int ri = (int)Math.Floor(row);
                        float rv = row - ri;
                        float shift = Hash(Wrap(ri, 5), 0, seed) * 0.3f;
                        float strand = FbmXY(u + shift, v, 160, 2, 2, seed + 1);
                        float fiber = FbmXY(u + shift, v, 96, 12, 2, seed + 3);
                        float shade = 0.55f + 0.45f * (float)Math.Pow(rv, 0.6);
                        float val = shade * (0.66f + strand * 0.28f + fiber * 0.14f);
                        float h = rv * 0.55f + strand * 0.35f;
                        return (val, h);
                    });
                    break;
                case DetailKind.Tiles:
                    tile = 1.0f; bump = 3f;
                    Fill((u, v, x, y) =>
                    {
                        // Teja árabe: columnas de canales curvos que se solapan por hileras
                        float row = v * 6f;
                        int ri = (int)Math.Floor(row);
                        float rv = row - ri;
                        float col = u * 7f;
                        int ci = (int)Math.Floor(col);
                        float cu = col - ci;
                        float curve = (float)Math.Sin(cu * Math.PI);
                        float tone = 0.78f + Hash(Wrap(ci, 7), Wrap(ri, 6), seed) * 0.2f;
                        float overlap = rv < 0.14f ? 0.55f + rv / 0.14f * 0.45f : 1f;
                        float valley = 0.55f + 0.45f * (float)Math.Pow(curve, 0.5);
                        float dirt = 0.92f + Fbm(u, v, 24, 3, seed + 1) * 0.1f;
                        float val = tone * valley * overlap * dirt;
                        return (val, curve * 0.6f + rv * 0.35f);
                    });
                    break;
                case DetailKind.Bark:
                    tile = 1.2f; bump = 4f;
                    Fill((u, v, x, y) =>
                    {
                        // Surcos verticales profundos entre placas alargadas
                        float warp = (FbmXY(u, v, 4, 2, 2, seed) - 0.5f) * 0.08f;
                        float n = FbmXY(u + warp, v, 14, 2, 4, seed + 1);
                        float ridge = 1f - Math.Abs(n * 2f - 1f);
                        float furrow = Clamp01((ridge - 0.72f) / 0.28f);
                        float plates = FbmXY(u, v, 24, 4, 3, seed + 2);
                        float val = 0.62f + plates * 0.3f - furrow * 0.42f;
                        return (val, 0.7f - furrow * 0.7f + plates * 0.25f);
                    });
                    break;
                case DetailKind.BirchBark:
                    tile = 1.0f; bump = 1.5f;
                    Fill((u, v, x, y) =>
                    {
                        float val = 0.92f + Fbm(u, v, 8, 3, seed) * 0.08f;
                        float h = 0.5f;
                        // Lenticelas horizontales oscuras
                        float lx = u * 10f, ly = v * 28f;
                        int ix = (int)Math.Floor(lx), iy = (int)Math.Floor(ly);
                        if (Hash(Wrap(ix, 10), Wrap(iy, 28), seed + 1) < 0.22f)
                        {
                            float fx = lx - ix, fy = ly - iy;
                            if (Math.Abs(fy - 0.5f) < 0.18f && fx > 0.1f && fx < 0.9f) { val = 0.25f + Hash(ix, iy, seed + 2) * 0.2f; h = 0.2f; }
                        }
                        float patch = Fbm(u, v, 4, 3, seed + 3);
                        if (patch > 0.7f) { val *= 0.45f; h = 0.3f; }
                        return (val, h);
                    });
                    break;
                case DetailKind.Foliage:
                    tile = 0.9f; bump = 2.5f;
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
                        {
                            c.C[y * N + x] = new Color(0.45f, 0.45f, 0.45f);
                            c.Hgt[y * N + x] = 0f;
                        }
                    for (int i = 0; i < 900; i++)
                    {
                        float x = rng.Value() * N, y = rng.Value() * N, a = rng.Value() * Mathf.PI * 2f;
                        float len = rng.Range(8f, 16f), w = rng.Range(3f, 5f);
                        float g = rng.Range(0.62f, 1f);
                        float h = rng.Range(0.3f, 1f);
                        c.Stroke(x, y, x + Mathf.Cos(a) * len, y + Mathf.Sin(a) * len, w * 0.6f, w * 0.3f, new Color(g * 0.8f, g * 0.8f, g * 0.8f), new Color(g, g, g), h, h + 0.15f);
                    }
                    break;
                case DetailKind.Fabric:
                    tile = 0.14f; bump = 1.5f;
                    Fill((u, v, x, y) =>
                    {
                        float tu = u * 48f, tv = v * 48f;
                        float wu = (float)Math.Sin(tu * Math.PI * 2f), wv = (float)Math.Sin(tv * Math.PI * 2f);
                        bool over = (((int)Math.Floor(tu) + (int)Math.Floor(tv)) & 1) == 0;
                        float weave = over ? Math.Abs(wv) : Math.Abs(wu);
                        float wrinkle = Fbm(u, v, 2, 3, seed);
                        float val = 0.82f + weave * 0.12f + wrinkle * 0.06f;
                        return (val, weave * 0.4f + wrinkle * 0.5f);
                    });
                    break;
                case DetailKind.Skin:
                    tile = 0.18f; bump = 0.6f;
                    Fill((u, v, x, y) =>
                    {
                        float n = Fbm(u, v, 16, 3, seed);
                        float pores = Hash(x, y, seed + 1) < 0.05f ? -0.03f : 0f;
                        return (0.94f + n * 0.06f + pores, n * 0.3f + pores * 2f);
                    });
                    break;
                case DetailKind.Hair:
                    tile = 0.16f; bump = 1.8f;
                    Fill((u, v, x, y) =>
                    {
                        float strand = FbmXY(u, v, 64, 3, 2, seed);
                        float s2 = 0.5f + 0.5f * (float)Math.Sin(u * 28f * Math.PI * 2f + strand * 8f);
                        return (0.72f + strand * 0.18f + s2 * 0.1f, strand * 0.5f + s2 * 0.3f);
                    });
                    break;
                case DetailKind.Leather:
                    tile = 0.3f; bump = 1.6f;
                    Fill((u, v, x, y) =>
                    {
                        Worley(u, v, 18, seed, out float f1, out float f2, out _);
                        float wr = Clamp01((f2 - f1) / 0.2f);
                        float n = Fbm(u, v, 8, 3, seed + 1);
                        return (0.84f + wr * 0.06f + n * 0.1f, wr * 0.25f + n * 0.4f);
                    });
                    break;
                case DetailKind.Hide:
                    tile = 0.45f; bump = 1.8f;
                    Fill((u, v, x, y) =>
                    {
                        float wr = Ridged(u, v, 10, 3, seed);
                        float n = Fbm(u, v, 6, 4, seed + 1);
                        float val = 0.8f + n * 0.14f - (wr > 0.9f ? (wr - 0.9f) * 1.6f : 0f);
                        return (val, n * 0.5f - wr * 0.25f);
                    });
                    break;
                default:
                    tile = 1.6f; bump = 1.2f;
                    Fill((u, v, x, y) =>
                    {
                        float n = Fbm(u, v, 6, 5, seed);
                        float g = Fbm(u, v, 48, 2, seed + 1);
                        return (0.84f + n * 0.1f + g * 0.06f, n * 0.5f + g * 0.3f);
                    });
                    break;
            }
            var albedo = c.ToRaw("Detalle_" + kind);
            float mean = (albedo.Average.r + albedo.Average.g + albedo.Average.b) / 3f;
            albedo.Tile = tile;
            albedo.Bump = bump;
            albedo.Compensation = mean > 0.05f ? Math.Min(1.4f, 1f / mean) : 1f;
            return new[] { albedo, c.NormalRaw("Detalle_" + kind + "_N", bump) };
        }

        // ------------------------------------------------------------------ Tarjetas con transparencia recortada

        /// <summary>Racimo de hojas para las copas: hojas en abanico desde el centro, con nervio y variación de tono.</summary>
        private static Raw LeafCardRaw()
        {
            int N = 256;
            var c = new Canvas(N, true);
            var rng = new OromRandom(7001);
            // Ramitas
            for (int i = 0; i < 5; i++)
            {
                float a = rng.Value() * Mathf.PI * 2f;
                var tw = new Color(0.42f, 0.34f, 0.26f);
                c.Stroke(N * 0.5f, N * 0.5f, N * 0.5f + Mathf.Cos(a) * N * 0.36f, N * 0.5f + Mathf.Sin(a) * N * 0.36f, 2.2f, 1f, tw, tw, 0.1f, 0.1f, false);
            }
            for (int i = 0; i < 150; i++)
            {
                float r = (float)Math.Sqrt(rng.Value()) * N * 0.4f;
                float a = rng.Value() * Mathf.PI * 2f;
                float cx = N * 0.5f + Mathf.Cos(a) * r, cy = N * 0.5f + Mathf.Sin(a) * r;
                float dir = a + rng.Range(-0.7f, 0.7f);
                float len = rng.Range(26f, 40f), wid = rng.Range(11f, 16f);
                float tone = rng.Range(0.6f, 1f);
                float h = 0.3f + rng.Value() * 0.7f;
                Leaf(c, cx, cy, dir, len, wid, tone, h);
            }
            return c.ToRaw("Tarjeta_Hojas");
        }

        private static void Leaf(Canvas c, float cx, float cy, float dir, float len, float wid, float tone, float h)
        {
            float ca = Mathf.Cos(dir), sa = Mathf.Sin(dir);
            int R = (int)Math.Ceiling(len);
            for (int y = -R; y <= R; y++)
                for (int x = -R; x <= R; x++)
                {
                    float lx = x * ca + y * sa;        // a lo largo de la hoja
                    float ly = -x * sa + y * ca;       // a lo ancho
                    float t = lx / len;
                    if (t < 0f || t > 1f) continue;
                    float half = wid * 0.5f * (float)Math.Pow(Math.Sin(Math.PI * t), 0.75);
                    if (Math.Abs(ly) > half) continue;
                    int px = (int)(cx + x), py = (int)(cy + y);
                    if (px < 0 || py < 0 || px >= c.N || py >= c.N) continue;
                    int i = py * c.N + px;
                    if (h < c.Hgt[i]) continue;
                    float edge = Math.Abs(ly) / Math.Max(0.5f, half);
                    float val = tone * (0.78f + 0.22f * t) * (1f - edge * 0.18f);
                    if (Math.Abs(ly) < 0.7f) val *= 0.82f;
                    c.C[i] = new Color(val * 0.93f, val, val * 0.86f);
                    c.Hgt[i] = h;
                    c.A[i] = 1f;
                }
        }

        /// <summary>Ramilla de pino: tallo central con agujas a ambos lados.</summary>
        private static Raw NeedleCardRaw()
        {
            int N = 256;
            var c = new Canvas(N, true);
            var rng = new OromRandom(7101);
            var stem = new Color(0.45f, 0.36f, 0.28f);
            for (int b = 0; b < 3; b++)
            {
                float x0 = N * (0.3f + b * 0.2f), y0 = N * 0.05f;
                float x1 = x0 + rng.Range(-20f, 20f), y1 = N * 0.95f;
                c.Stroke(x0, y0, x1, y1, 2f, 1f, stem, stem, 0.2f, 0.2f, false);
                for (int i = 0; i < 140; i++)
                {
                    float t = rng.Value();
                    float sx = Lerp(x0, x1, t), sy = Lerp(y0, y1, t);
                    float side = rng.Value() < 0.5f ? -1f : 1f;
                    float a = Mathf.PI * 0.5f + side * rng.Range(0.5f, 1.1f);
                    float len = rng.Range(16f, 28f) * (1f - t * 0.4f);
                    float tone = rng.Range(0.6f, 1f);
                    var col = new Color(tone * 0.9f, tone, tone * 0.85f);
                    c.Stroke(sx, sy, sx + Mathf.Cos(a) * len, sy + Mathf.Sin(a) * len, 1.7f, 0.7f, col * 0.75f, col, 0.5f, 0.9f, false);
                }
            }
            return c.ToRaw("Tarjeta_Agujas");
        }

        /// <summary>Mata de hierba de perfil: briznas curvadas que nacen abajo y se aclaran hacia la punta.</summary>
        private static Raw GrassCardRaw()
        {
            int N = 256;
            var c = new Canvas(N, true);
            var rng = new OromRandom(7201);
            for (int i = 0; i < 46; i++)
            {
                float x0 = rng.Range(0.12f, 0.88f) * N;
                float height = rng.Range(0.45f, 0.97f) * N;
                float bend = rng.Range(-0.28f, 0.28f) * N;
                float w = rng.Range(3.5f, 6.5f);
                float tone = rng.Range(0.8f, 1.05f);
                int steps = 22;
                float px = x0, py = 0f;
                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float x = x0 + bend * t * t, y = height * t;
                    float val0 = tone * Lerp(0.42f, 1f, (s - 1) / (float)steps), val1 = tone * Lerp(0.42f, 1f, t);
                    c.Stroke(px, py, x, y, w * (1f - (s - 1) / (float)steps * 0.9f), w * (1f - t * 0.9f),
                        new Color(val0 * 0.92f, val0, val0 * 0.8f), new Color(val1 * 0.92f, val1, val1 * 0.8f), i / 46f, i / 46f, false);
                    px = x; py = y;
                }
            }
            return c.ToRaw("Tarjeta_Hierba");
        }

        private static Raw[] FlowerRaws()
        {
            Color[] petals = { new Color(0.98f, 0.85f, 0.25f), new Color(0.97f, 0.96f, 0.92f), new Color(0.66f, 0.46f, 0.9f), new Color(0.95f, 0.55f, 0.66f) };
            var list = new Raw[4];
            for (int f = 0; f < 4; f++)
            {
                int N = 128;
                var c = new Canvas(N, true);
                var rng = new OromRandom(7300 + f);
                var green = new Color(0.32f, 0.5f, 0.2f);
                for (int s = 0; s < 3; s++)
                {
                    float x0 = N * (0.3f + s * 0.2f), top = N * rng.Range(0.62f, 0.82f), xt = x0 + rng.Range(-10f, 10f);
                    c.Stroke(x0, 0f, xt, top, 1.6f, 1.2f, green * 0.7f, green, 0.1f, 0.1f, false);
                    c.Stroke(x0, N * 0.2f, x0 + rng.Range(-14f, 14f), N * 0.35f, 2.5f, 0.6f, green * 0.8f, green, 0.12f, 0.12f, false);
                    float r = rng.Range(6f, 9f);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 1.047f + rng.Value() * 0.3f;
                        c.Dab(xt + Mathf.Cos(a) * r * 0.75f, top + Mathf.Sin(a) * r * 0.75f, r * 0.55f, petals[f] * rng.Range(0.85f, 1f), 0.5f, false);
                    }
                    c.Dab(xt, top, r * 0.35f, new Color(0.85f, 0.62f, 0.15f), 0.6f, false);
                }
                list[f] = c.ToRaw("Tarjeta_Flor" + f);
            }
            return list;
        }

        // ------------------------------------------------------------------ Agua

        private static Raw WaterRaw()
        {
            int N = 256;
            var c = new Canvas(N);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float h = 0f;
                    h += (float)Math.Sin((u * 3f + v * 2f) * Math.PI * 2f) * 0.3f;
                    h += (float)Math.Sin((u * -2f + v * 5f) * Math.PI * 2f) * 0.2f;
                    h += (float)Math.Sin((u * 7f + v * -4f) * Math.PI * 2f) * 0.1f;
                    h += Fbm(u, v, 8, 4, 8001) * 0.6f;
                    c.Hgt[y * N + x] = h * 0.25f;
                }
            return c.NormalRaw("Agua_N", 6f);
        }
    }
}
