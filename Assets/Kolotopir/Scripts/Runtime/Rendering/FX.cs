using UnityEngine;

namespace Kolotopir
{
    /// <summary>Efectos de partículas creados por código: fuego, humo, destellos, motas de Resonancia, cascadas.</summary>
    public static class FX
    {
        private static Texture2D _soft;
        private static Material _add, _alpha;

        private static void Init()
        {
            if (_soft != null) return;
            _soft = TextureGen.SoftCircle(64);
            _add = MaterialLibrary.Particle(true, _soft);
            _alpha = MaterialLibrary.Particle(false, _soft);
        }

        public static Material AdditiveMaterial { get { Init(); return _add; } }
        public static Material AlphaMaterial { get { Init(); return _alpha; } }

        private static ParticleSystem Make(Transform parent, Vector3 localPos, string name, bool additive)
        {
            Init();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = additive ? _add : _alpha;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var main = ps.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return ps;
        }

        private static void Gradient(ParticleSystem ps, Color a, Color b, float fadeIn = 0.15f)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(a.a, fadeIn), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        public static ParticleSystem Fire(Transform parent, Vector3 localPos, float size = 1f)
        {
            var ps = Make(parent, localPos, "Fuego", true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * size, 1.4f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * size, 0.7f * size);
            main.startColor = new Color(1f, 0.6f, 0.2f, 0.9f);
            main.maxParticles = 80;
            var em = ps.emission;
            em.rateOverTime = 28f * size;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f * size;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            Gradient(ps, new Color(1f, 0.75f, 0.3f, 0.95f), new Color(0.9f, 0.2f, 0.05f, 0.9f), 0.1f);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ps.Play();

            var smoke = Make(parent, localPos + Vector3.up * 0.8f * size, "Humo", false);
            var sm = smoke.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.5f * size, 1f * size);
            sm.startColor = new Color(0.45f, 0.42f, 0.4f, 0.35f);
            sm.maxParticles = 40;
            var sem = smoke.emission;
            sem.rateOverTime = 5f;
            var ssh = smoke.shape;
            ssh.shapeType = ParticleSystemShapeType.Circle;
            ssh.radius = 0.2f;
            ssh.rotation = new Vector3(-90f, 0f, 0f);
            Gradient(smoke, new Color(0.5f, 0.48f, 0.46f, 0.35f), new Color(0.7f, 0.7f, 0.7f, 0.2f), 0.2f);
            var ssz = smoke.sizeOverLifetime;
            ssz.enabled = true;
            ssz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            smoke.Play();
            return ps;
        }

        public static ParticleSystem Smoke(Transform parent, Vector3 localPos)
        {
            var smoke = Make(parent, localPos, "Chimenea", false);
            var sm = smoke.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            sm.startColor = new Color(0.6f, 0.58f, 0.56f, 0.3f);
            sm.maxParticles = 40;
            var em = smoke.emission;
            em.rateOverTime = 3f;
            var sh = smoke.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.15f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            Gradient(smoke, new Color(0.6f, 0.58f, 0.56f, 0.3f), new Color(0.8f, 0.8f, 0.8f, 0.1f));
            var sz = smoke.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 3f));
            smoke.Play();
            return smoke;
        }

        /// <summary>Destello que señala objetos importantes (doc: señales claras para objetos interactivos).</summary>
        public static ParticleSystem Sparkle(Transform parent, Vector3 localPos, Color color, float radius = 0.25f, float rate = 3f)
        {
            var ps = Make(parent, localPos, "Destello", true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = color;
            main.maxParticles = 30;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            Gradient(ps, color, color, 0.3f);
            ps.Play();
            return ps;
        }

        /// <summary>Motas violetas de Resonancia: la firma visual de los recuerdos.</summary>
        public static ParticleSystem Motes(Transform parent, Vector3 localPos, Color color, float radius, float rate)
        {
            var ps = Make(parent, localPos, "Motas", true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = color;
            main.gravityModifier = -0.02f;
            main.maxParticles = 120;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.6f;
            Gradient(ps, color, color, 0.25f);
            ps.Play();
            return ps;
        }

        public static ParticleSystem Waterfall(Transform parent, Vector3 localPos, float width, float drop)
        {
            var ps = Make(parent, localPos, "Cascada", false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(drop / 14f, drop / 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startColor = new Color(0.85f, 0.92f, 1f, 0.55f);
            main.gravityModifier = 1.1f;
            main.maxParticles = 600;
            var em = ps.emission;
            em.rateOverTime = 70f * width / 4f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, 0.2f, 0.4f);
            Gradient(ps, new Color(0.9f, 0.95f, 1f, 0.6f), new Color(1f, 1f, 1f, 0.2f), 0.05f);
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            ps.Play();
            return ps;
        }

        /// <summary>Estallido breve (impactos, recogidas, ecos que se deshacen). Se destruye solo.</summary>
        public static void Burst(Vector3 pos, Color color, int count = 18, float speed = 3f, float size = 0.18f, float life = 0.6f)
        {
            Init();
            var go = new GameObject("Estallido");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = _add;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var main = ps.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            Gradient(ps, color, color, 0.02f);
            ps.Play();
            Object.Destroy(go, life + 0.5f);
        }

        /// <summary>Anillo expansivo (Pulso de Resonancia, golpes del Guardián).</summary>
        public static void Ring(Vector3 pos, Color color, float radius, float duration = 0.5f)
        {
            var go = new GameObject("Anillo");
            go.transform.position = pos + Vector3.up * 0.1f;
            var ring = go.AddComponent<RingEffect>();
            ring.Init(color, radius, duration);
        }
    }

    /// <summary>Anillo plano que se expande y se desvanece.</summary>
    public sealed class RingEffect : MonoBehaviour
    {
        private Material _mat;
        private float _radius, _duration, _t;
        private Color _color;

        public void Init(Color color, float radius, float duration)
        {
            _color = color; _radius = radius; _duration = duration;
            var b = new MeshBuilder();
            int seg = 40;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 i0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.85f, i1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.85f;
                Vector3 o0 = i0 / 0.85f, o1 = i1 / 0.85f;
                b.Quad(i0, i1, o1, o0, Vector3.up);
            }
            gameObject.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("Ring");
            var mr = gameObject.AddComponent<MeshRenderer>();
            _mat = MaterialLibrary.Particle(true, null);
            MaterialLibrary.SetColor(_mat, color);
            mr.sharedMaterial = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            transform.localScale = Vector3.one * 0.1f;
        }

        private void Update()
        {
            _t += Time.deltaTime / Mathf.Max(0.01f, _duration);
            float r = Mathf.Lerp(0.2f, _radius, 1f - (1f - _t) * (1f - _t));
            transform.localScale = new Vector3(r, 1f, r);
            MaterialLibrary.SetColor(_mat, K.WithA(_color, (1f - _t) * _color.a));
            if (_t >= 1f)
            {
                Destroy(_mat);
                Destroy(gameObject);
            }
        }
    }
}
