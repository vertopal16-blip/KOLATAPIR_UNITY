using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kolotopir
{
    /// <summary>
    /// Constructor de mallas procedurales. Todas las primitivas aceptan una matriz de transformación y orientan
    /// sus caras automáticamente hacia fuera (robusto ante errores de orden). Genera UV proyectadas en metros
    /// (para texturas de detalle), sombreado suave opcional por ángulo y tangentes para los mapas de relieve.
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();

        public Matrix4x4 Matrix = Matrix4x4.identity;
        /// <summary>Multiplicador de segmentos de cilindros, esferas y tornos (más detalle en personajes).</summary>
        public float Detail = 1f;
        /// <summary>Hay normales personalizadas (tarjetas de hojas o hierba): no se suavizan.</summary>
        public bool CustomNormals { get; private set; }
        public int VertexCount => Vertices.Count;
        public bool IsEmpty => Triangles.Count == 0;

        public MeshBuilder Push(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            Matrix = Matrix * Matrix4x4.TRS(pos, rot, scale);
            return this;
        }

        public void Reset() => Matrix = Matrix4x4.identity;

        private Vector3 T(Vector3 p) => Matrix.MultiplyPoint3x4(p);

        /// <summary>Triángulo con normal plana; si su normal apunta en contra de <paramref name="outward"/>, se invierte.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 wa = T(a), wb = T(b), wc = T(c);
            Vector3 wo = Matrix.MultiplyVector(outward);
            Vector3 n = Vector3.Cross(wb - wa, wc - wa);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, wo) < 0f) { var tmp = wb; wb = wc; wc = tmp; n = -n; }
            n.Normalize();
            int i = Vertices.Count;
            Vertices.Add(wa); Vertices.Add(wb); Vertices.Add(wc);
            Normals.Add(n); Normals.Add(n); Normals.Add(n);
            // Proyección "de caja" en metros según el eje dominante de la cara: las texturas no se estiran.
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) { Uvs.Add(new Vector2(wa.x, wa.z)); Uvs.Add(new Vector2(wb.x, wb.z)); Uvs.Add(new Vector2(wc.x, wc.z)); }
            else if (ax >= az) { Uvs.Add(new Vector2(wa.z, wa.y)); Uvs.Add(new Vector2(wb.z, wb.y)); Uvs.Add(new Vector2(wc.z, wc.y)); }
            else { Uvs.Add(new Vector2(wa.x, wa.y)); Uvs.Add(new Vector2(wb.x, wb.y)); Uvs.Add(new Vector2(wc.x, wc.y)); }
            Triangles.Add(i); Triangles.Add(i + 1); Triangles.Add(i + 2);
        }

        /// <summary>
        /// Tarjeta (cuadrilátero) con UV de 0 a 1 y normales indicadas por vértice: hojas, hierba, flores.
        /// Si es de doble cara se añade la cara trasera con el mismo sombreado (no necesita desactivar el culling).
        /// </summary>
        public void Card(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, bool doubleSided = true)
        {
            CustomNormals = true;
            Vector3 wa = T(a), wb = T(b), wc = T(c), wd = T(d);
            Vector3 n0 = Matrix.MultiplyVector(na).normalized, n1 = Matrix.MultiplyVector(nb).normalized;
            Vector3 n2 = Matrix.MultiplyVector(nc).normalized, n3 = Matrix.MultiplyVector(nd).normalized;
            for (int side = 0; side < (doubleSided ? 2 : 1); side++)
            {
                int i = Vertices.Count;
                Vertices.Add(wa); Vertices.Add(wb); Vertices.Add(wc); Vertices.Add(wd);
                Normals.Add(n0); Normals.Add(n1); Normals.Add(n2); Normals.Add(n3);
                Uvs.Add(new Vector2(0f, 0f)); Uvs.Add(new Vector2(1f, 0f)); Uvs.Add(new Vector2(1f, 1f)); Uvs.Add(new Vector2(0f, 1f));
                if (side == 0) { Triangles.Add(i); Triangles.Add(i + 2); Triangles.Add(i + 1); Triangles.Add(i); Triangles.Add(i + 3); Triangles.Add(i + 2); }
                else { Triangles.Add(i); Triangles.Add(i + 1); Triangles.Add(i + 2); Triangles.Add(i); Triangles.Add(i + 2); Triangles.Add(i + 3); }
            }
        }

        private int Seg(int n, int min) => Mathf.Max(min, Mathf.RoundToInt(n * Detail));

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            Tri(a, b, c, outward);
            Tri(a, c, d, outward);
        }

        /// <summary>Cara visible por ambos lados (hojas, alas, telas).</summary>
        public void DoubleQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            Quad(a, b, c, d, n);
            Quad(a, b, c, d, -n);
        }

        public void DoubleTri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            Tri(a, b, c, n);
            Tri(a, b, c, -n);
        }

        // ------------------------------------------------------------------ Primitivas

        public void Box(Vector3 center, Vector3 size, Quaternion rot)
        {
            Vector3 h = size * 0.5f;
            Vector3[] c =
            {
                new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z),
                new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z),
            };
            for (int i = 0; i < 8; i++) c[i] = center + rot * c[i];
            Vector3 f = rot * Vector3.forward, r = rot * Vector3.right, u = rot * Vector3.up;
            Quad(c[0], c[1], c[2], c[3], -f);
            Quad(c[5], c[4], c[7], c[6], f);
            Quad(c[4], c[0], c[3], c[7], -r);
            Quad(c[1], c[5], c[6], c[2], r);
            Quad(c[3], c[2], c[6], c[7], u);
            Quad(c[4], c[5], c[1], c[0], -u);
        }

        public void Box(Vector3 center, Vector3 size) => Box(center, size, Quaternion.identity);

        /// <summary>Cilindro o tronco de cono a lo largo del eje local Y (base en <paramref name="baseCenter"/>).</summary>
        public void Cylinder(Vector3 baseCenter, float rBottom, float rTop, float height, int segments, Quaternion rot, bool capBottom = true, bool capTop = true)
        {
            segments = Seg(segments, 3);
            Vector3 up = rot * Vector3.up;
            Vector3 top = baseCenter + up * height;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = rot * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = rot * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = baseCenter + d0 * rBottom, b1 = baseCenter + d1 * rBottom;
                Vector3 t0 = top + d0 * rTop, t1 = top + d1 * rTop;
                Vector3 outward = (d0 + d1).normalized;
                if (rTop > 0.0001f && rBottom > 0.0001f) Quad(b0, b1, t1, t0, outward);
                else if (rTop <= 0.0001f) Tri(b0, b1, top, outward + up * 0.2f);
                else Tri(baseCenter, t0, t1, outward - up * 0.2f);
                if (rTop <= 0.0001f && rBottom <= 0.0001f) continue;
                if (capBottom && rBottom > 0.0001f) Tri(baseCenter, b0, b1, -up);
                if (capTop && rTop > 0.0001f) Tri(top, t0, t1, up);
            }
        }

        public void Cone(Vector3 baseCenter, float radius, float height, int segments, Quaternion rot, bool capBottom = true)
            => Cylinder(baseCenter, radius, 0f, height, segments, rot, capBottom, false);

        /// <summary>Superficie de revolución: perfil de (radio, altura) alrededor del eje local Y.</summary>
        public void Lathe(Vector3 baseCenter, Vector2[] profile, int segments, Quaternion rot, bool capBottom = true, bool capTop = true)
        {
            segments = Seg(segments, 3);
            Vector3 up = rot * Vector3.up;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 d0 = rot * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = rot * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 outward = (d0 + d1).normalized;
                for (int k = 0; k < profile.Length - 1; k++)
                {
                    Vector2 p0 = profile[k], p1 = profile[k + 1];
                    Vector3 v00 = baseCenter + d0 * p0.x + up * p0.y, v01 = baseCenter + d1 * p0.x + up * p0.y;
                    Vector3 v10 = baseCenter + d0 * p1.x + up * p1.y, v11 = baseCenter + d1 * p1.x + up * p1.y;
                    // La normal "hacia fuera" se inclina según la pendiente del perfil.
                    Vector2 slope = p1 - p0;
                    Vector3 hint = outward * slope.y - up * slope.x;
                    if (hint.sqrMagnitude < 1e-6f) hint = outward;
                    if (p0.x > 0.0001f && p1.x > 0.0001f) Quad(v00, v01, v11, v10, hint);
                    else if (p0.x <= 0.0001f && p1.x > 0.0001f) Tri(v00, v11, v10, hint);
                    else if (p0.x > 0.0001f) Tri(v00, v01, v10, hint);
                }
                Vector2 first = profile[0], last = profile[profile.Length - 1];
                if (capBottom && first.x > 0.0001f)
                    Tri(baseCenter + up * first.y, baseCenter + d0 * first.x + up * first.y, baseCenter + d1 * first.x + up * first.y, -up);
                if (capTop && last.x > 0.0001f)
                    Tri(baseCenter + up * last.y, baseCenter + d0 * last.x + up * last.y, baseCenter + d1 * last.x + up * last.y, up);
            }
        }

        /// <summary>Esfera (o elipsoide) low-poly.</summary>
        public void Sphere(Vector3 center, Vector3 radii, int lon, int lat, Quaternion rot)
        {
            lon = Seg(lon, 4); lat = Seg(lat, 3);
            for (int j = 0; j < lat; j++)
            {
                float t0 = Mathf.PI * j / lat, t1 = Mathf.PI * (j + 1) / lat;
                for (int i = 0; i < lon; i++)
                {
                    float p0 = Mathf.PI * 2f * i / lon, p1 = Mathf.PI * 2f * (i + 1) / lon;
                    Vector3 a = SpherePoint(t0, p0, radii), b = SpherePoint(t0, p1, radii);
                    Vector3 c = SpherePoint(t1, p1, radii), d = SpherePoint(t1, p0, radii);
                    a = center + rot * a; b = center + rot * b; c = center + rot * c; d = center + rot * d;
                    Vector3 mid = (a + b + c + d) * 0.25f - center;
                    if (j == 0) Tri(a, c, d, mid);
                    else if (j == lat - 1) Tri(a, b, c, mid);
                    else Quad(a, b, c, d, mid);
                }
            }
        }

        private static Vector3 SpherePoint(float theta, float phi, Vector3 r)
        {
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi) * r.x, Mathf.Cos(theta) * r.y, Mathf.Sin(theta) * Mathf.Sin(phi) * r.z);
        }

        private static readonly Vector3[] IcoVerts;
        private static readonly int[] IcoTris =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        static MeshBuilder()
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            IcoVerts = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < IcoVerts.Length; i++) IcoVerts[i] = IcoVerts[i].normalized;
        }

        /// <summary>Roca o masa de follaje: icosaedro subdividido y deformado de forma determinista.</summary>
        public void Rock(Vector3 center, Vector3 size, Quaternion rot, int seed, float roughness = 0.22f, bool subdivide = true)
        {
            var rng = new Kolotopir.Core.OromRandom(seed);
            var verts = new List<Vector3>(IcoVerts);
            var tris = new List<int>(IcoTris);
            if (subdivide)
            {
                var mid = new Dictionary<long, int>();
                var newTris = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, mid, a, b), bc = Mid(verts, mid, b, c), ca = Mid(verts, mid, c, a);
                    newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = newTris;
            }
            for (int i = 0; i < verts.Count; i++)
            {
                float k = 1f + (rng.Value() - 0.5f) * 2f * roughness;
                verts[i] = verts[i] * k;
            }
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = Scale(verts[tris[i]], size), b = Scale(verts[tris[i + 1]], size), c = Scale(verts[tris[i + 2]], size);
                Vector3 mid = (a + b + c) / 3f;
                Tri(center + rot * a, center + rot * b, center + rot * c, rot * mid);
            }
        }

        /// <summary>
        /// Roca natural: icosaedro subdividido dos veces, recortado por planos al azar (caras de fractura),
        /// con la base aplanada para asentarse en el suelo y una ligera irregularidad.
        /// </summary>
        public void Boulder(Vector3 center, Vector3 size, Quaternion rot, int seed)
        {
            var rng = new Kolotopir.Core.OromRandom(seed);
            var verts = new List<Vector3>(IcoVerts);
            var tris = new List<int>(IcoTris);
            for (int level = 0; level < 2; level++)
            {
                var mid = new Dictionary<long, int>();
                var newTris = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, mid, a, b), bc = Mid(verts, mid, b, c), ca = Mid(verts, mid, c, a);
                    newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = newTris;
            }
            int cuts = 5 + rng.Range(0, 4);
            var planes = new Vector4[cuts];
            for (int k = 0; k < cuts; k++)
            {
                var n = new Vector3(rng.Range(-1f, 1f), rng.Range(-0.3f, 1f), rng.Range(-1f, 1f)).normalized;
                planes[k] = new Vector4(n.x, n.y, n.z, rng.Range(0.62f, 0.85f));
            }
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i] * (1f + (rng.Value() - 0.5f) * 0.08f);
                for (int k = 0; k < cuts; k++)
                {
                    var n = new Vector3(planes[k].x, planes[k].y, planes[k].z);
                    float d = Vector3.Dot(v, n);
                    if (d > planes[k].w) v -= n * (d - planes[k].w) * 0.9f;
                }
                if (v.y < -0.35f) v.y = -0.35f + (v.y + 0.35f) * 0.25f;
                verts[i] = v;
            }
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = Scale(verts[tris[i]], size), b = Scale(verts[tris[i + 1]], size), c = Scale(verts[tris[i + 2]], size);
                Vector3 m = (a + b + c) / 3f;
                Tri(center + rot * a, center + rot * b, center + rot * c, rot * m);
            }
        }

        private static Vector3 Scale(Vector3 v, Vector3 s) => new Vector3(v.x * s.x * 0.5f, v.y * s.y * 0.5f, v.z * s.z * 0.5f);

        private static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        }

        /// <summary>Copia otra malla (con su transformación) dentro de este constructor.</summary>
        public void Append(MeshBuilder other, Matrix4x4 m)
        {
            int baseIndex = Vertices.Count;
            for (int i = 0; i < other.Vertices.Count; i++)
            {
                Vertices.Add(m.MultiplyPoint3x4(other.Vertices[i]));
                Normals.Add(m.MultiplyVector(other.Normals[i]).normalized);
                Uvs.Add(other.Uvs[i]);
            }
            for (int i = 0; i < other.Triangles.Count; i++) Triangles.Add(baseIndex + other.Triangles[i]);
        }

        /// <summary>
        /// Sombreado suave por ángulo: en cada posición se promedian las normales de las caras que forman un ángulo
        /// menor que el indicado (las aristas vivas se conservan). Los vértices siguen siendo propios de cada cara,
        /// así que las UV proyectadas no se alteran.
        /// </summary>
        public void Smooth(float angleDeg)
        {
            if (angleDeg <= 0f || CustomNormals || Vertices.Count == 0) return;
            float cos = Mathf.Cos(angleDeg * Mathf.Deg2Rad);
            int triCount = Triangles.Count / 3;
            var faceN = new Vector3[triCount];
            var faceU = new Vector3[triCount];
            var vertTri = new int[Vertices.Count];
            for (int t = 0; t < triCount; t++)
            {
                int ia = Triangles[t * 3], ib = Triangles[t * 3 + 1], ic = Triangles[t * 3 + 2];
                var n = Vector3.Cross(Vertices[ib] - Vertices[ia], Vertices[ic] - Vertices[ia]);
                faceN[t] = n;
                faceU[t] = n.sqrMagnitude > 1e-14f ? n.normalized : Normals[ia];
                vertTri[ia] = t; vertTri[ib] = t; vertTri[ic] = t;
            }
            var groups = new Dictionary<long, List<int>>(Vertices.Count / 4);
            for (int i = 0; i < Vertices.Count; i++)
            {
                long key = PosKey(Vertices[i]);
                if (!groups.TryGetValue(key, out var list)) { list = new List<int>(6); groups[key] = list; }
                list.Add(i);
            }
            foreach (var list in groups.Values)
            {
                if (list.Count == 1) continue;
                for (int a = 0; a < list.Count; a++)
                {
                    int va = list[a];
                    var fa = faceU[vertTri[va]];
                    Vector3 sum = Vector3.zero;
                    for (int b = 0; b < list.Count; b++)
                    {
                        int tb = vertTri[list[b]];
                        if (Vector3.Dot(fa, faceU[tb]) >= cos) sum += faceN[tb];
                    }
                    if (sum.sqrMagnitude > 1e-14f) Normals[va] = sum.normalized;
                }
            }
        }

        private static long PosKey(Vector3 p)
        {
            long x = (long)Mathf.Round(p.x * 1000f) + 1048576, y = (long)Mathf.Round(p.y * 1000f) + 1048576, z = (long)Mathf.Round(p.z * 1000f) + 1048576;
            return ((x & 0x1FFFFF) << 42) | ((y & 0x1FFFFF) << 21) | (z & 0x1FFFFF);
        }

        public Mesh ToMesh(string name = "Kolotopir")
        {
            var mesh = new Mesh { name = name };
            if (Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            // Tangentes para los mapas de relieve (las UV ya están proyectadas en metros).
            if (Triangles.Count > 0) mesh.RecalculateTangents();
            return mesh;
        }
    }

    /// <summary>
    /// Agrupa geometría por material para generar pocos renderers (un objeto por material).
    /// Se usa para casas, sectores y personajes.
    /// </summary>
    public sealed class ModelBuilder
    {
        private readonly Dictionary<Material, MeshBuilder> _parts = new Dictionary<Material, MeshBuilder>();
        private readonly List<Material> _order = new List<Material>();
        /// <summary>Ángulo de suavizado de normales (0 = facetado, para arquitectura; 40–75 para formas orgánicas).</summary>
        public float SmoothAngle;
        /// <summary>Multiplicador de segmentos de las primitivas redondas.</summary>
        public float Detail = 1f;

        public MeshBuilder this[Material m] => For(m);

        public MeshBuilder For(Material m)
        {
            if (!_parts.TryGetValue(m, out var b))
            {
                b = new MeshBuilder { Detail = Detail };
                _parts[m] = b;
                _order.Add(m);
            }
            return b;
        }

        /// <summary>Material del mundo según el color (madera, piedra, paja...), con textura de detalle.</summary>
        public MeshBuilder For(Color c, float smoothness = 0.12f) => For(MaterialLibrary.World(c, smoothness));

        public bool IsEmpty
        {
            get
            {
                foreach (var b in _parts.Values) if (!b.IsEmpty) return false;
                return true;
            }
        }

        public List<MeshRenderer> Build(Transform parent, string name, bool castShadows = true, bool receiveShadows = true)
        {
            var list = new List<MeshRenderer>();
            foreach (var mat in _order)
            {
                var b = _parts[mat];
                if (b.IsEmpty) continue;
                var go = new GameObject(name + "_" + mat.name);
                go.transform.SetParent(parent, false);
                b.Smooth(SmoothAngle);
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(name);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mr.receiveShadows = receiveShadows;
                list.Add(mr);
            }
            return list;
        }

        public void Clear() { _parts.Clear(); _order.Clear(); }
    }

    /// <summary>Atajos para crear colisionadores primitivos sin geometría visible.</summary>
    public static class Colliders
    {
        public static BoxCollider Box(Transform parent, Vector3 localCenter, Vector3 size, Quaternion localRot, string name = "col")
        {
            var t = K.Child(parent, name, localCenter, localRot);
            var c = t.gameObject.AddComponent<BoxCollider>();
            c.size = size;
            return c;
        }

        public static BoxCollider Box(Transform parent, Vector3 localCenter, Vector3 size) => Box(parent, localCenter, size, Quaternion.identity);

        public static CapsuleCollider Capsule(Transform parent, Vector3 localCenter, float radius, float height, string name = "col")
        {
            var t = K.Child(parent, name, localCenter);
            var c = t.gameObject.AddComponent<CapsuleCollider>();
            c.radius = radius;
            c.height = height;
            c.direction = 1;
            return c;
        }

        public static SphereCollider Sphere(Transform parent, Vector3 localCenter, float radius, string name = "col")
        {
            var t = K.Child(parent, name, localCenter);
            var c = t.gameObject.AddComponent<SphereCollider>();
            c.radius = radius;
            return c;
        }
    }
}
