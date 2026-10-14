using System;

namespace Kolotopir.Core
{
    /// <summary>Punto 2D en el plano del mundo (X = este, Z = norte). Independiente del motor.</summary>
    [Serializable]
    public struct P2
    {
        public float X;
        public float Z;

        public P2(float x, float z) { X = x; Z = z; }

        public static P2 operator +(P2 a, P2 b) => new P2(a.X + b.X, a.Z + b.Z);
        public static P2 operator -(P2 a, P2 b) => new P2(a.X - b.X, a.Z - b.Z);
        public static P2 operator *(P2 a, float s) => new P2(a.X * s, a.Z * s);

        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public static float Distance(P2 a, P2 b) => (a - b).Length;
        public static P2 Lerp(P2 a, P2 b, float t) => new P2(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t);
        public override string ToString() => $"({X:0.#}, {Z:0.#})";
    }

    /// <summary>Funciones matemáticas sin dependencias de UnityEngine.</summary>
    public static class OromMath
    {
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float v) => Math.Abs(b - a) < 1e-6f ? 0f : Clamp01((v - a) / (b - a));

        public static float SmoothStep(float e0, float e1, float x)
        {
            float t = InverseLerp(e0, e1, x);
            return t * t * (3f - 2f * t);
        }

        public static float Gaussian(float d, float radius) => (float)Math.Exp(-(d * d) / (2f * radius * radius));

        /// <summary>Distancia de un punto a un segmento.</summary>
        public static float DistanceToSegment(P2 p, P2 a, P2 b)
        {
            float dx = b.X - a.X, dz = b.Z - a.Z;
            float len2 = dx * dx + dz * dz;
            float t = len2 < 1e-6f ? 0f : Clamp01(((p.X - a.X) * dx + (p.Z - a.Z) * dz) / len2);
            float px = a.X + dx * t - p.X, pz = a.Z + dz * t - p.Z;
            return (float)Math.Sqrt(px * px + pz * pz);
        }

        /// <summary>Ángulo en grados (0 = norte, sentido horario) de un vector XZ.</summary>
        public static float HeadingDeg(float dx, float dz) => (float)(Math.Atan2(dx, dz) * Rad2Deg);
    }

    /// <summary>Generador pseudoaleatorio determinista (xorshift) para que el mundo sea idéntico en cada carga.</summary>
    public sealed class OromRandom
    {
        private uint _state;

        public OromRandom(int seed)
        {
            _state = (uint)seed * 747796405u + 2891336453u;
            if (_state == 0) _state = 0x9E3779B9u;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public float Value() => (NextUInt() & 0xFFFFFF) / (float)0x1000000;
        public float Range(float min, float max) => min + (max - min) * Value();
        public int Range(int minInclusive, int maxExclusive) => maxExclusive <= minInclusive ? minInclusive : minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        public bool Chance(float p) => Value() < p;

        public static int Hash(int a, int b, int c = 0)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)a) * 16777619u;
                h = (h ^ (uint)b) * 16777619u;
                h = (h ^ (uint)c) * 16777619u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (int)h;
            }
        }

        public static int HashString(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in s) h = h * 31 + ch;
                return h;
            }
        }
    }

    /// <summary>Ruido de Perlin 2D propio (determinista y testeable fuera de Unity).</summary>
    public sealed class OromNoise
    {
        private readonly int[] _perm = new int[512];

        public OromNoise(int seed)
        {
            var rng = new OromRandom(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                int tmp = p[i]; p[i] = p[j]; p[j] = tmp;
            }
            for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Grad(int hash, float x, float y)
        {
            switch (hash & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        /// <summary>Ruido en el rango aproximado [-1, 1].</summary>
        public float Sample(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float xf = x - xi, yf = y - yi;
            xi &= 255; yi &= 255;
            float u = Fade(xf), v = Fade(yf);
            int aa = _perm[_perm[xi] + yi], ab = _perm[_perm[xi] + yi + 1];
            int ba = _perm[_perm[xi + 1] + yi], bb = _perm[_perm[xi + 1] + yi + 1];
            float x1 = OromMath.LerpUnclamped(Grad(aa, xf, yf), Grad(ba, xf - 1f, yf), u);
            float x2 = OromMath.LerpUnclamped(Grad(ab, xf, yf - 1f), Grad(bb, xf - 1f, yf - 1f), u);
            return OromMath.LerpUnclamped(x1, x2, v) * 0.7071f * 1.4142f;
        }

        public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Sample(x * freq, y * freq) * amp;
                norm += amp;
                amp *= gain;
                freq *= lacunarity;
            }
            return sum / norm;
        }
    }
}
