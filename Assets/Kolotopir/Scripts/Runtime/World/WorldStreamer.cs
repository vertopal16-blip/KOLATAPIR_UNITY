using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kolotopir
{
    /// <summary>
    /// Divide Orom en sectores de 250 × 250 m (doc: referencia inicial). Carga el sector actual y los
    /// cercanos con todo su contenido, y simplifica los lejanos a una malla de baja resolución.
    /// Los cambios persistentes (cofres, objetos, criaturas, Cápsulas) viven en GameState, no en los sectores.
    /// </summary>
    public sealed class WorldStreamer : MonoBehaviour
    {
        public const float LoadDistance = 290f;
        public const float UnloadDistance = 420f;
        public const int HeightRes = 129;
        public const int AlphaRes = 128;
        public const int FarRes = 33;
        public const int ColorRes = 32;

        public sealed class Sector
        {
            public int X, Z, Index;
            public Vector3 Origin;
            public GameObject Far;
            public GameObject Root;
            public Terrain Terrain;
            public bool Loaded, Loading;
            public readonly List<InstanceBatch> Instances = new List<InstanceBatch>();
            public Color[] ColorMap;
            public float DistanceToFocus;
            public Bounds Bounds => new Bounds(Origin + new Vector3(WorldShape.SectorSize * 0.5f, 0f, WorldShape.SectorSize * 0.5f), new Vector3(WorldShape.SectorSize, 400f, WorldShape.SectorSize));
        }

        public sealed class InstanceBatch
        {
            public Mesh Mesh;
            public Material Material;
            public Matrix4x4[] Matrices;
            public int Count;
            public Bounds Bounds;
        }

        private Sector[] _sectors;
        private TerrainLayer[] _layers;
        private readonly List<Sector> _queue = new List<Sector>();
        private Coroutine _loader;
        private Transform _farRoot;

        public Vector3 Focus;
        public bool HasFocus;
        public int LoadedCount { get; private set; }
        public bool Ready { get; private set; }
        public float VegetationDensity = 1f;
        public float GrassDistance = 140f;

        public event Action<Sector> SectorLoaded;
        public event Action<Sector> SectorUnloading;

        public IReadOnlyList<Sector> Sectors => _sectors;

        // ---------------------------------------------------------------- Inicialización

        public IEnumerator Init()
        {
            int n = WorldShape.SectorsPerSide;
            _sectors = new Sector[n * n];
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    var s = new Sector { X = x, Z = z, Index = WorldShape.SectorIndex(x, z), Origin = new Vector3(x * WorldShape.SectorSize, 0f, z * WorldShape.SectorSize) };
                    _sectors[s.Index] = s;
                }

            _layers = new[]
            {
                Layer(TextureGen.GroundKind.Hierba, 9f),
                Layer(TextureGen.GroundKind.Pradera, 11f),
                Layer(TextureGen.GroundKind.Tierra, 6f),
                Layer(TextureGen.GroundKind.Roca, 12f),
            };
            yield return null;

            // Datos lejanos (alturas y colores) en segundo plano: la función del terreno es pura.
            var farHeights = new float[_sectors.Length][];
            var task = Task.Run(() =>
            {
                for (int i = 0; i < _sectors.Length; i++)
                {
                    var s = _sectors[i];
                    farHeights[i] = FarHeights(s.Origin.x, s.Origin.z);
                    s.ColorMap = ColorMap(s.Origin.x, s.Origin.z);
                }
            });
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null) Debug.LogException(task.Exception);

            _farRoot = K.Child(transform, "Lejanos");
            for (int i = 0; i < _sectors.Length; i++)
            {
                BuildFar(_sectors[i], farHeights[i]);
                if (i % 4 == 3) yield return null;
            }
            Ready = true;
        }

        private static TerrainLayer Layer(TextureGen.GroundKind kind, float tile)
        {
            var l = new TerrainLayer
            {
                diffuseTexture = TextureGen.Ground(kind),
                tileSize = new Vector2(tile, tile),
                smoothness = kind == TextureGen.GroundKind.Roca ? 0.15f : 0.05f,
                metallic = 0f,
            };
            l.name = "Capa_" + kind;
            return l;
        }

        private static float[] FarHeights(float x0, float z0)
        {
            var h = new float[FarRes * FarRes];
            float step = WorldShape.SectorSize / (FarRes - 1);
            for (int j = 0; j < FarRes; j++)
                for (int i = 0; i < FarRes; i++)
                    h[j * FarRes + i] = WorldShape.Height(x0 + i * step, z0 + j * step);
            return h;
        }

        private static readonly Color[] LayerColors =
        {
            new Color(0.33f, 0.46f, 0.19f), new Color(0.42f, 0.58f, 0.22f), new Color(0.5f, 0.4f, 0.27f), new Color(0.47f, 0.45f, 0.42f),
        };

        private static Color[] ColorMap(float x0, float z0)
        {
            var c = new Color[ColorRes * ColorRes];
            var w = new float[4];
            float step = WorldShape.SectorSize / ColorRes;
            for (int j = 0; j < ColorRes; j++)
                for (int i = 0; i < ColorRes; i++)
                {
                    float x = x0 + (i + 0.5f) * step, z = z0 + (j + 0.5f) * step;
                    WorldShape.SplatWeights(x, z, w);
                    var col = LayerColors[0] * w[0] + LayerColors[1] * w[1] + LayerColors[2] * w[2] + LayerColors[3] * w[3];
                    if (WorldShape.IsUnderwater(x, z)) col = new Color(0.25f, 0.42f, 0.55f);
                    col.a = 1f;
                    c[j * ColorRes + i] = col;
                }
            return c;
        }

        private void BuildFar(Sector s, float[] heights)
        {
            var mesh = new Mesh { name = "Far_" + s.Index };
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float step = WorldShape.SectorSize / (FarRes - 1);
            for (int j = 0; j < FarRes; j++)
                for (int i = 0; i < FarRes; i++)
                {
                    verts.Add(new Vector3(i * step, heights[j * FarRes + i] - 0.15f, j * step));
                    uvs.Add(new Vector2(i / (float)(FarRes - 1), j / (float)(FarRes - 1)));
                }
            for (int j = 0; j < FarRes - 1; j++)
                for (int i = 0; i < FarRes - 1; i++)
                {
                    int a = j * FarRes + i, b = a + 1, c = a + FarRes, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            // Faldón para tapar rendijas entre sectores lejanos y cercanos.
            int skirtStart = verts.Count;
            var border = new List<int>();
            for (int i = 0; i < FarRes; i++) border.Add(i);
            for (int j = 1; j < FarRes; j++) border.Add(j * FarRes + FarRes - 1);
            for (int i = FarRes - 2; i >= 0; i--) border.Add((FarRes - 1) * FarRes + i);
            for (int j = FarRes - 2; j > 0; j--) border.Add(j * FarRes);
            // Copias propias del borde para que las normales de la superficie no se mezclen con el faldón.
            for (int k = 0; k < border.Count; k++)
            {
                var v = verts[border[k]];
                var uv = uvs[border[k]];
                verts.Add(v); uvs.Add(uv);
                verts.Add(new Vector3(v.x, v.y - 6f, v.z)); uvs.Add(uv);
            }
            for (int k = 0; k < border.Count; k++)
            {
                int kn = (k + 1) % border.Count;
                int a = skirtStart + k * 2, b = skirtStart + kn * 2;
                int a2 = a + 1, b2 = b + 1;
                tris.Add(a); tris.Add(b); tris.Add(a2);
                tris.Add(b); tris.Add(b2); tris.Add(a2);
                tris.Add(a); tris.Add(a2); tris.Add(b);
                tris.Add(b); tris.Add(a2); tris.Add(b2);
            }
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var tex = new Texture2D(ColorRes, ColorRes, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "FarColor_" + s.Index };
            tex.SetPixels(s.ColorMap);
            tex.Apply(false);

            var go = new GameObject("Lejano_" + s.X + "_" + s.Z);
            go.transform.SetParent(_farRoot, false);
            go.transform.position = s.Origin;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Textured(tex, Color.white, 0f);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            s.Far = go;
        }

        // ---------------------------------------------------------------- Consultas

        public Sector SectorAt(float x, float z)
        {
            if (_sectors == null || !WorldShape.InWorld(x, z)) return null;
            WorldShape.SectorOf(x, z, out int sx, out int sz);
            return _sectors[WorldShape.SectorIndex(sx, sz)];
        }

        public Terrain TerrainAt(float x, float z)
        {
            var s = SectorAt(x, z);
            return s != null && s.Loaded ? s.Terrain : null;
        }

        public bool IsLoadedAt(float x, float z)
        {
            var s = SectorAt(x, z);
            return s != null && s.Loaded;
        }

        public Transform SectorRoot(float x, float z)
        {
            var s = SectorAt(x, z);
            return s != null && s.Loaded ? s.Root.transform : null;
        }

        private static float DistanceToSector(Sector s, Vector3 p)
        {
            float cx = Mathf.Clamp(p.x, s.Origin.x, s.Origin.x + WorldShape.SectorSize);
            float cz = Mathf.Clamp(p.z, s.Origin.z, s.Origin.z + WorldShape.SectorSize);
            float dx = p.x - cx, dz = p.z - cz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ---------------------------------------------------------------- Carga

        /// <summary>Carga de forma inmediata (bloqueante) los sectores alrededor de un punto: pantallas de carga.</summary>
        public IEnumerator LoadAround(Vector3 p, float radius)
        {
            Focus = p;
            HasFocus = true;
            var list = new List<Sector>();
            foreach (var s in _sectors) if (!s.Loaded && DistanceToSector(s, p) < radius) list.Add(s);
            list.Sort((a, b) => DistanceToSector(a, p).CompareTo(DistanceToSector(b, p)));
            foreach (var s in list)
            {
                if (s.Loaded || s.Loading) continue;
                s.Loading = true;
                yield return SectorBuilder.Build(this, s, _layers, true);
                FinishLoad(s);
            }
            // Algún sector puede estar construyéndose en segundo plano: esperar a que termine.
            float waited = 0f;
            while (waited < 20f)
            {
                bool pending = false;
                foreach (var s in list) if (!s.Loaded && s.Loading) pending = true;
                if (!pending) break;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Update()
        {
            if (!Ready || !HasFocus) return;
            var p = Focus;
            foreach (var s in _sectors)
            {
                s.DistanceToFocus = DistanceToSector(s, p);
                if (!s.Loaded && !s.Loading && s.DistanceToFocus < LoadDistance && !_queue.Contains(s)) _queue.Add(s);
                if (s.Loaded && s.DistanceToFocus > UnloadDistance) Unload(s);
            }
            if (_loader == null && _queue.Count > 0)
            {
                _queue.Sort((a, b) => a.DistanceToFocus.CompareTo(b.DistanceToFocus));
                var next = _queue[0];
                _queue.RemoveAt(0);
                if (!next.Loaded && !next.Loading && next.DistanceToFocus < LoadDistance + 40f) _loader = StartCoroutine(LoadAsync(next));
            }
            DrawInstances(p);
        }

        private IEnumerator LoadAsync(Sector s)
        {
            s.Loading = true;
            yield return SectorBuilder.Build(this, s, _layers, false);
            FinishLoad(s);
            _loader = null;
        }

        private void FinishLoad(Sector s)
        {
            s.Loading = false;
            s.Loaded = true;
            if (s.Far != null) s.Far.SetActive(false);
            LoadedCount++;
            ConnectNeighbors();
            SectorLoaded?.Invoke(s);
        }

        private void Unload(Sector s)
        {
            SectorUnloading?.Invoke(s);
            s.Loaded = false;
            if (s.Root != null)
            {
                if (s.Terrain != null && s.Terrain.terrainData != null) Destroy(s.Terrain.terrainData);
                Destroy(s.Root);
            }
            foreach (var b in s.Instances) b.Matrices = null;
            s.Instances.Clear();
            s.Root = null;
            s.Terrain = null;
            if (s.Far != null) s.Far.SetActive(true);
            LoadedCount--;
        }

        private void ConnectNeighbors()
        {
            int n = WorldShape.SectorsPerSide;
            foreach (var s in _sectors)
            {
                if (!s.Loaded || s.Terrain == null) continue;
                Terrain T(int x, int z)
                {
                    if (x < 0 || z < 0 || x >= n || z >= n) return null;
                    var o = _sectors[WorldShape.SectorIndex(x, z)];
                    return o.Loaded ? o.Terrain : null;
                }
                s.Terrain.SetNeighbors(T(s.X - 1, s.Z), T(s.X, s.Z + 1), T(s.X + 1, s.Z), T(s.X, s.Z - 1));
            }
        }

        public void UnloadAll()
        {
            if (_loader != null) { StopCoroutine(_loader); _loader = null; }
            _queue.Clear();
            if (_sectors == null) return;
            foreach (var s in _sectors)
            {
                if (s.Loaded) Unload(s);
                else if (s.Loading)
                {
                    // Sector a medio construir: se descarta lo que hubiera.
                    if (s.Root != null)
                    {
                        if (s.Terrain != null && s.Terrain.terrainData != null) Destroy(s.Terrain.terrainData);
                        Destroy(s.Root);
                    }
                    s.Root = null;
                    s.Terrain = null;
                    s.Instances.Clear();
                    if (s.Far != null) s.Far.SetActive(true);
                }
                s.Loading = false;
            }
        }

        // ---------------------------------------------------------------- Instanciación (hierba y flores)

        private void DrawInstances(Vector3 p)
        {
            if (VegetationDensity <= 0.01f) return;
            foreach (var s in _sectors)
            {
                if (!s.Loaded || s.DistanceToFocus > GrassDistance) continue;
                foreach (var b in s.Instances)
                {
                    if (b.Matrices == null || b.Count == 0) continue;
                    int count = Mathf.Max(1, Mathf.RoundToInt(b.Count * VegetationDensity));
                    var rp = new RenderParams(b.Material)
                    {
                        shadowCastingMode = ShadowCastingMode.Off,
                        receiveShadows = true,
                        worldBounds = b.Bounds,
                    };
                    for (int start = 0; start < count; start += 1000)
                        Graphics.RenderMeshInstanced(rp, b.Mesh, 0, b.Matrices, Mathf.Min(1000, count - start), start);
                }
            }
        }
    }
}
