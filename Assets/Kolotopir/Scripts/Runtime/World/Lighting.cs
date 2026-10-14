using System.Collections.Generic;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Luz puntual gestionada por presupuesto: solo las más cercanas o importantes se encienden
    /// (doc: optimizar pronto; límites de distancia).
    /// </summary>
    public sealed class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();
        public Light Light;
        public float BaseIntensity = 1f;
        public bool NightOnly;
        public int Priority;
        public bool Flicker;
        public float Boost = 1f;
        private float _seed;

        public static LightSource Create(Transform at, Color color, float range, float intensity, bool nightOnly, int priority, bool flicker = false)
        {
            var l = at.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.enabled = false;
            var s = at.gameObject.AddComponent<LightSource>();
            s.Light = l;
            s.BaseIntensity = intensity;
            s.NightOnly = nightOnly;
            s.Priority = priority;
            s.Flicker = flicker;
            return s;
        }

        private void Awake() => _seed = Random.value * 100f;
        private void OnEnable() => All.Add(this);
        private void OnDisable() { All.Remove(this); if (Light != null) Light.enabled = false; }

        public void Apply(bool on, float night)
        {
            if (Light == null) return;
            if (!on) { if (Light.enabled) Light.enabled = false; return; }
            float k = NightOnly ? night : Mathf.Lerp(0.35f, 1f, night);
            float f = Flicker ? 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, _seed) : 1f;
            Light.intensity = BaseIntensity * k * f * Boost;
            if (!Light.enabled) Light.enabled = true;
        }
    }

    public static class LightBudget
    {
        public static int MaxLights = 8;
        public static float MaxDistance = 70f;
        private static readonly List<LightSource> Sorted = new List<LightSource>();

        public static void Update(Vector3 camPos, float night)
        {
            Sorted.Clear();
            foreach (var l in LightSource.All)
            {
                if (l == null || l.Light == null) continue;
                bool wanted = (!l.NightOnly || night > 0.15f) && (l.transform.position - camPos).sqrMagnitude < MaxDistance * MaxDistance;
                if (wanted) Sorted.Add(l);
                else l.Apply(false, night);
            }
            Sorted.Sort((a, b) =>
            {
                float da = (a.transform.position - camPos).sqrMagnitude / (1f + a.Priority);
                float db = (b.transform.position - camPos).sqrMagnitude / (1f + b.Priority);
                return da.CompareTo(db);
            });
            for (int i = 0; i < Sorted.Count; i++) Sorted[i].Apply(i < MaxLights, night);
        }
    }

    /// <summary>Hogueras: los ecos errantes no se acercan a la luz del fuego.</summary>
    public static class FirePoint
    {
        public static readonly List<Vector4> Points = new List<Vector4>();

        public static void Register(Vector3 pos, float radius)
        {
            foreach (var p in Points)
                if ((new Vector3(p.x, p.y, p.z) - pos).sqrMagnitude < 1f) return;
            Points.Add(new Vector4(pos.x, pos.y, pos.z, radius));
        }

        /// <summary>Devuelve la hoguera más cercana dentro de su radio (o falso).</summary>
        public static bool Near(Vector3 pos, float extra, out Vector3 firePos)
        {
            foreach (var p in Points)
            {
                var fp = new Vector3(p.x, p.y, p.z);
                float r = p.w + extra;
                if ((fp - pos).sqrMagnitude < r * r) { firePos = fp; return true; }
            }
            firePos = Vector3.zero;
            return false;
        }
    }
}
