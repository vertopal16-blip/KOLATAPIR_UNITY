using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kolotopir
{
    public enum SurfaceKind { Hierba, Tierra, Madera, Piedra, Agua }

    /// <summary>Tipo de superficie (para pasos y sonidos).</summary>
    public sealed class SurfaceTag : MonoBehaviour
    {
        public SurfaceKind Kind = SurfaceKind.Hierba;
    }

    /// <summary>Superficie estrecha: caminar por ella exige mantener el equilibrio.</summary>
    public sealed class BalanceBeam : MonoBehaviour { }

    /// <summary>Pared trepable diseñada para ello (doc: saltar y trepar en puntos diseñados). Su frente mira a +Z local.</summary>
    public sealed class ClimbZone : MonoBehaviour
    {
        public static readonly List<ClimbZone> All = new List<ClimbZone>();
        public float Height = 5f;
        public float Width = 1.8f;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>¿Puede el jugador en <paramref name="pos"/> mirando hacia <paramref name="forward"/> agarrarse?</summary>
        public bool CanGrab(Vector3 pos, Vector3 forward)
        {
            var local = transform.InverseTransformPoint(pos);
            if (Mathf.Abs(local.x) > Width * 0.5f + 0.35f) return false;
            if (local.z < -0.2f || local.z > 1.25f) return false;
            if (local.y < -1.2f || local.y > Height - 0.3f) return false;
            return Vector3.Dot(new Vector3(forward.x, 0f, forward.z).normalized, -transform.forward) > 0.45f;
        }

        public float TopY => transform.position.y + Height;
    }

    /// <summary>Lagos: superficie de agua y zona de nado (doc: nadar en zonas específicamente diseñadas).</summary>
    public static class WaterSystem
    {
        public static void Build(Transform parent)
        {
            var mat = MaterialLibrary.Transparent(new Color(0.24f, 0.45f, 0.58f, 0.72f), 0.92f);
            foreach (var lake in WorldShape.Lakes)
            {
                var b = new MeshBuilder();
                int seg = 56;
                float r = lake.Radius * 1.18f;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    b.Tri(Vector3.zero, new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r), new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r), Vector3.up);
                }
                var go = new GameObject("Lago");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(lake.Center.X, lake.WaterLevel, lake.Center.Z);
                go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("Agua");
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                go.AddComponent<WaterRipple>();
            }
        }

        /// <summary>Nivel del agua en un punto, o NaN si no hay lago.</summary>
        public static float LevelAt(float x, float z)
        {
            return WorldShape.TryGetLake(x, z, out var lake) ? lake.WaterLevel : float.NaN;
        }
    }

    /// <summary>Oscilación suave del agua.</summary>
    public sealed class WaterRipple : MonoBehaviour
    {
        private float _baseY;
        private void Start() => _baseY = transform.position.y;
        private void Update()
        {
            var p = transform.position;
            p.y = _baseY + Mathf.Sin(Time.time * 0.6f) * 0.03f;
            transform.position = p;
        }
    }

    /// <summary>
    /// Huellas que solo se ven con la Escucha de Resonancias (doc: la Resonancia detecta rastros invisibles).
    /// Cada sector genera sus tramos; el oyente ajusta su visibilidad.
    /// </summary>
    public sealed class FootprintTrail : MonoBehaviour
    {
        public static readonly List<FootprintTrail> All = new List<FootprintTrail>();
        private static Texture2D _tex;
        public int TrailIndex;
        private Material _mat;
        private float _alpha;

        public static void Create(Transform root, int trailIndex, Vector3 origin, float size)
        {
            var pts = WorldShape.SamplePolyline(WorldLayout.Trails[trailIndex], 1.35f);
            var b = new MeshBuilder();
            bool any = false;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p = pts[i];
                if (p.X < origin.x || p.X >= origin.x + size || p.Z < origin.z || p.Z >= origin.z + size) continue;
                var next = pts[i + 1];
                var dir = new Vector3(next.X - p.X, 0f, next.Z - p.Z).normalized;
                var side = Vector3.Cross(Vector3.up, dir) * ((i % 2 == 0) ? 0.17f : -0.17f);
                // Dos rastros paralelos (dos personas), y en los Prados, un tercero de zancada larga.
                for (int person = 0; person < (trailIndex == 1 ? 3 : 2); person++)
                {
                    if (person == 2 && i % 2 == 1) continue;
                    var offset = Vector3.Cross(Vector3.up, dir) * (person == 0 ? -0.55f : person == 1 ? 0.55f : 1.6f);
                    var c = new Vector3(p.X, 0f, p.Z) + side + offset;
                    c.y = WorldShape.Height(c.x, c.z) + 0.04f;
                    float len = person == 2 ? 0.34f : 0.26f, wid = person == 2 ? 0.16f : 0.12f;
                    var right = Vector3.Cross(Vector3.up, dir) * wid;
                    var fwd = dir * len;
                    b.Quad(c - right - fwd, c - right + fwd, c + right + fwd, c + right - fwd, Vector3.up);
                    any = true;
                }
            }
            if (!any) return;
            if (_tex == null) _tex = TextureGen.Footprint();
            var go = new GameObject("Huellas_" + trailIndex);
            go.transform.SetParent(root, false);
            var mesh = b.ToMesh("Huellas");
            // UV por huella: el constructor genera UV planas; las recalculamos por quad para el sprite.
            var uvs = new List<Vector2>();
            for (int i = 0; i < mesh.vertexCount; i += 6)
            {
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0));
            }
            if (uvs.Count == mesh.vertexCount) mesh.SetUVs(0, uvs);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            var trail = go.AddComponent<FootprintTrail>();
            trail.TrailIndex = trailIndex;
            trail._mat = MaterialLibrary.UnlitTransparent(K.WithA(Pal.ResonanceSoft, 0f), _tex);
            mr.sharedMaterial = trail._mat;
            mr.enabled = false;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        public void SetReveal(float a)
        {
            if (Mathf.Abs(a - _alpha) < 0.005f) return;
            _alpha = a;
            var mr = GetComponent<MeshRenderer>();
            mr.enabled = a > 0.01f;
            MaterialLibrary.SetColor(_mat, K.WithA(Pal.ResonanceSoft, a * 0.85f));
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }

    /// <summary>Nodos de recolección (renovables) y objetos únicos repartidos por el mundo.</summary>
    public static class GatherNodes
    {
        private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
        private static Transform _templateRoot;

        private static int CountFor(string item)
        {
            switch (item)
            {
                case "fibra": return 2;
                case "bayas": return 2;
                case "lasca": return 2;
                default: return 1;
            }
        }

        public static void Create(Transform root, string id, string item, Vector3 pos)
        {
            var go = new GameObject("Recurso_" + item);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var vis = Visual(item);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.Euler(0f, (id.GetHashCode() & 359), 0f);
            var p = go.AddComponent<PickupItem>();
            p.Id = id;
            p.ItemId = item;
            p.Count = CountFor(item);
            p.Renewable = true;
            if (item == "fibra") p.VerbOverride = "Cortar";
        }

        public static void CreateUnique(Transform root, PickupDef def)
        {
            var go = new GameObject("Objeto_" + def.Id);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(def.Pos.X, WorldShape.Height(def.Pos.X, def.Pos.Z) + def.YOffset, def.Pos.Z);
            var holder = new GameObject("Visual");
            holder.transform.SetParent(go.transform, false);
            var vis = Visual(def.ItemId);
            vis.transform.SetParent(holder.transform, false);
            FX.Sparkle(holder.transform, Vector3.up * 0.15f, new Color(1f, 0.9f, 0.6f, 0.9f), 0.2f, 4f);
            var p = go.AddComponent<PickupItem>();
            p.Id = def.Id;
            p.ItemId = def.ItemId;
            p.Count = def.Count;
            p.Renewable = false;
        }

        private static GameObject Visual(string item)
        {
            if (!Templates.TryGetValue(item, out var tpl) || tpl == null)
            {
                if (_templateRoot == null)
                {
                    var r = new GameObject("PlantillasRecursos");
                    r.SetActive(false);
                    Object.DontDestroyOnLoad(r);
                    _templateRoot = r.transform;
                }
                tpl = new GameObject("tpl_" + item);
                tpl.transform.SetParent(_templateRoot, false);
                var mb = new ModelBuilder();
                switch (item)
                {
                    case "fibra": Props.Reeds(mb, Vector3.zero, 3); break;
                    case "hierba": Props.Herb(mb, Vector3.zero, 5); break;
                    case "bayas":
                        var berry = MaterialLibrary.Lit(new Color(0.62f, 0.12f, 0.3f), 0.5f);
                        for (int i = 0; i < 6; i++) mb.For(berry).Sphere(new Vector3(Mathf.Cos(i) * 0.12f, (i % 3) * 0.06f, Mathf.Sin(i) * 0.12f), Vector3.one * 0.06f, 5, 3, Quaternion.identity);
                        break;
                    case "resina":
                        mb.For(MaterialLibrary.Emissive(new Color(0.85f, 0.6f, 0.15f), new Color(0.5f, 0.3f, 0.05f), 0.6f)).Rock(Vector3.zero, new Vector3(0.18f, 0.24f, 0.12f), Quaternion.identity, 9, 0.3f, false);
                        break;
                    case "lasca":
                        for (int i = 0; i < 3; i++) mb.For(MaterialLibrary.Lit(new Color(0.5f, 0.49f, 0.47f), 0.2f)).Rock(new Vector3(i * 0.18f - 0.18f, 0.06f, (i % 2) * 0.12f), new Vector3(0.22f, 0.14f, 0.18f), Quaternion.identity, 20 + i, 0.4f, false);
                        break;
                    case "fragmento":
                        mb.For(MaterialLibrary.Emissive(new Color(0.7f, 0.6f, 0.9f), Pal.Resonance, 1.2f)).Cylinder(Vector3.zero, 0.07f, 0.0f, 0.38f, 4, Quaternion.Euler(12f, 0f, 18f));
                        mb.For(MaterialLibrary.Emissive(new Color(0.7f, 0.6f, 0.9f), Pal.Resonance, 1.2f)).Cylinder(new Vector3(0.1f, 0f, 0.05f), 0.05f, 0.0f, 0.24f, 4, Quaternion.Euler(-10f, 0f, -25f));
                        break;
                    case "cuchara_doblada":
                        mb.For(MaterialLibrary.Lit(new Color(0.75f, 0.75f, 0.72f), 0.7f, 0.8f)).Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.03f, 0.015f, 0.2f), Quaternion.Euler(18f, 30f, 0f));
                        mb.For(MaterialLibrary.Lit(new Color(0.75f, 0.75f, 0.72f), 0.7f, 0.8f)).Sphere(new Vector3(0.05f, 0.04f, 0.09f), new Vector3(0.05f, 0.015f, 0.07f), 6, 3, Quaternion.Euler(0f, 30f, 0f));
                        break;
                    case "boton_nacar":
                        mb.For(MaterialLibrary.Lit(new Color(0.95f, 0.93f, 0.9f), 0.9f)).Cylinder(Vector3.zero, 0.05f, 0.05f, 0.015f, 10, Quaternion.Euler(70f, 0f, 0f));
                        break;
                    default:
                        mb.For(MaterialLibrary.Emissive(Pal.Warm, Pal.Warm, 0.6f)).Sphere(Vector3.up * 0.1f, Vector3.one * 0.09f, 8, 5, Quaternion.identity);
                        break;
                }
                mb.Build(tpl.transform, "Recurso", false, true);
                Templates[item] = tpl;
            }
            var inst = Object.Instantiate(tpl);
            inst.name = "Visual_" + item;
            return inst;
        }
    }

    /// <summary>Mobiliario compartido: cofres, marcas de examen, enredaderas.</summary>
    public static class Furniture
    {
        public static void Chest(Transform root, ChestDef def)
        {
            var go = new GameObject("Cofre_" + def.Id);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(def.Pos.X, WorldShape.Height(def.Pos.X, def.Pos.Z), def.Pos.Z);
            go.transform.rotation = K.Yaw(def.Rot);
            var mb = new ModelBuilder();
            mb.For(Pal.Wood).Box(new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.55f), Quaternion.identity);
            mb.For(Pal.Gold).Box(new Vector3(0f, 0.25f, 0.28f), new Vector3(0.12f, 0.14f, 0.03f), Quaternion.identity);
            mb.For(Pal.WoodDark).Box(new Vector3(-0.4f, 0.25f, 0f), new Vector3(0.06f, 0.52f, 0.57f), Quaternion.identity);
            mb.For(Pal.WoodDark).Box(new Vector3(0.4f, 0.25f, 0f), new Vector3(0.06f, 0.52f, 0.57f), Quaternion.identity);
            mb.Build(go.transform, "Cofre");
            var lidPivot = K.Child(go.transform, "Tapa", new Vector3(0f, 0.5f, -0.27f));
            var lid = new ModelBuilder();
            lid.For(Pal.WoodLight).Box(new Vector3(0f, 0.08f, 0.27f), new Vector3(0.92f, 0.16f, 0.57f), Quaternion.identity);
            lid.Build(lidPivot, "Tapa");
            Colliders.Box(go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.6f, 0.55f));
            var c = go.AddComponent<ChestInteractable>();
            c.Def = def;
            c.Lid = lidPivot;
        }

        public static void ExamineVisual(Transform parent, string visual)
        {
            var holder = K.Child(parent, "Visual");
            switch (visual)
            {
                case "papel":
                {
                    var mb = new ModelBuilder();
                    mb.For(new Color(0.92f, 0.88f, 0.76f)).Box(new Vector3(0f, -0.05f, 0f), new Vector3(0.24f, 0.01f, 0.32f), Quaternion.Euler(0f, 15f, 0f));
                    mb.Build(holder, "Papel", false);
                    FX.Sparkle(holder, Vector3.zero, new Color(1f, 0.9f, 0.7f, 0.7f), 0.15f, 2f);
                    break;
                }
                case "gubia":
                {
                    var mb = new ModelBuilder();
                    mb.For(Pal.WoodLight).Cylinder(new Vector3(-0.08f, -0.05f, 0f), 0.02f, 0.02f, 0.14f, 6, Quaternion.Euler(0f, 0f, 90f));
                    mb.For(MaterialLibrary.Lit(new Color(0.7f, 0.72f, 0.75f), 0.7f, 0.8f)).Box(new Vector3(0.06f, -0.05f, 0f), new Vector3(0.12f, 0.012f, 0.025f), Quaternion.identity);
                    mb.For(Pal.Wood).Rock(new Vector3(0.05f, -0.02f, 0.12f), new Vector3(0.12f, 0.1f, 0.18f), Quaternion.identity, 4, 0.2f, false);
                    mb.Build(holder, "Gubia", false);
                    FX.Sparkle(holder, Vector3.zero, new Color(1f, 0.9f, 0.7f, 0.7f), 0.15f, 2f);
                    break;
                }
                case "marca":
                {
                    var mb = new ModelBuilder();
                    var glow = MaterialLibrary.Emissive(new Color(0.8f, 0.7f, 0.95f), Pal.Resonance, 0.8f);
                    mb.For(glow).Box(Vector3.zero, new Vector3(0.18f, 0.18f, 0.02f), Quaternion.Euler(0f, 0f, 45f));
                    mb.Build(holder, "Marca", false);
                    FX.Sparkle(holder, Vector3.zero, K.WithA(Pal.ResonanceSoft, 0.7f), 0.25f, 2f);
                    break;
                }
                default:
                    FX.Sparkle(holder, Vector3.zero, new Color(1f, 0.9f, 0.7f, 0.5f), 0.3f, 1.5f);
                    break;
            }
        }

        public static void Vines(Transform parent, float height)
        {
            var mb = new ModelBuilder();
            var leaf = MaterialLibrary.Lit(new Color(0.24f, 0.45f, 0.2f), 0.1f);
            var stem = MaterialLibrary.Lit(new Color(0.32f, 0.3f, 0.16f), 0.1f);
            var rng = new OromRandom(77);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.7f + i * 0.35f;
                mb.For(stem).Box(new Vector3(x, height * 0.5f, 0.06f), new Vector3(0.05f, height, 0.04f), Quaternion.Euler(0f, 0f, rng.Range(-3f, 3f)));
                for (float y = 0.3f; y < height; y += 0.45f)
                    mb.For(leaf).DoubleTri(new Vector3(x, y, 0.08f), new Vector3(x + 0.18f, y + 0.1f, 0.1f), new Vector3(x + 0.05f, y + 0.25f, 0.09f));
            }
            mb.Build(parent, "Enredadera", false);
        }
    }
}
