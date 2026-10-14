using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Zonas donde no debe crecer vegetación (estructuras, caminos de NPC, plazas).</summary>
    public static class Placement
    {
        private static List<Vector3> _circles;          // x, z, radio
        private static List<Vector4> _navSegments;      // ax, az, bx, bz

        private static void Init()
        {
            if (_circles != null) return;
            _circles = new List<Vector3>();
            foreach (var s in WorldLayout.Structures) _circles.Add(new Vector3(s.Pos.X, s.Pos.Z, RadiusFor(s.Kind)));
            foreach (var p in WorldShape.Pads) _circles.Add(new Vector3(p.Center.X, p.Center.Z, p.Radius + 2f));
            foreach (var r in WorldLayout.RestPoints) _circles.Add(new Vector3(r.Pos.X, r.Pos.Z, 4f));
            foreach (var c in WorldLayout.Chests) _circles.Add(new Vector3(c.Pos.X, c.Pos.Z, 2.5f));
            foreach (var r in WorldLayout.ResonanceSpots) _circles.Add(new Vector3(r.Pos.X, r.Pos.Z, 3f));
            foreach (var p in WorldLayout.Pickups) _circles.Add(new Vector3(p.Pos.X, p.Pos.Z, 2f));
            foreach (var l in WorldLayout.Locations) if (l.Landmark) _circles.Add(new Vector3(l.Pos.X, l.Pos.Z, 6f));
            _navSegments = new List<Vector4>();
            foreach (var e in WorldLayout.Edges)
            {
                var a = WorldLayout.FindNode(e.Key);
                var b = WorldLayout.FindNode(e.Value);
                if (a != null && b != null) _navSegments.Add(new Vector4(a.Pos.X, a.Pos.Z, b.Pos.X, b.Pos.Z));
            }
        }

        public static float RadiusFor(string kind)
        {
            switch (kind)
            {
                case "house": return 9f;
                case "workshop": return 9f;
                case "storehouse": return 8f;
                case "plaza": return 19f;
                case "fields": return 24f;
                case "giant_tree": return 17f;
                case "migration_stones": return 13f;
                case "gate": return 7f;
                case "guard_post": return 13f;
                case "training_dummy": return 4f;
                case "rope_bridge": return 6f;
                case "study_hut": return 10f;
                case "capsule_hollow": return 15f;
                case "grotto": return 13f;
                case "mojon": return 9f;
                case "boro_camp": return 15f;
                case "stone_arch": return 10f;
                case "ruins": return 33f;
                case "ruin_shelter": return 8f;
                case "lookout": return 8f;
                case "cloud_rock": return 7f;
                case "kolotapir_camp": return 11f;
                case "east_gate": return 12f;
                case "boar_den": return 6f;
                case "lantern": return 1.5f;
                default: return 3f;
            }
        }

        public static bool Blocked(float x, float z, float margin = 0f)
        {
            Init();
            for (int i = 0; i < _circles.Count; i++)
            {
                var c = _circles[i];
                float dx = x - c.x, dz = z - c.y, r = c.z + margin;
                if (dx * dx + dz * dz < r * r) return true;
            }
            var p = new P2(x, z);
            for (int i = 0; i < _navSegments.Count; i++)
            {
                var s = _navSegments[i];
                if (Mathf.Abs(x - s.x) > 40f && Mathf.Abs(x - s.z) > 40f) continue;
                if (OromMath.DistanceToSegment(p, new P2(s.x, s.y), new P2(s.z, s.w)) < 2.6f + margin) return true;
            }
            if (OromMath.DistanceToSegment(p, WorldShape.BridgeWest, WorldShape.BridgeEast) < 4f) return true;
            return false;
        }
    }

    /// <summary>Construye el contenido completo de un sector (terreno, vegetación, estructuras e interactivos).</summary>
    public static class SectorBuilder
    {
        private sealed class TerrainJob
        {
            public float[,] Heights;
            public float[,,] Alpha;
        }

        private static Mesh _grassMesh, _flowerMesh;
        private static Material[] _grassMats, _flowerMats;

        public static IEnumerator Build(WorldStreamer streamer, WorldStreamer.Sector s, TerrainLayer[] layers, bool immediate)
        {
            float x0 = s.Origin.x, z0 = s.Origin.z;
            var job = new TerrainJob();
            var task = Task.Run(() => ComputeTerrain(job, x0, z0));
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null)
            {
                Debug.LogException(task.Exception);
                yield break;
            }

            var root = new GameObject($"Sector_{s.X}_{s.Z}");
            root.transform.SetParent(streamer.transform, false);
            s.Root = root;

            // --- Terreno de Unity con capas de textura procedurales
            var td = new TerrainData();
            td.heightmapResolution = WorldStreamer.HeightRes;
            td.size = new Vector3(WorldShape.SectorSize, WorldShape.TerrainHeightRange, WorldShape.SectorSize);
            td.SetHeights(0, 0, job.Heights);
            td.alphamapResolution = WorldStreamer.AlphaRes;
            td.terrainLayers = layers;
            td.SetAlphamaps(0, 0, job.Alpha);
            var tgo = Terrain.CreateTerrainGameObject(td);
            tgo.name = "Terreno";
            tgo.transform.SetParent(root.transform, false);
            tgo.transform.position = new Vector3(x0, WorldShape.TerrainBaseY, z0);
            var terrain = tgo.GetComponent<Terrain>();
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 260f;
            terrain.drawInstanced = true;
            terrain.groupingID = 7;
            terrain.allowAutoConnect = true;
            var terrainMat = MaterialLibrary.TerrainMaterial();
            if (terrainMat != null) terrain.materialTemplate = terrainMat;
            s.Terrain = terrain;
            tgo.AddComponent<SurfaceTag>().Kind = SurfaceKind.Hierba;
            if (!immediate) yield return null;

            // --- Vegetación y rocas (lotes por material)
            var rng = new OromRandom(OromRandom.Hash(s.X, s.Z, 909));
            // Formas orgánicas con sombreado suave (las aristas de más de 45° se conservan: rocas facetadas).
            var mb = new ModelBuilder { SmoothAngle = 45f };
            var gathers = new List<Action>();
            int gatherIndex = 0;
            int built = 0;
            const float cell = 7.5f;
            int cells = Mathf.CeilToInt(WorldShape.SectorSize / cell);
            var colliders = K.Child(root.transform, "Colisiones");
            for (int cz = 0; cz < cells; cz++)
            {
                for (int cx = 0; cx < cells; cx++)
                {
                    float x = x0 + (cx + rng.Value()) * cell, z = z0 + (cz + rng.Value()) * cell;
                    float roll = rng.Value();
                    int seed = OromRandom.Hash((int)(x * 10f), (int)(z * 10f), 3);
                    ScatterAt(mb, colliders, x, z, roll, seed, s, ref gatherIndex, gathers, root.transform);
                }
                built++;
                if (!immediate && built % 6 == 0) yield return null;
            }
            mb.Build(root.transform, "Vegetacion", true, true);
            if (!immediate) yield return null;

            // --- Juncos en las orillas y recursos fijos
            AddShoreReeds(s, root.transform, ref gatherIndex);
            AddRuinFragments(s, root.transform, ref gatherIndex);
            AddFieldReeds(s, root.transform, ref gatherIndex);
            foreach (var g in gathers) g();
            if (!immediate) yield return null;

            // --- Estructuras de este sector
            foreach (var def in WorldLayout.Structures)
            {
                if (!InSector(def.Pos, s)) continue;
                try { StructureBuilder.Build(def, root.transform); }
                catch (Exception e) { Debug.LogError($"KOLOTOPIR: error construyendo {def.Kind} ({def.Param}): {e}"); }
                if (!immediate) yield return null;
            }

            // --- Interactivos y contenido persistente
            try { AddInteractables(s, root.transform); }
            catch (Exception e) { Debug.LogException(e); }
            if (!immediate) yield return null;

            // --- Hierba y flores instanciadas
            BuildInstances(s);
        }

        private static bool InSector(P2 p, WorldStreamer.Sector s)
        {
            return p.X >= s.Origin.x && p.X < s.Origin.x + WorldShape.SectorSize && p.Z >= s.Origin.z && p.Z < s.Origin.z + WorldShape.SectorSize;
        }

        private static void ComputeTerrain(TerrainJob job, float x0, float z0)
        {
            int hr = WorldStreamer.HeightRes, ar = WorldStreamer.AlphaRes;
            job.Heights = new float[hr, hr];
            float step = WorldShape.SectorSize / (hr - 1);
            for (int j = 0; j < hr; j++)
                for (int i = 0; i < hr; i++)
                {
                    float h = WorldShape.Height(x0 + i * step, z0 + j * step);
                    job.Heights[j, i] = Mathf.Clamp01((h - WorldShape.TerrainBaseY) / WorldShape.TerrainHeightRange);
                }
            job.Alpha = new float[ar, ar, 4];
            var w = new float[4];
            float astep = WorldShape.SectorSize / ar;
            for (int j = 0; j < ar; j++)
                for (int i = 0; i < ar; i++)
                {
                    WorldShape.SplatWeights(x0 + (i + 0.5f) * astep, z0 + (j + 0.5f) * astep, w);
                    for (int k = 0; k < 4; k++) job.Alpha[j, i, k] = w[k];
                }
        }

        // ---------------------------------------------------------------- Dispersión

        private static void ScatterAt(ModelBuilder mb, Transform colliders, float x, float z, float roll, int seed, WorldStreamer.Sector s,
            ref int gatherIndex, List<Action> gathers, Transform root)
        {
            if (!WorldShape.InWorld(x, z)) return;
            float h = WorldShape.Height(x, z);
            if (h < 2f || WorldShape.EdgeDistance(x, z) < 36f) return;
            var region = WorldShape.RegionAt(x, z);
            if (region == Region.Abismo || region == Region.BordeDelMundo) return;
            float slope = WorldShape.SlopeDegrees(x, z);
            if (WorldShape.TryGetLake(x, z, out var lake) && P2.Distance(new P2(x, z), lake.Center) < lake.Radius + 4f) return;
            float pathDist = WorldShape.DistanceToPath(x, z, 8f);
            bool blocked = Placement.Blocked(x, z);
            var pos = new Vector3(x, h - 0.05f, z);
            var rng = new OromRandom(seed);
            bool tribe = region == Region.TribuDelReverso;
            float nearPlaza = P2.Distance(new P2(x, z), WorldLayout.Plaza);
            float forest = 0.5f + 0.5f * Mathf.PerlinNoise(x * 0.012f + 31.7f, z * 0.012f + 11.3f);

            if (slope > 33f)
            {
                if (roll < 0.35f && !blocked)
                {
                    Props.Rock(mb, pos, new Vector3(rng.Range(1.4f, 3.2f), rng.Range(1f, 2.2f), rng.Range(1.4f, 3f)), seed, false);
                    if (roll < 0.1f) AddGather(gathers, root, s, ref gatherIndex, "lasca", pos + Vector3.up * 0.1f + new Vector3(1.2f, 0f, 0.4f));
                }
                return;
            }
            if (blocked || pathDist < 3.5f) return;

            float treeChance = tribe ? (nearPlaza < 75f ? 0.015f : 0.07f) : 0.11f * forest * forest * 1.4f;
            if (region == Region.ParadaAntigua) treeChance *= 0.5f;
            if (roll < treeChance && pathDist > 5f)
            {
                TreeKind kind = TreeKind.Roble;
                float k = rng.Value();
                if (!tribe && z > 700f && k < 0.45f) kind = TreeKind.Pino;
                else if (!tribe && k < 0.2f) kind = TreeKind.Memoria;
                else if (k < 0.35f) kind = TreeKind.Abedul;
                else if (tribe && k > 0.8f) kind = TreeKind.Pino;
                float scale = rng.Range(0.85f, 1.3f);
                Props.Tree(mb, pos, scale, seed, kind);
                var col = Colliders.Capsule(colliders, new Vector3(x, h + 1.5f, z), 0.35f * scale, 3.2f, "Tronco");
                col.gameObject.AddComponent<SurfaceTag>().Kind = SurfaceKind.Madera;
                if (kind == TreeKind.Roble && rng.Value() < 0.14f)
                {
                    var p = new Vector3(x, h + 1.25f, z) + Quaternion.Euler(0f, rng.Range(0f, 360f), 0f) * Vector3.forward * (0.42f * scale);
                    AddGather(gathers, root, s, ref gatherIndex, "resina", p);
                }
                return;
            }
            roll -= treeChance;
            if (roll < 0.06f)
            {
                bool berries = rng.Value() < (tribe ? 0.12f : 0.25f);
                Props.Bush(mb, pos, rng.Range(0.8f, 1.3f), seed, berries);
                if (berries) AddGather(gathers, root, s, ref gatherIndex, "bayas", pos + Vector3.up * 0.6f);
                return;
            }
            roll -= 0.06f;
            if (roll < 0.035f)
            {
                var size = new Vector3(rng.Range(0.7f, 2.4f), rng.Range(0.5f, 1.5f), rng.Range(0.7f, 2.2f));
                Props.Rock(mb, pos, size, seed, rng.Value() < 0.6f);
                if (size.x > 1.6f) Colliders.Sphere(colliders, pos + Vector3.up * size.y * 0.25f, size.x * 0.42f, "Roca").gameObject.AddComponent<SurfaceTag>().Kind = SurfaceKind.Piedra;
                if (rng.Value() < 0.3f) AddGather(gathers, root, s, ref gatherIndex, "hierba", pos + new Vector3(size.x * 0.6f + 0.3f, 0f, 0.2f));
                return;
            }
            roll -= 0.035f;
            if (roll < 0.012f) AddGather(gathers, root, s, ref gatherIndex, "hierba", pos);
        }

        private static void AddGather(List<Action> gathers, Transform root, WorldStreamer.Sector s, ref int index, string item, Vector3 pos)
        {
            string id = $"g{s.X}{s.Z}_{index++}";
            gathers.Add(() => GatherNodes.Create(root, id, item, pos));
        }

        private static void AddShoreReeds(WorldStreamer.Sector s, Transform root, ref int index)
        {
            foreach (var lake in WorldShape.Lakes)
            {
                int count = Mathf.RoundToInt(lake.Radius * 0.9f);
                var mb = new ModelBuilder();
                bool any = false;
                for (int i = 0; i < count; i++)
                {
                    float a = i * Mathf.PI * 2f / count + OromRandom.Hash(i, (int)lake.Radius) % 100 * 0.004f;
                    float r = lake.Radius + 0.6f + (OromRandom.Hash(i, 7) & 7) * 0.4f;
                    var p = new P2(lake.Center.X + Mathf.Cos(a) * r, lake.Center.Z + Mathf.Sin(a) * r);
                    if (!InSector(p, s) || Placement.Blocked(p.X, p.Z, -1.5f)) continue;
                    var pos = new Vector3(p.X, WorldShape.Height(p.X, p.Z) - 0.05f, p.Z);
                    Props.Reeds(mb, pos, i * 17 + (int)lake.Radius);
                    any = true;
                    if (i % 4 == 0) GatherNodes.Create(root, $"r{s.X}{s.Z}_{index++}", "fibra", pos + new Vector3(0.5f, 0f, 0.3f));
                }
                if (any) mb.Build(root, "Juncos", false, true);
            }
        }

        private static void AddFieldReeds(WorldStreamer.Sector s, Transform root, ref int index)
        {
            // Juncos en el borde norte de los campos de cultivo (misión de Tomé).
            if (!InSector(WorldLayout.Fields, s)) return;
            var mb = new ModelBuilder();
            for (int i = 0; i < 7; i++)
            {
                var p = WorldLayout.LocalToWorld(WorldLayout.Fields, 40f, -12f + i * 4f, 12.5f);
                var pos = new Vector3(p.X, WorldShape.Height(p.X, p.Z) - 0.05f, p.Z);
                Props.Reeds(mb, pos, 500 + i);
                GatherNodes.Create(root, $"f{s.X}{s.Z}_{index++}", "fibra", pos + new Vector3(0.4f, 0f, 0.4f));
            }
            mb.Build(root, "JuncosCampo", false, true);
        }

        private static void AddRuinFragments(WorldStreamer.Sector s, Transform root, ref int index)
        {
            if (!InSector(WorldLayout.Ruins, s)) return;
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad + 0.3f;
                float r = 9f + (i % 3) * 7f;
                var p = new P2(WorldLayout.Ruins.X + Mathf.Cos(a) * r, WorldLayout.Ruins.Z + Mathf.Sin(a) * r);
                GatherNodes.Create(root, $"fr{s.X}{s.Z}_{index++}", "fragmento", new Vector3(p.X, WorldShape.Height(p.X, p.Z), p.Z));
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad + 1.1f;
                var p = new P2(WorldLayout.Ruins.X + Mathf.Cos(a) * 27f, WorldLayout.Ruins.Z + Mathf.Sin(a) * 27f);
                GatherNodes.Create(root, $"ls{s.X}{s.Z}_{index++}", "lasca", new Vector3(p.X, WorldShape.Height(p.X, p.Z), p.Z));
            }
        }

        // ---------------------------------------------------------------- Interactivos

        private static void AddInteractables(WorldStreamer.Sector s, Transform root)
        {
            foreach (var p in WorldLayout.Pickups)
                if (InSector(p.Pos, s)) GatherNodes.CreateUnique(root, p);
            foreach (var c in WorldLayout.Chests)
                if (InSector(c.Pos, s)) Furniture.Chest(root, c);
            foreach (var r in WorldLayout.RestPoints)
            {
                if (!InSector(r.Pos, s)) continue;
                var go = new GameObject("Descanso_" + r.Id);
                go.transform.SetParent(root, false);
                go.transform.position = K.V3(r.Pos, WorldShape.Height(r.Pos.X, r.Pos.Z) + 0.6f + r.YOffset);
                go.AddComponent<RestPointInteractable>().Def = r;
            }
            foreach (var ex in GameDb.Examines.Values)
            {
                if (!InSector(ex.Pos, s)) continue;
                var go = new GameObject("Examinar_" + ex.Id);
                go.transform.SetParent(root, false);
                go.transform.position = K.V3(ex.Pos, WorldShape.Height(ex.Pos.X, ex.Pos.Z) + ex.YOffset);
                var spot = go.AddComponent<ExamineSpot>();
                spot.Def = ex;
                Furniture.ExamineVisual(go.transform, ex.Visual);
            }
            foreach (var rs in WorldLayout.ResonanceSpots)
            {
                if (!InSector(rs.Pos, s)) continue;
                var go = new GameObject("Resonancia_" + rs.ResonanceId);
                go.transform.SetParent(root, false);
                go.transform.position = K.V3(rs.Pos, WorldShape.Height(rs.Pos.X, rs.Pos.Z) + rs.YOffset);
                var spot = go.AddComponent<ResonanceSpot>();
                spot.Def = rs;
            }
            foreach (var c in WorldLayout.Climbs)
            {
                if (!InSector(c.Pos, s)) continue;
                var go = new GameObject("Trepar");
                go.transform.SetParent(root, false);
                float baseY = WorldShape.Height(c.Pos.X, c.Pos.Z);
                go.transform.position = K.V3(c.Pos, baseY + c.Bottom);
                go.transform.rotation = K.Yaw(c.Rot);
                var zone = go.AddComponent<ClimbZone>();
                float top = WorldShape.Height(c.TopRef.X, c.TopRef.Z) + c.Height;
                zone.Height = Mathf.Max(1f, top - (baseY + c.Bottom));
                Furniture.Vines(go.transform, zone.Height);
            }
            foreach (var sp in WorldLayout.Spawns)
            {
                if (!InSector(sp.Pos, s)) continue;
                var go = new GameObject("Criaturas_" + sp.Id);
                go.transform.SetParent(root, false);
                go.transform.position = K.V3(sp.Pos, WorldShape.Height(sp.Pos.X, sp.Pos.Z));
                go.AddComponent<CreatureSpawner>().Def = sp;
            }
            for (int t = 0; t < WorldLayout.Trails.Length; t++)
                FootprintTrail.Create(root, t, s.Origin, WorldShape.SectorSize);
        }

        // ---------------------------------------------------------------- Hierba instanciada

        private static void BuildInstances(WorldStreamer.Sector s)
        {
            if (_grassMesh == null)
            {
                _grassMesh = Props.GrassTuftMesh();
                _flowerMesh = Props.FlowerMesh();
                var grassTex = SurfaceTextures.GrassCard;
                _grassMats = new[]
                {
                    grassTex != null ? MaterialLibrary.Cutout(grassTex, new Color(0.42f, 0.56f, 0.22f), 0.05f, 0.4f) : MaterialLibrary.Lit(new Color(0.36f, 0.52f, 0.2f), 0.05f),
                    grassTex != null ? MaterialLibrary.Cutout(grassTex, new Color(0.52f, 0.64f, 0.26f), 0.05f, 0.4f) : MaterialLibrary.Lit(new Color(0.46f, 0.6f, 0.24f), 0.05f),
                };
                Color[] flowerFallback = { new Color(0.95f, 0.85f, 0.35f), new Color(0.95f, 0.95f, 0.92f), new Color(0.66f, 0.45f, 0.85f), new Color(0.9f, 0.5f, 0.6f) };
                _flowerMats = new Material[4];
                for (int i = 0; i < 4; i++)
                    _flowerMats[i] = SurfaceTextures.FlowerCards[i] != null ? MaterialLibrary.Cutout(SurfaceTextures.FlowerCards[i], Color.white, 0.1f, 0.4f) : MaterialLibrary.Lit(flowerFallback[i], 0.2f);
                foreach (var m in _grassMats) m.enableInstancing = true;
                foreach (var m in _flowerMats) m.enableInstancing = true;
            }
            var rng = new OromRandom(OromRandom.Hash(s.X, s.Z, 4444));
            var grass = new List<Matrix4x4>[2] { new List<Matrix4x4>(), new List<Matrix4x4>() };
            var flowers = new List<Matrix4x4>[4] { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
            int attempts = 5200;
            for (int i = 0; i < attempts; i++)
            {
                float x = s.Origin.x + rng.Value() * WorldShape.SectorSize, z = s.Origin.z + rng.Value() * WorldShape.SectorSize;
                float h = WorldShape.Height(x, z);
                if (h < 2f || WorldShape.EdgeDistance(x, z) < 36f) continue;
                if (WorldShape.IsUnderwater(x, z) || WorldShape.SlopeDegrees(x, z) > 26f) continue;
                if (WorldShape.DistanceToPath(x, z, 3f) < 2.4f) continue;
                if (P2.Distance(new P2(x, z), WorldLayout.Plaza) < 15f) continue;
                var region = WorldShape.RegionAt(x, z);
                var m = Matrix4x4.TRS(new Vector3(x, h - 0.05f, z), Quaternion.Euler(0f, rng.Value() * 360f, 0f), Vector3.one * rng.Range(0.7f, 1.35f));
                bool meadow = region == Region.PradosQueRecuerdan || region == Region.CaminoDelEste;
                if (meadow && rng.Value() < 0.32f) flowers[rng.Range(0, 4)].Add(m);
                else grass[rng.Range(0, 2)].Add(m);
            }
            for (int i = 0; i < 2; i++) AddBatch(s, _grassMesh, _grassMats[i], grass[i]);
            for (int i = 0; i < 4; i++) AddBatch(s, _flowerMesh, _flowerMats[i], flowers[i]);
        }

        private static void AddBatch(WorldStreamer.Sector s, Mesh mesh, Material mat, List<Matrix4x4> list)
        {
            if (list.Count == 0) return;
            s.Instances.Add(new WorldStreamer.InstanceBatch
            {
                Mesh = mesh, Material = mat, Matrices = list.ToArray(), Count = list.Count,
                Bounds = new Bounds(s.Origin + new Vector3(WorldShape.SectorSize * 0.5f, 20f, WorldShape.SectorSize * 0.5f), new Vector3(WorldShape.SectorSize, 120f, WorldShape.SectorSize)),
            });
        }
    }
}
