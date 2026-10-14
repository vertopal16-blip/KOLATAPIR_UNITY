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
        private static Material M(Color c, float s = 0.1f) => MaterialLibrary.World(c, s);

        /// <summary>Variación discreta (3 tonos) para que los materiales se compartan entre muchos objetos.</summary>
        private static Color Vary(Color c, OromRandom rng, float amount = 0.08f)
        {
            int step = rng.Range(0, 3) - 1;
            float k = 1f + step * amount;
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k));
        }

        private static float Quant(float t, int levels) => Mathf.Round(t * (levels - 1)) / (levels - 1);

        public static Material Leaves(Color c) => SurfaceTextures.LeafCard != null ? MaterialLibrary.Cutout(SurfaceTextures.LeafCard, c) : null;
        private static Material Needles(Color c) => SurfaceTextures.NeedleCard != null ? MaterialLibrary.Cutout(SurfaceTextures.NeedleCard, c) : null;

        // ------------------------------------------------------------------ Árboles

        /// <summary>
        /// Tronco ligeramente curvado (tramos cónicos encadenados) con ensanche de raíces.
        /// Devuelve la punta y la dirección final para colocar ramas y copa.
        /// </summary>
        private static Vector3 Trunk(MeshBuilder b, Vector3 basePos, float r0, float r1, float height, int segs, OromRandom rng, float bend, out Vector3 dir)
        {
            dir = Vector3.up;
            var p = basePos;
            const int pieces = 4;
            // Raíces: un torno ancho en la base y contrafuertes
            b.Lathe(basePos + Vector3.down * 0.15f, new[] { new Vector2(r0 * 1.9f, 0f), new Vector2(r0 * 1.35f, 0.3f), new Vector2(r0 * 1.05f, 0.75f) }, segs, Quaternion.identity, false, false);
            int roots = 4 + rng.Range(0, 3);
            for (int i = 0; i < roots; i++)
            {
                float a = i * Mathf.PI * 2f / roots + rng.Value() * 0.6f;
                var outDir = new Vector3(Mathf.Cos(a), -0.35f, Mathf.Sin(a)).normalized;
                b.Cylinder(basePos + Vector3.up * 0.45f, r0 * 0.38f, r0 * 0.12f, r0 * 2.6f, 5, Quaternion.FromToRotation(Vector3.up, outDir), false, true);
            }
            for (int i = 0; i < pieces; i++)
            {
                float t0 = i / (float)pieces, t1 = (i + 1) / (float)pieces;
                float len = height / pieces;
                var rot = Quaternion.FromToRotation(Vector3.up, dir);
                b.Cylinder(p, Mathf.Lerp(r0, r1, t0), Mathf.Lerp(r0, r1, t1), len * 1.02f, segs, rot, false, i == pieces - 1);
                p += dir * len;
                dir = (dir + new Vector3(rng.Range(-1f, 1f), 0f, rng.Range(-1f, 1f)) * bend).normalized;
            }
            return p;
        }

        private static void Branch(MeshBuilder b, Vector3 from, Vector3 dir, float r, float len, int segs)
        {
            b.Cylinder(from, r, r * 0.35f, len, segs, Quaternion.FromToRotation(Vector3.up, dir.normalized), false, true);
        }

        /// <summary>
        /// Copa de hojas: tarjetas con racimos de hojas repartidas por la superficie de un elipsoide. Las normales
        /// apuntan hacia fuera desde el centro de la copa, así se ilumina como un volumen y no como planos sueltos.
        /// </summary>
        public static void LeafShell(MeshBuilder cards, Vector3 center, Vector3 radii, int count, float size, OromRandom rng, float droop = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                var d = new Vector3(rng.Range(-1f, 1f), rng.Range(-0.55f, 1f), rng.Range(-1f, 1f));
                if (d.sqrMagnitude < 0.01f) d = Vector3.up;
                d.Normalize();
                var p = center + Vector3.Scale(d, radii) * rng.Range(0.7f, 1.02f);
                // Orientación: tarjeta casi tangente a la superficie, con giro aleatorio
                var normal = Vector3.Slerp(d, Random3(rng), 0.45f).normalized;
                var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                tangent = Quaternion.AngleAxis(rng.Range(0f, 360f), normal) * tangent;
                var bit = Vector3.Cross(normal, tangent);
                float s = size * rng.Range(0.8f, 1.2f) * 0.5f;
                var down = Vector3.down * droop * s;
                Vector3 a = p - tangent * s - bit * s + down, b = p + tangent * s - bit * s + down, c = p + tangent * s + bit * s, e = p - tangent * s + bit * s;
                cards.Card(a, b, c, e, Nrm(a, center), Nrm(b, center), Nrm(c, center), Nrm(e, center));
            }
        }

        private static Vector3 Nrm(Vector3 p, Vector3 center) => ((p - center).normalized + Vector3.up * 0.35f).normalized;
        private static Vector3 Random3(OromRandom rng) => new Vector3(rng.Range(-1f, 1f), rng.Range(-1f, 1f), rng.Range(-1f, 1f));

        public static void Tree(ModelBuilder mb, Vector3 pos, float scale, int seed, TreeKind kind)
        {
            var rng = new OromRandom(seed);
            var m = Matrix4x4.TRS(pos, Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), Vector3.one * scale);
            switch (kind)
            {
                case TreeKind.Pino:
                {
                    var barkMat = MaterialLibrary.Surface(DetailKind.Bark, new Color(0.34f, 0.24f, 0.18f), 0.08f);
                    var bark = Begin(mb, barkMat, m);
                    var top = Trunk(bark, Vector3.zero, 0.3f, 0.06f, 7.6f, 9, rng, 0.03f, out _);
                    var needleCol = Vary(new Color(0.2f, 0.34f, 0.2f), rng, 0.1f);
                    var core = Begin(mb, MaterialLibrary.Surface(DetailKind.Foliage, needleCol * 0.7f, 0.05f), m);
                    var cardsMat = Needles(needleCol);
                    var cards = cardsMat != null ? Begin(mb, cardsMat, m) : null;
                    int whorls = 7;
                    for (int i = 0; i < whorls; i++)
                    {
                        float t = i / (float)(whorls - 1);
                        float y = 1.6f + t * 5.8f;
                        float r = Mathf.Lerp(2.5f, 0.5f, t);
                        core.Cone(new Vector3(0f, y - 0.25f, 0f), r * 0.78f, 1.6f, 10, Quaternion.Euler(0f, i * 23f, 0f));
                        int branches = 5 + rng.Range(0, 3);
                        for (int k = 0; k < branches; k++)
                        {
                            float a = k * Mathf.PI * 2f / branches + rng.Value() * 0.5f;
                            Branch(bark, new Vector3(0f, y, 0f), new Vector3(Mathf.Cos(a), -0.3f, Mathf.Sin(a)), 0.05f * (1.2f - t), r * 0.9f, 4);
                        }
                        if (cards == null) continue;
                        // Ramillas de agujas inclinadas hacia abajo que cubren el cono (visibles de lado y desde arriba)
                        int count = 14 + (int)(r * 10f);
                        var axis = new Vector3(0f, y, 0f);
                        for (int k = 0; k < count; k++)
                        {
                            float a = rng.Value() * Mathf.PI * 2f;
                            float h = rng.Value();
                            float rr = r * (1f - h * 0.8f) * rng.Range(0.55f, 0.95f);
                            var outDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                            var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                            var along = (outDir * 0.72f + Vector3.down * 0.7f).normalized;
                            float sz = Mathf.Lerp(1.1f, 0.6f, t) * rng.Range(0.85f, 1.2f);
                            var basePt = new Vector3(outDir.x * rr, y - 0.1f + h * 1.4f, outDir.z * rr);
                            Vector3 a0 = basePt - tangent * sz * 0.45f - along * sz * 0.15f, a1 = basePt + tangent * sz * 0.45f - along * sz * 0.15f;
                            Vector3 a2 = a1 + along * sz, a3 = a0 + along * sz;
                            cards.Card(a0, a1, a2, a3, Nrm(a0, axis), Nrm(a1, axis), Nrm(a2, axis), Nrm(a3, axis));
                        }
                    }
                    End(mb, barkMat);
                    End(mb, MaterialLibrary.Surface(DetailKind.Foliage, needleCol * 0.7f, 0.05f));
                    if (cardsMat != null) End(mb, cardsMat);
                    break;
                }
                case TreeKind.Abedul:
                {
                    var barkMat = MaterialLibrary.Surface(DetailKind.BirchBark, new Color(0.86f, 0.84f, 0.79f), 0.1f);
                    var bark = Begin(mb, barkMat, m);
                    var top = Trunk(bark, Vector3.zero, 0.17f, 0.06f, 6.2f, 8, rng, 0.05f, out var dir);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 1.26f + rng.Value();
                        var from = Vector3.up * (3.4f + i * 0.5f);
                        Branch(bark, from, new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)), 0.05f, 1.6f, 5);
                    }
                    End(mb, barkMat);
                    var leafCol = Vary(new Color(0.5f, 0.64f, 0.26f), rng);
                    var coreMat = MaterialLibrary.Surface(DetailKind.Foliage, leafCol * 0.68f, 0.05f);
                    var core = Begin(mb, coreMat, m);
                    var center = top + Vector3.down * 1.2f;
                    core.Rock(center, new Vector3(2.1f, 3f, 2.1f), Quaternion.identity, seed, 0.18f, true);
                    End(mb, coreMat);
                    var cardsMat = Leaves(leafCol);
                    if (cardsMat != null)
                    {
                        LeafShell(Begin(mb, cardsMat, m), center, new Vector3(1.7f, 2.3f, 1.7f), 58, 1.4f, rng, 0.35f);
                        End(mb, cardsMat);
                    }
                    break;
                }
                case TreeKind.Memoria:
                {
                    var barkMat = MaterialLibrary.Surface(DetailKind.Bark, new Color(0.32f, 0.26f, 0.3f), 0.06f);
                    var bark = Begin(mb, barkMat, m);
                    var top = Trunk(bark, Vector3.zero, 0.4f, 0.18f, 4.2f, 9, rng, 0.18f, out var dir);
                    var tint = Color.Lerp(Pal.LeafDream, new Color(0.85f, 0.6f, 0.7f), Quant(rng.Value(), 3));
                    var leafCol = Vary(tint, rng, 0.06f);
                    var coreMat = MaterialLibrary.Surface(DetailKind.Foliage, leafCol * 0.65f, 0.1f);
                    var core = Begin(mb, coreMat, m);
                    var cardsMat = Leaves(leafCol);
                    var cards = cardsMat != null ? Begin(mb, cardsMat, m) : null;
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 1.7f + rng.Value();
                        var c = new Vector3(Mathf.Cos(a) * 1.4f, 4.6f + rng.Range(-0.4f, 0.6f), Mathf.Sin(a) * 1.4f);
                        Branch(bark, top + Vector3.down * 0.5f, c - (top + Vector3.down * 0.5f), 0.14f, (c - top).magnitude + 0.3f, 6);
                        core.Rock(c, new Vector3(1.9f, 1.5f, 1.9f), Quaternion.identity, seed * 3 + i, 0.2f, true);
                        if (cards != null) LeafShell(cards, c, new Vector3(1.55f, 1.25f, 1.55f), 26, 1.4f, rng, 0.45f);
                    }
                    End(mb, barkMat);
                    End(mb, coreMat);
                    if (cardsMat != null) End(mb, cardsMat);
                    break;
                }
                default:
                {
                    // Roble: tronco grueso, ramas principales abiertas y una copa amplia de varios lóbulos.
                    var barkMat = MaterialLibrary.Surface(DetailKind.Bark, Vary(Pal.Bark, rng, 0.06f), 0.08f);
                    var bark = Begin(mb, barkMat, m);
                    var top = Trunk(bark, Vector3.zero, 0.46f, 0.24f, 3.4f, 8, rng, 0.08f, out var dir);
                    var leafCol = Vary(Color.Lerp(Pal.Leaf, Pal.LeafLight, Quant(rng.Value(), 3)), rng);
                    var coreMat = MaterialLibrary.Surface(DetailKind.Foliage, leafCol * 0.62f, 0.05f);
                    var core = Begin(mb, coreMat, m);
                    var cardsMat = Leaves(leafCol);
                    var cards = cardsMat != null ? Begin(mb, cardsMat, m) : null;
                    int lobes = 3 + rng.Range(0, 3);
                    for (int i = 0; i < lobes; i++)
                    {
                        float a = i * Mathf.PI * 2f / lobes + rng.Value();
                        float r = rng.Range(1.1f, 1.8f);
                        var c = new Vector3(Mathf.Cos(a) * r, 4.6f + rng.Range(-0.4f, 0.9f), Mathf.Sin(a) * r);
                        var from = top + Vector3.down * 0.4f;
                        Branch(bark, from, c - from, 0.2f, (c - from).magnitude * 0.95f, 7);
                        var size = new Vector3(2.9f, 2.3f, 2.9f) * rng.Range(0.85f, 1.15f);
                        core.Rock(c, size * 0.62f, Quaternion.identity, seed * 7 + i, 0.18f, true);
                        if (cards != null) LeafShell(cards, c, size * 0.55f, 22, 1.55f, rng, 0.25f);
                    }
                    var crown = top + Vector3.up * 1.4f;
                    core.Rock(crown, new Vector3(2.9f, 2.3f, 2.9f), Quaternion.identity, seed * 7 + 99, 0.18f, true);
                    if (cards != null) LeafShell(cards, crown, new Vector3(1.9f, 1.5f, 1.9f), 26, 1.6f, rng, 0.25f);
                    End(mb, barkMat);
                    End(mb, coreMat);
                    if (cardsMat != null) End(mb, cardsMat);
                    break;
                }
            }
        }

        private static MeshBuilder Begin(ModelBuilder mb, Material mat, Matrix4x4 m)
        {
            var b = mb.For(mat);
            b.Matrix = m;
            return b;
        }

        private static void End(ModelBuilder mb, Material mat) => mb.For(mat).Matrix = Matrix4x4.identity;

        // ------------------------------------------------------------------ Rocas y arbustos

        public static void Rock(ModelBuilder mb, Vector3 pos, Vector3 size, int seed, bool mossy, Color? tint = null)
        {
            var rng = new OromRandom(seed);
            var rot = Quaternion.Euler(rng.Range(-8f, 8f), rng.Range(0f, 360f), rng.Range(-8f, 8f));
            var col = tint ?? Vary(Pal.Stone, rng, 0.1f);
            mb.For(MaterialLibrary.Surface(DetailKind.Stone, col, 0.12f)).Boulder(pos, size, rot, seed);
            if (mossy)
            {
                var top = pos + Vector3.up * size.y * 0.3f;
                mb.For(MaterialLibrary.Surface(DetailKind.Foliage, Vary(Pal.Moss, rng), 0.02f))
                  .Rock(top, new Vector3(size.x * 0.75f, size.y * 0.28f, size.z * 0.75f), rot, seed + 1, 0.22f, true);
            }
        }

        public static void Bush(ModelBuilder mb, Vector3 pos, float scale, int seed, bool berries)
        {
            var rng = new OromRandom(seed);
            var leafCol = Vary(new Color(0.27f, 0.42f, 0.19f), rng, 0.12f);
            var core = mb.For(MaterialLibrary.Surface(DetailKind.Foliage, leafCol * 0.65f, 0.04f));
            var cardsMat = Leaves(leafCol);
            var cards = cardsMat != null ? mb.For(cardsMat) : null;
            int blobs = 2 + rng.Range(0, 2);
            for (int i = 0; i < blobs; i++)
            {
                var c = pos + new Vector3(rng.Range(-0.5f, 0.5f), 0.45f, rng.Range(-0.5f, 0.5f)) * scale;
                core.Rock(c, new Vector3(0.95f, 0.75f, 0.95f) * scale, Quaternion.identity, seed + i, 0.25f, true);
                if (cards != null) LeafShell(cards, c + Vector3.up * 0.05f, new Vector3(0.6f, 0.48f, 0.6f) * scale, 15, 0.8f * scale, rng, 0.2f);
            }
            if (berries)
            {
                var berry = MaterialLibrary.Lit(new Color(0.62f, 0.12f, 0.3f), 0.5f);
                var bb = mb.For(berry);
                for (int i = 0; i < 12; i++)
                {
                    float a = rng.Range(0f, Mathf.PI * 2f);
                    var p = pos + new Vector3(Mathf.Cos(a) * 0.62f, rng.Range(0.35f, 0.95f), Mathf.Sin(a) * 0.62f) * scale;
                    bb.Sphere(p, Vector3.one * 0.06f, 6, 4, Quaternion.identity);
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

        /// <summary>
        /// Mata de hierba instanciable: tres tarjetas cruzadas con la textura de briznas (recorte alfa).
        /// Las normales apuntan hacia arriba para que la hierba se ilumine como el suelo y no parpadee.
        /// </summary>
        public static Mesh GrassTuftMesh()
        {
            var b = new MeshBuilder();
            const float w = 0.42f, h = 0.48f;
            for (int i = 0; i < 3; i++)
            {
                var r = Quaternion.Euler(0f, i * 60f + 10f, 0f);
                var lean = r * new Vector3(0f, 0f, 0.06f);
                Vector3 a = r * new Vector3(-w, 0f, 0f), c = r * new Vector3(w, 0f, 0f);
                Vector3 up = Vector3.up, tilt = (Vector3.up + r * Vector3.forward * 0.25f).normalized;
                b.Card(a, c, c + Vector3.up * h + lean, a + Vector3.up * h + lean, up, up, tilt, tilt);
            }
            return b.ToMesh("MataHierba");
        }

        /// <summary>Flores silvestres: dos tarjetas cruzadas con tallos, hojas y corolas.</summary>
        public static Mesh FlowerMesh()
        {
            var b = new MeshBuilder();
            const float w = 0.2f, h = 0.42f;
            for (int i = 0; i < 2; i++)
            {
                var r = Quaternion.Euler(0f, i * 90f + 20f, 0f);
                Vector3 a = r * new Vector3(-w, 0f, 0f), c = r * new Vector3(w, 0f, 0f);
                b.Card(a, c, c + Vector3.up * h, a + Vector3.up * h, Vector3.up, Vector3.up, Vector3.up, Vector3.up);
            }
            return b.ToMesh("Flores");
        }
    }
}
