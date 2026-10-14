using Kolotopir.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kolotopir
{
    /// <summary>
    /// Día y noche, cielo, niebla, luz ambiental, mar de nubes y horizontes con siluetas de regiones
    /// lejanas (doc: "horizontes con siluetas de regiones lejanas que despierten curiosidad").
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        public Light Sun;
        public Light Moon;
        public float Night { get; private set; }
        public Transform QuestCloud { get; private set; }
        public float ResonanceTint;

        private Material _sky;
        private Material _cloudSea, _cloudSea2;
        private ParticleSystem _stars;
        private Material _starMat;
        private Region _region = Region.TribuDelReverso;

        private struct Key
        {
            public float Hour;
            public Color Fog, SunColor, AmbSky, AmbEq, AmbGround, SkyTint;
            public float SunIntensity, Exposure;
            public Key(float h, Color fog, Color sun, float sunI, Color sky, Color eq, Color ground, Color tint, float exposure)
            {
                Hour = h; Fog = fog; SunColor = sun; SunIntensity = sunI; AmbSky = sky; AmbEq = eq; AmbGround = ground; SkyTint = tint; Exposure = exposure;
            }
        }

        private static readonly Key[] Keys =
        {
            new Key(0f, new Color(0.07f, 0.08f, 0.14f), new Color(0.5f, 0.6f, 1f), 0f, new Color(0.08f, 0.1f, 0.18f), new Color(0.06f, 0.06f, 0.1f), new Color(0.04f, 0.04f, 0.05f), new Color(0.3f, 0.3f, 0.5f), 0.18f),
            new Key(5f, new Color(0.12f, 0.12f, 0.2f), new Color(0.6f, 0.5f, 0.6f), 0f, new Color(0.12f, 0.12f, 0.2f), new Color(0.1f, 0.09f, 0.12f), new Color(0.05f, 0.05f, 0.06f), new Color(0.4f, 0.35f, 0.5f), 0.3f),
            new Key(6.5f, new Color(0.78f, 0.58f, 0.52f), new Color(1f, 0.62f, 0.42f), 0.6f, new Color(0.55f, 0.45f, 0.52f), new Color(0.5f, 0.38f, 0.34f), new Color(0.2f, 0.17f, 0.15f), new Color(0.7f, 0.5f, 0.5f), 0.9f),
            new Key(9f, new Color(0.72f, 0.78f, 0.86f), new Color(1f, 0.94f, 0.84f), 1.2f, new Color(0.6f, 0.67f, 0.8f), new Color(0.52f, 0.52f, 0.5f), new Color(0.28f, 0.25f, 0.22f), new Color(0.5f, 0.55f, 0.65f), 1.2f),
            new Key(13f, new Color(0.75f, 0.81f, 0.89f), new Color(1f, 0.97f, 0.9f), 1.35f, new Color(0.62f, 0.7f, 0.84f), new Color(0.55f, 0.55f, 0.52f), new Color(0.3f, 0.27f, 0.24f), new Color(0.5f, 0.55f, 0.65f), 1.3f),
            new Key(17.5f, new Color(0.86f, 0.73f, 0.57f), new Color(1f, 0.8f, 0.52f), 1.05f, new Color(0.62f, 0.56f, 0.6f), new Color(0.58f, 0.46f, 0.36f), new Color(0.28f, 0.22f, 0.18f), new Color(0.6f, 0.5f, 0.45f), 1.1f),
            new Key(19.2f, new Color(0.82f, 0.5f, 0.46f), new Color(1f, 0.5f, 0.32f), 0.5f, new Color(0.5f, 0.36f, 0.45f), new Color(0.48f, 0.3f, 0.28f), new Color(0.18f, 0.12f, 0.12f), new Color(0.8f, 0.45f, 0.45f), 0.8f),
            new Key(20.6f, new Color(0.24f, 0.21f, 0.34f), new Color(0.6f, 0.5f, 0.7f), 0f, new Color(0.2f, 0.18f, 0.3f), new Color(0.14f, 0.12f, 0.18f), new Color(0.06f, 0.05f, 0.07f), new Color(0.4f, 0.35f, 0.55f), 0.35f),
            new Key(24f, new Color(0.07f, 0.08f, 0.14f), new Color(0.5f, 0.6f, 1f), 0f, new Color(0.08f, 0.1f, 0.18f), new Color(0.06f, 0.06f, 0.1f), new Color(0.04f, 0.04f, 0.05f), new Color(0.3f, 0.3f, 0.5f), 0.18f),
        };

        public void Init()
        {
            var sunGo = new GameObject("Sol");
            sunGo.transform.SetParent(transform, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.85f;
            Sun.shadowBias = 0.05f;
            Sun.shadowNormalBias = 0.4f;
            RenderSettings.sun = Sun;

            var moonGo = new GameObject("Luna");
            moonGo.transform.SetParent(transform, false);
            Moon = moonGo.AddComponent<Light>();
            Moon.type = LightType.Directional;
            Moon.color = new Color(0.55f, 0.65f, 1f);
            Moon.intensity = 0f;
            Moon.shadows = LightShadows.None;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (RenderSettings.skybox != null) _sky = new Material(RenderSettings.skybox);
            else if (skyShader != null) _sky = new Material(skyShader);
            if (_sky != null)
            {
                if (_sky.HasProperty("_SunSize")) _sky.SetFloat("_SunSize", 0.045f);
                if (_sky.HasProperty("_AtmosphereThickness")) _sky.SetFloat("_AtmosphereThickness", 0.85f);
                if (_sky.HasProperty("_GroundColor")) _sky.SetColor("_GroundColor", new Color(0.62f, 0.62f, 0.68f));
                RenderSettings.skybox = _sky;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;

            BuildCloudSea();
            BuildCloudPuffs();
            BuildStars();
            BuildVistas();
            BuildQuestCloud();
            Apply(7.5f, 0f);
        }

        // ---------------------------------------------------------------- Ciclo del día

        public void Apply(float hour, float dt)
        {
            int i = 0;
            while (i < Keys.Length - 2 && hour >= Keys[i + 1].Hour) i++;
            var a = Keys[i];
            var b = Keys[i + 1];
            float t = Mathf.InverseLerp(a.Hour, b.Hour, hour);
            t = t * t * (3f - 2f * t);

            Color fog = Color.Lerp(a.Fog, b.Fog, t);
            float sunI = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t);
            Color sunC = Color.Lerp(a.SunColor, b.SunColor, t);
            Night = 1f - Mathf.Clamp01(sunI / 0.8f);

            // Sol: sale por el este (+X), pasa algo al sur y se pone por el oeste.
            float theta = (hour - 6f) / 12f * Mathf.PI;
            var p = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), -0.35f * Mathf.Abs(Mathf.Sin(theta)) - 0.1f).normalized;
            if (p.y < 0.03f) p.y = 0.03f;
            Sun.transform.rotation = Quaternion.LookRotation(-p);
            Sun.color = sunC;
            Sun.intensity = sunI;
            Sun.enabled = sunI > 0.01f;
            Sun.shadows = sunI > 0.25f && (Game.Settings == null || Game.Settings.Data.Shadows) ? LightShadows.Soft : LightShadows.None;

            var mp = new Vector3(-p.x, Mathf.Max(0.35f, -p.y), 0.2f).normalized;
            Moon.transform.rotation = Quaternion.LookRotation(-mp);
            Moon.intensity = Mathf.Lerp(0f, 0.22f, Night);
            Moon.enabled = Moon.intensity > 0.01f;

            if (ResonanceTint > 0f)
                fog = Color.Lerp(fog, new Color(0.42f, 0.3f, 0.62f), ResonanceTint * 0.6f);
            RenderSettings.fogColor = fog;
            float density = _region == Region.TribuDelReverso ? 0.0016f : _region == Region.ParadaAntigua ? 0.0021f : 0.0012f;
            density *= Mathf.Lerp(1f, 1.6f, Night);
            density += ResonanceTint * 0.002f;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, density, dt > 0f ? 1f - Mathf.Exp(-dt * 0.8f) : 1f);
            RenderSettings.ambientSkyColor = Color.Lerp(a.AmbSky, b.AmbSky, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.AmbEq, b.AmbEq, t);
            RenderSettings.ambientGroundColor = Color.Lerp(a.AmbGround, b.AmbGround, t);

            if (_sky != null)
            {
                if (_sky.HasProperty("_SkyTint")) _sky.SetColor("_SkyTint", Color.Lerp(a.SkyTint, b.SkyTint, t));
                if (_sky.HasProperty("_Exposure")) _sky.SetFloat("_Exposure", Mathf.Lerp(a.Exposure, b.Exposure, t));
            }
            var cloudTint = Color.Lerp(fog, Color.white, 0.55f - Night * 0.4f);
            if (_cloudSea != null) MaterialLibrary.SetColor(_cloudSea, K.WithA(cloudTint, 0.95f));
            if (_cloudSea2 != null) MaterialLibrary.SetColor(_cloudSea2, K.WithA(cloudTint, 0.55f));
            if (_starMat != null) MaterialLibrary.SetColor(_starMat, new Color(1f, 1f, 1f, Mathf.Clamp01(Night * 1.2f - 0.2f)));
        }

        public void SetRegion(Region r) => _region = r;

        private void Update()
        {
            float off = Time.time * 0.002f;
            if (_cloudSea != null) _cloudSea.mainTextureOffset = new Vector2(off, off * 0.4f);
            if (_cloudSea2 != null) _cloudSea2.mainTextureOffset = new Vector2(-off * 0.6f, off);
            var cam = Camera.main;
            if (cam != null && _stars != null) _stars.transform.position = cam.transform.position;
            if (QuestCloud != null)
            {
                float tt = Time.time * 0.02f;
                QuestCloud.position = new Vector3(790f + Mathf.Sin(tt) * 18f, 128f + Mathf.Sin(tt * 2.3f) * 2f, 440f + Mathf.Cos(tt * 0.7f) * 12f);
            }
            LightBudget.Update(cam != null ? cam.transform.position : Vector3.zero, Night);
        }

        // ---------------------------------------------------------------- Nubes y estrellas

        private void BuildCloudSea()
        {
            var tex = TextureGen.Clouds(256);
            for (int layer = 0; layer < 2; layer++)
            {
                var go = new GameObject(layer == 0 ? "MarDeNubes" : "MarDeNubes2");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(500f, WorldShape.CloudSeaY + layer * 14f, 500f);
                var mesh = new Mesh { name = "Plano" };
                float s = 6000f;
                mesh.vertices = new[] { new Vector3(-s, 0, -s), new Vector3(-s, 0, s), new Vector3(s, 0, s), new Vector3(s, 0, -s) };
                float uv = layer == 0 ? 14f : 9f;
                mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, uv), new Vector2(uv, uv), new Vector2(uv, 0) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                var mat = MaterialLibrary.UnlitTransparent(Color.white, tex);
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                if (layer == 0) _cloudSea = mat; else _cloudSea2 = mat;
            }
        }

        private void BuildCloudPuffs()
        {
            var mb = new ModelBuilder();
            var white = MaterialLibrary.Lit(new Color(0.95f, 0.95f, 0.97f), 0.02f);
            var rng = new OromRandom(321);
            for (int i = 0; i < 34; i++)
            {
                float a = rng.Range(0f, Mathf.PI * 2f);
                float r = rng.Range(620f, 900f);
                var c = new Vector3(500f + Mathf.Cos(a) * r, rng.Range(-45f, 30f), 500f + Mathf.Sin(a) * r);
                int blobs = rng.Range(3, 7);
                for (int k = 0; k < blobs; k++)
                    mb.For(white).Rock(c + new Vector3(rng.Range(-30f, 30f), rng.Range(-6f, 8f), rng.Range(-20f, 20f)), new Vector3(rng.Range(30f, 60f), rng.Range(14f, 24f), rng.Range(26f, 48f)), Quaternion.identity, 3000 + i * 10 + k, 0.15f, false);
            }
            // Nubes dentro del abismo
            for (int i = 0; i < 10; i++)
            {
                float u = i / 9f;
                float ang = Mathf.Lerp(5f, 85f, u) * Mathf.Deg2Rad;
                float d = WorldShape.PlateauExtent + WorldShape.ChasmWidth * 0.5f;
                var c = new Vector3(Mathf.Cos(ang) * d * 0.94f, rng.Range(-48f, -28f), Mathf.Sin(ang) * d * 0.94f);
                mb.For(white).Rock(c, new Vector3(22f, 9f, 22f), Quaternion.identity, 3500 + i, 0.2f, false);
            }
            var list = mb.Build(transform, "Nubes", false, false);
            foreach (var r in list) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void BuildStars()
        {
            var go = new GameObject("Estrellas");
            go.transform.SetParent(transform, false);
            _stars = go.AddComponent<ParticleSystem>();
            _stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _stars.main;
            main.loop = true;
            main.startLifetime = 1e6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.maxParticles = 700;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = _stars.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 700) });
            var shape = _stars.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 2000f;
            shape.radiusThickness = 0f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            _starMat = MaterialLibrary.Particle(true, TextureGen.SoftCircle(32));
            r.sharedMaterial = _starMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            _stars.Play();
        }

        /// <summary>La nube de la misión «El nombre de la nube»: tiene forma de kolotapir dormido y la tripa gris.</summary>
        private void BuildQuestCloud()
        {
            var root = new GameObject("NubeTapir");
            root.transform.SetParent(transform, false);
            var mb = new ModelBuilder();
            var white = MaterialLibrary.Lit(new Color(0.97f, 0.97f, 0.98f), 0.02f);
            var grey = MaterialLibrary.Lit(new Color(0.62f, 0.64f, 0.7f), 0.02f);
            mb.For(white).Rock(new Vector3(0f, 0f, 0f), new Vector3(46f, 20f, 26f), Quaternion.identity, 4000, 0.12f);
            mb.For(white).Rock(new Vector3(24f, 6f, 0f), new Vector3(20f, 17f, 18f), Quaternion.identity, 4001, 0.12f);
            mb.For(white).Rock(new Vector3(36f, 1f, 0f), new Vector3(18f, 8f, 9f), Quaternion.Euler(0f, 0f, -18f), 4002, 0.1f);
            mb.For(white).Rock(new Vector3(20f, 16f, 6f), new Vector3(6f, 9f, 4f), Quaternion.identity, 4003, 0.1f);
            mb.For(white).Rock(new Vector3(20f, 16f, -6f), new Vector3(6f, 9f, 4f), Quaternion.identity, 4004, 0.1f);
            mb.For(grey).Rock(new Vector3(-2f, -8f, 0f), new Vector3(40f, 9f, 22f), Quaternion.identity, 4005, 0.15f);
            mb.For(white).Rock(new Vector3(-22f, -2f, 4f), new Vector3(16f, 12f, 12f), Quaternion.identity, 4006, 0.15f);
            var list = mb.Build(root.transform, "NubeTapir", false, false);
            foreach (var r in list) r.shadowCastingMode = ShadowCastingMode.Off;
            root.transform.position = new Vector3(790f, 128f, 440f);
            QuestCloud = root.transform;
        }

        // ---------------------------------------------------------------- Regiones lejanas

        private void BuildVistas()
        {
            var root = K.Child(transform, "Horizonte");
            foreach (var v in WorldLayout.Vistas)
            {
                var t = K.Child(root, v.Id, new Vector3(v.Pos.X, v.Elevation, v.Pos.Z));
                var mb = new ModelBuilder();
                var rng = new OromRandom(OromRandom.HashString(v.Id));
                // Base de isla flotante (como en la lámina del mapa de Orom)
                var rockCol = v.Id == "vista_gargantas" ? new Color(0.62f, 0.32f, 0.22f) : new Color(0.45f, 0.42f, 0.4f);
                mb.For(MaterialLibrary.Lit(rockCol, 0.02f)).Cylinder(new Vector3(0f, -220f, 0f), 30f, 260f, 220f, 9, Quaternion.identity, false, true);
                mb.For(MaterialLibrary.Lit(new Color(0.35f, 0.48f, 0.28f), 0.02f)).Cylinder(new Vector3(0f, -2f, 0f), 262f, 255f, 4f, 9, Quaternion.identity);
                switch (v.Id)
                {
                    case "vista_ciudad":
                    {
                        var sand = MaterialLibrary.Lit(new Color(0.86f, 0.72f, 0.52f), 0.1f);
                        var gold = MaterialLibrary.Lit(new Color(0.9f, 0.7f, 0.35f), 0.4f, 0.4f);
                        var glow = MaterialLibrary.Emissive(new Color(1f, 0.85f, 0.6f), new Color(1f, 0.7f, 0.35f), 0.8f);
                        for (int i = 0; i < 26; i++)
                        {
                            float a = rng.Range(0f, Mathf.PI * 2f), r = rng.Range(0f, 210f);
                            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                            float h = rng.Range(40f, 140f) * (1f - r / 300f);
                            float w = rng.Range(14f, 30f);
                            mb.For(sand).Cylinder(p, w, w * 0.8f, h, 8, Quaternion.identity);
                            mb.For(i % 3 == 0 ? gold : sand).Sphere(p + Vector3.up * h, new Vector3(w * 0.9f, w * 0.75f, w * 0.9f), 10, 6, Quaternion.identity);
                            if (i % 2 == 0) mb.For(glow).Box(p + new Vector3(0f, h * 0.6f, w * 0.8f), new Vector3(3f, 5f, 1f), Quaternion.identity);
                        }
                        mb.For(sand).Cylinder(Vector3.zero, 22f, 12f, 230f, 8, Quaternion.identity);
                        mb.For(gold).Sphere(Vector3.up * 235f, new Vector3(26f, 22f, 26f), 12, 7, Quaternion.identity);
                        for (int i = 0; i < 5; i++)
                            mb.For(sand).Box(new Vector3(rng.Range(-120f, 120f), rng.Range(30f, 70f), rng.Range(-120f, 120f)), new Vector3(140f, 4f, 8f), Quaternion.Euler(0f, rng.Range(0f, 180f), 0f));
                        break;
                    }
                    case "vista_bosque":
                    {
                        var bark = MaterialLibrary.Lit(new Color(0.3f, 0.24f, 0.26f), 0.02f);
                        var leaf = MaterialLibrary.Lit(new Color(0.32f, 0.36f, 0.5f), 0.02f);
                        var leaf2 = MaterialLibrary.Lit(new Color(0.42f, 0.32f, 0.5f), 0.02f);
                        for (int i = 0; i < 18; i++)
                        {
                            float a = rng.Range(0f, Mathf.PI * 2f), r = rng.Range(0f, 220f);
                            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                            float h = rng.Range(80f, 180f);
                            bool inverted = i % 4 == 0; // árboles del revés: raíces al cielo
                            mb.For(bark).Cylinder(p, 9f, 4f, h, 6, Quaternion.identity);
                            if (inverted)
                            {
                                for (int k = 0; k < 5; k++)
                                    mb.For(bark).Cylinder(p + Vector3.up * h, 3f, 0.5f, 50f, 5, Quaternion.Euler(rng.Range(20f, 50f), k * 72f, 0f));
                                mb.For(leaf2).Rock(p + Vector3.up * 20f, new Vector3(90f, 40f, 90f), Quaternion.identity, 5000 + i, 0.2f, false);
                            }
                            else mb.For(i % 2 == 0 ? leaf : leaf2).Rock(p + Vector3.up * h, new Vector3(110f, 70f, 110f), Quaternion.identity, 5000 + i, 0.2f, false);
                        }
                        break;
                    }
                    case "vista_gargantas":
                    {
                        var red = MaterialLibrary.Lit(new Color(0.72f, 0.36f, 0.24f), 0.02f);
                        var red2 = MaterialLibrary.Lit(new Color(0.6f, 0.3f, 0.2f), 0.02f);
                        for (int i = 0; i < 16; i++)
                        {
                            var p = new Vector3(rng.Range(-200f, 200f), 0f, rng.Range(-200f, 200f));
                            float h = rng.Range(40f, 120f);
                            mb.For(i % 2 == 0 ? red : red2).Cylinder(p, rng.Range(30f, 60f), rng.Range(24f, 50f), h, 7, Quaternion.Euler(0f, rng.Range(0f, 60f), 0f));
                        }
                        break;
                    }
                    case "vista_umbral":
                    {
                        var violet = MaterialLibrary.Lit(new Color(0.48f, 0.4f, 0.62f), 0.1f);
                        var glow = MaterialLibrary.Emissive(new Color(0.8f, 0.65f, 1f), Pal.Resonance, 1.4f);
                        for (int i = 0; i < 9; i++)
                        {
                            var p = new Vector3(rng.Range(-240f, 240f), rng.Range(20f, 160f), rng.Range(-160f, 160f));
                            mb.For(violet).Cylinder(p + Vector3.down * 60f, 6f, rng.Range(30f, 60f), 60f, 7, Quaternion.identity);
                        }
                        // Anillo luminoso
                        for (int i = 0; i < 24; i++)
                        {
                            float a = i * Mathf.PI * 2f / 24f;
                            mb.For(glow).Box(new Vector3(Mathf.Cos(a) * 90f, 180f + Mathf.Sin(a) * 90f, 0f), new Vector3(10f, 26f, 8f), Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg));
                        }
                        break;
                    }
                    case "vista_mar":
                    {
                        var water = MaterialLibrary.Lit(new Color(0.25f, 0.6f, 0.65f), 0.7f);
                        for (int i = 0; i < 7; i++)
                        {
                            var p = new Vector3(rng.Range(-260f, 260f), rng.Range(-20f, 80f), rng.Range(-200f, 200f));
                            mb.For(MaterialLibrary.Lit(new Color(0.5f, 0.48f, 0.45f), 0.02f)).Cylinder(p + Vector3.down * 50f, 8f, 45f, 50f, 7, Quaternion.identity);
                            mb.For(water).Cylinder(p, 40f, 40f, 2f, 9, Quaternion.identity);
                        }
                        break;
                    }
                    default:
                    {
                        var stone = MaterialLibrary.Lit(new Color(0.5f, 0.55f, 0.5f), 0.02f);
                        var water = MaterialLibrary.Emissive(new Color(0.85f, 0.92f, 1f), new Color(0.3f, 0.35f, 0.4f), 0.5f);
                        mb.For(stone).Box(new Vector3(0f, 80f, 0f), new Vector3(420f, 160f, 60f), Quaternion.identity);
                        for (int i = 0; i < 5; i++)
                            mb.For(water).Box(new Vector3(-160f + i * 80f, 70f, 32f), new Vector3(18f, 150f, 4f), Quaternion.identity);
                        break;
                    }
                }
                var list = mb.Build(t, v.Id, false, false);
                foreach (var r in list) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
