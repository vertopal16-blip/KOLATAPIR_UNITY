using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Consulta de altura del suelo: usa el terreno cargado si existe y, si no, la función analítica.</summary>
    public static class Ground
    {
        public static float Height(float x, float z)
        {
            var w = Game.World;
            if (w != null)
            {
                var t = w.TerrainAt(x, z);
                if (t != null) return t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y;
            }
            return WorldShape.Height(x, z);
        }

        public static Vector3 Snap(Vector3 p, float offset = 0f) => new Vector3(p.x, Height(p.x, p.z) + offset, p.z);
    }

    public enum TreeKind { Roble, Pino, Abedul, Memoria }

    /// <summary>
    /// Generadores de atrezo low-poly (árboles, rocas, arbustos, barriles...) que escriben directamente
    /// en un <see cref="ModelBuilder"/> para que cada sector se dibuje con pocos renderers.
    /// </summary>
    public static class Props
    {
        private static Material M(Color c, float s = 0.1f) => MaterialLibrary.Lit(c, s);

        private static MeshBuilder Begin(ModelBuilder mb, Material mat, Matrix4x4 m)
        {
            var b = mb.For(mat);
            b.Matrix = m;
            return b;
        }

        private static void End(ModelBuilder mb, Material mat) => mb.For(mat).Matrix = Matrix4x4.identity;

        /// <summary>Variación discreta (3 tonos) para que los materiales se compartan entre muchos objetos.</summary>
        private static Color Vary(Color c, OromRandom rng, float amount = 0.08f)
        {
            int step = rng.Range(0, 3) - 1;
            float k = 1f + step * amount;
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k));
        }

        private static float Quant(float t, int levels) => Mathf.Round(t * (levels - 1)) / (levels - 1);

        public static void Tree(ModelBuilder mb, Vector3 pos, float scale, int seed, TreeKind kind)
        {
            var rng = new OromRandom(seed);
            var m = Matrix4x4.TRS(pos, Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), Vector3.one * scale);
            switch (kind)
            {
                case TreeKind.Pino:
                {
                    var bark = M(Pal.Bark);
                    Begin(mb, bark, m).Cylinder(Vector3.zero, 0.28f, 0.14f, 5.5f, 6, Quaternion.identity);
                    End(mb, bark);
                    var leaf = M(Vary(new Color(0.2f, 0.36f, 0.22f), rng, 0.1f));
                    var b = Begin(mb, leaf, m);
                    for (int i = 0; i < 4; i++)
                    {
                        float y = 1.6f + i * 1.2f;
                        float r = 2.2f - i * 0.45f;
                        b.Cone(new Vector3(0f, y, 0f), r, 2.2f, 7, Quaternion.Euler(0f, i * 23f, 0f));
                    }
                    End(mb, leaf);
                    break;
                }
                case TreeKind.Abedul:
                {
                    var bark = M(new Color(0.85f, 0.82f, 0.76f));
                    Begin(mb, bark, m).Cylinder(Vector3.zero, 0.16f, 0.09f, 5.2f, 6, Quaternion.Euler(rng.Range(-4f, 4f), 0f, rng.Range(-4f, 4f)));
                    End(mb, bark);
                    var leaf = M(Vary(new Color(0.55f, 0.66f, 0.28f), rng));
                    var b = Begin(mb, leaf, m);
                    for (int i = 0; i < 3; i++)
                        b.Rock(new Vector3(rng.Range(-0.5f, 0.5f), 3.6f + i * 0.8f, rng.Range(-0.5f, 0.5f)), new Vector3(1.9f, 2.4f, 1.9f) * (1f - i * 0.15f), Quaternion.identity, seed + i, 0.18f, false);
                    End(mb, leaf);
                    break;
                }
                case TreeKind.Memoria:
                {
                    var bark = M(new Color(0.32f, 0.26f, 0.3f));
                    var bb = Begin(mb, bark, m);
                    bb.Cylinder(Vector3.zero, 0.38f, 0.26f, 2.4f, 6, Quaternion.Euler(0f, 0f, 8f));
                    bb.Cylinder(new Vector3(0.33f, 2.3f, 0f), 0.26f, 0.15f, 2.4f, 6, Quaternion.Euler(0f, 0f, -18f));
                    End(mb, bark);
                    var tint = Color.Lerp(Pal.LeafDream, new Color(0.85f, 0.6f, 0.7f), Quant(rng.Value(), 3));
                    var leaf = M(Vary(tint, rng, 0.06f), 0.25f);
                    var b = Begin(mb, leaf, m);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.7f + rng.Value();
                        b.Rock(new Vector3(Mathf.Cos(a) * 1.3f - 0.4f, 4.4f + rng.Range(-0.4f, 0.6f), Mathf.Sin(a) * 1.3f), new Vector3(2.8f, 2.2f, 2.8f), Quaternion.identity, seed * 3 + i, 0.2f, false);
                    }
                    End(mb, leaf);
                    break;
                }
                default:
                {
                    var bark = M(Pal.Bark);
                    var bb = Begin(mb, bark, m);
                    bb.Cylinder(Vector3.zero, 0.42f, 0.26f, 3.4f, 7, Quaternion.identity);
                    bb.Cylinder(new Vector3(0f, 2.4f, 0f), 0.16f, 0.08f, 2f, 5, Quaternion.Euler(0f, 0f, 48f));
                    bb.Cylinder(new Vector3(0f, 2.6f, 0f), 0.15f, 0.07f, 1.8f, 5, Quaternion.Euler(40f, 0f, -35f));
                    End(mb, bark);
                    var leaf = M(Vary(Color.Lerp(Pal.Leaf, Pal.LeafLight, Quant(rng.Value(), 3)), rng));
                    var b = Begin(mb, leaf, m);
                    int blobs = 3 + rng.Range(0, 3);
                    for (int i = 0; i < blobs; i++)
                    {
                        float a = i * Mathf.PI * 2f / blobs + rng.Value();
                        float r = i == 0 ? 0f : 1.4f;
                        b.Rock(new Vector3(Mathf.Cos(a) * r, 4.2f + rng.Range(-0.5f, 0.8f) + (i == 0 ? 0.8f : 0f), Mathf.Sin(a) * r),
                            new Vector3(3.4f, 2.8f, 3.4f) * rng.Range(0.85f, 1.15f), Quaternion.identity, seed * 7 + i, 0.2f, false);
                    }
                    End(mb, leaf);
                    break;
                }
            }
        }

        public static void Rock(ModelBuilder mb, Vector3 pos, Vector3 size, int seed, bool mossy, Color? tint = null)
        {
            var rng = new OromRandom(seed);
            var rot = Quaternion.Euler(rng.Range(-8f, 8f), rng.Range(0f, 360f), rng.Range(-8f, 8f));
            var col = tint ?? Vary(Pal.Stone, rng, 0.1f);
            mb.For(M(col, 0.05f)).Rock(pos, size, rot, seed, 0.28f);
            if (mossy)
            {
                var top = pos + Vector3.up * size.y * 0.32f;
                mb.For(M(Vary(Pal.Moss, rng), 0.02f)).Rock(top, new Vector3(size.x * 0.8f, size.y * 0.3f, size.z * 0.8f), rot, seed + 1, 0.2f, false);
            }
        }

        public static void Bush(ModelBuilder mb, Vector3 pos, float scale, int seed, bool berries)
        {
            var rng = new OromRandom(seed);
            var leaf = M(Vary(new Color(0.27f, 0.42f, 0.19f), rng, 0.12f));
            var b = mb.For(leaf);
            int blobs = 2 + rng.Range(0, 2);
            for (int i = 0; i < blobs; i++)
                b.Rock(pos + new Vector3(rng.Range(-0.5f, 0.5f), 0.45f, rng.Range(-0.5f, 0.5f)) * scale, new Vector3(1.3f, 1f, 1.3f) * scale, Quaternion.identity, seed + i, 0.25f, false);
            if (berries)
            {
                var berry = MaterialLibrary.Lit(new Color(0.62f, 0.12f, 0.3f), 0.5f);
                var bb = mb.For(berry);
                for (int i = 0; i < 9; i++)
                {
                    float a = rng.Range(0f, Mathf.PI * 2f);
                    var p = pos + new Vector3(Mathf.Cos(a) * 0.6f, rng.Range(0.4f, 0.9f), Mathf.Sin(a) * 0.6f) * scale;
                    bb.Sphere(p, Vector3.one * 0.07f, 5, 3, Quaternion.identity);
                }
            }
        }

        public static void Reeds(ModelBuilder mb, Vector3 pos, int seed)
        {
            var rng = new OromRandom(seed);
            var stem = M(new Color(0.55f, 0.6f, 0.3f));
            var top = M(new Color(0.45f, 0.32f, 0.2f));
            for (int i = 0; i < 7; i++)
            {
                var p = pos + new Vector3(rng.Range(-0.4f, 0.4f), 0f, rng.Range(-0.4f, 0.4f));
                float h = rng.Range(1.1f, 1.7f);
                var rot = Quaternion.Euler(rng.Range(-8f, 8f), 0f, rng.Range(-8f, 8f));
                mb.For(stem).Cylinder(p, 0.025f, 0.015f, h, 4, rot, false, false);
                if (i % 2 == 0) mb.For(top).Cylinder(p + rot * Vector3.up * (h - 0.25f), 0.05f, 0.05f, 0.22f, 5, rot);
            }
        }

        public static void Herb(ModelBuilder mb, Vector3 pos, int seed)
        {
            var rng = new OromRandom(seed);
            var leaf = M(new Color(0.36f, 0.58f, 0.3f), 0.2f);
            var b = mb.For(leaf);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f + rng.Range(0f, 20f);
                var r = Quaternion.Euler(0f, a, 0f);
                b.DoubleTri(pos + r * new Vector3(-0.06f, 0.02f, 0f), pos + r * new Vector3(0.06f, 0.02f, 0f), pos + r * new Vector3(0f, 0.32f, 0.28f));
            }
            mb.For(MaterialLibrary.Lit(new Color(0.9f, 0.9f, 0.7f))).Sphere(pos + Vector3.up * 0.3f, Vector3.one * 0.04f, 5, 3, Quaternion.identity);
        }

        public static void Barrel(ModelBuilder mb, Vector3 pos, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            mb.For(M(Pal.Wood)).Lathe(pos, new[] { new Vector2(0.28f, 0f), new Vector2(0.34f, 0.4f), new Vector2(0.28f, 0.82f) }, 10, rot);
            mb.For(M(Pal.WoodDark)).Cylinder(pos + Vector3.up * 0.2f, 0.33f, 0.335f, 0.05f, 10, rot);
            mb.For(M(Pal.WoodDark)).Cylinder(pos + Vector3.up * 0.6f, 0.335f, 0.33f, 0.05f, 10, rot);
        }

        public static void Crate(ModelBuilder mb, Vector3 pos, float yaw, float size = 0.6f)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            mb.For(M(Pal.WoodLight)).Box(pos + Vector3.up * size * 0.5f, Vector3.one * size, rot);
            mb.For(M(Pal.WoodDark)).Box(pos + Vector3.up * size * 0.5f, new Vector3(size * 1.02f, size * 0.12f, size * 1.02f), rot);
        }

        public static void Basket(ModelBuilder mb, Vector3 pos, Color fill)
        {
            mb.For(M(new Color(0.66f, 0.52f, 0.3f))).Lathe(pos, new[] { new Vector2(0.18f, 0f), new Vector2(0.26f, 0.28f), new Vector2(0.27f, 0.3f) }, 9, Quaternion.identity, true, false);
            mb.For(M(fill)).Sphere(pos + Vector3.up * 0.28f, new Vector3(0.24f, 0.08f, 0.24f), 7, 3, Quaternion.identity);
        }

        public static void Firewood(ModelBuilder mb, Vector3 pos, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var wood = M(Pal.Wood);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4 - row; i++)
                {
                    var p = pos + rot * new Vector3((i - (3 - row) * 0.5f) * 0.22f, 0.11f + row * 0.19f, 0f);
                    mb.For(wood).Cylinder(p - rot * Vector3.forward * 0.5f, 0.1f, 0.1f, 1f, 6, rot * Quaternion.Euler(90f, 0f, 0f));
                }
        }

        public static void Fence(ModelBuilder mb, Vector3 a, Vector3 b)
        {
            var wood = M(Pal.WoodLight);
            float len = Vector3.Distance(a, b);
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                mb.For(wood).Box(p + Vector3.up * 0.45f, new Vector3(0.12f, 0.9f, 0.12f), Quaternion.identity);
            }
            var dir = (b - a);
            var rot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized);
            var mid = (a + b) * 0.5f;
            mb.For(wood).Box(mid + Vector3.up * 0.7f, new Vector3(0.06f, 0.08f, len), rot * Quaternion.Euler(-Mathf.Atan2(b.y - a.y, len) * Mathf.Rad2Deg, 0f, 0f));
            mb.For(wood).Box(mid + Vector3.up * 0.4f, new Vector3(0.06f, 0.08f, len), rot * Quaternion.Euler(-Mathf.Atan2(b.y - a.y, len) * Mathf.Rad2Deg, 0f, 0f));
        }

        public static void Pot(ModelBuilder mb, Vector3 pos, Color c)
        {
            mb.For(M(c, 0.3f)).Lathe(pos, new[] { new Vector2(0.12f, 0f), new Vector2(0.24f, 0.18f), new Vector2(0.2f, 0.34f), new Vector2(0.12f, 0.4f), new Vector2(0.14f, 0.45f) }, 10, Quaternion.identity, true, false);
        }

        /// <summary>Malla instanciable de mata de hierba (dos planos cruzados).</summary>
        public static Mesh GrassTuftMesh()
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                var r = Quaternion.Euler(0f, i * 60f, 0f);
                Vector3 a = r * new Vector3(-0.25f, 0f, 0f), c = r * new Vector3(0.25f, 0f, 0f);
                Vector3 top = r * new Vector3(0f, 0.55f, 0.06f);
                b.DoubleTri(a, c, top);
                b.DoubleTri(r * new Vector3(-0.15f, 0f, 0.05f), r * new Vector3(0.05f, 0f, -0.05f), r * new Vector3(-0.22f, 0.42f, 0.1f));
            }
            return b.ToMesh("GrassTuft");
        }

        public static Mesh FlowerMesh()
        {
            var b = new MeshBuilder();
            b.Cylinder(Vector3.zero, 0.012f, 0.01f, 0.32f, 3, Quaternion.identity, false, false);
            for (int i = 0; i < 5; i++)
            {
                var r = Quaternion.Euler(0f, i * 72f, 0f);
                b.DoubleTri(new Vector3(0f, 0.32f, 0f), r * new Vector3(0.05f, 0.34f, 0.07f), r * new Vector3(-0.05f, 0.34f, 0.07f));
            }
            return b.ToMesh("Flower");
        }
    }
}
