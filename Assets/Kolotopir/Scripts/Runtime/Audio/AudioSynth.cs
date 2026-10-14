using System;
using System.Collections.Generic;

namespace Kolotopir
{
    /// <summary>
    /// Sintetizador de sonido procedural (sin archivos de audio). Calcula muestras en un hilo aparte;
    /// <see cref="AudioManager"/> crea los AudioClip en el hilo principal.
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;

        public sealed class ClipData
        {
            public string Name;
            public float[] Samples;
            public bool Loop;
        }

        private sealed class Rng
        {
            private uint _s;
            public Rng(int seed) { _s = (uint)seed * 2654435761u + 1u; }
            public float Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s & 0xFFFFFF) / (float)0x1000000 * 2f - 1f; }
            public float Value() => (Next() + 1f) * 0.5f;
        }

        private static float[] Buf(float seconds) => new float[Math.Max(1, (int)(seconds * Rate))];
        private static float Sin(double phase) => (float)Math.Sin(phase);
        private const double Tau = Math.PI * 2.0;

        private static float Env(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            if (t < attack) return t / attack;
            return (float)Math.Exp(-(t - attack) / Math.Max(0.0001f, decay));
        }

        private static void Normalize(float[] b, float peak = 0.9f)
        {
            float max = 0f;
            for (int i = 0; i < b.Length; i++) max = Math.Max(max, Math.Abs(b[i]));
            if (max < 1e-5f) return;
            float k = peak / max;
            for (int i = 0; i < b.Length; i++) b[i] *= k;
        }

        /// <summary>Suaviza inicio y final para bucles sin chasquidos.</summary>
        private static void LoopFade(float[] b, float seconds = 0.05f)
        {
            int n = Math.Min(b.Length / 2, (int)(seconds * Rate));
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                b[i] *= k;
                b[b.Length - 1 - i] *= k;
            }
        }

        /// <summary>Filtro paso bajo de un polo aplicado en sitio.</summary>
        private static void LowPass(float[] b, float cutoff)
        {
            float rc = 1f / (float)(Tau * cutoff), dt = 1f / Rate, a = dt / (rc + dt);
            float y = 0f;
            for (int i = 0; i < b.Length; i++) { y += a * (b[i] - y); b[i] = y; }
        }

        private static void HighPass(float[] b, float cutoff)
        {
            float rc = 1f / (float)(Tau * cutoff), dt = 1f / Rate, a = rc / (rc + dt);
            float prevX = 0f, y = 0f;
            for (int i = 0; i < b.Length; i++) { float x = b[i]; y = a * (y + x - prevX); prevX = x; b[i] = y; }
        }

        /// <summary>Reverberación muy simple (dos filtros peine) para dar espacio.</summary>
        private static void Reverb(float[] b, float mix = 0.3f, float feedback = 0.45f)
        {
            int d1 = (int)(0.0297f * Rate), d2 = (int)(0.0371f * Rate), d3 = (int)(0.0411f * Rate);
            var c1 = new float[d1]; var c2 = new float[d2]; var c3 = new float[d3];
            int i1 = 0, i2 = 0, i3 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float x = b[i];
                float y1 = c1[i1]; c1[i1] = x + y1 * feedback; i1 = (i1 + 1) % d1;
                float y2 = c2[i2]; c2[i2] = x + y2 * feedback; i2 = (i2 + 1) % d2;
                float y3 = c3[i3]; c3[i3] = x + y3 * feedback; i3 = (i3 + 1) % d3;
                b[i] = x * (1f - mix) + (y1 + y2 + y3) * (mix / 3f);
            }
        }

        // ------------------------------------------------------------------ Generadores

        private static float[] NoiseBurst(int seed, float seconds, float attack, float decay, float lp, float hp, float gain = 1f)
        {
            var r = new Rng(seed);
            var b = Buf(seconds);
            for (int i = 0; i < b.Length; i++) b[i] = r.Next() * Env(i / (float)Rate, attack, decay) * gain;
            if (lp > 0f) { LowPass(b, lp); LowPass(b, lp); }
            if (hp > 0f) HighPass(b, hp);
            Normalize(b, 0.8f);
            return b;
        }

        private static float[] Tone(float seconds, float freq, float attack, float decay, float[] harmonics, float glide = 0f, float vibrato = 0f)
        {
            var b = Buf(seconds);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = freq * (1f + glide * t) * (1f + vibrato * (float)Math.Sin(t * 30f));
                ph += Tau * f / Rate;
                float s = 0f;
                for (int h = 0; h < harmonics.Length; h++) s += Sin(ph * (h + 1)) * harmonics[h];
                b[i] = s * Env(t, attack, decay);
            }
            Normalize(b, 0.8f);
            return b;
        }

        private static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n = Math.Max(n, p.Length);
            var b = new float[n];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) b[i] += p[i];
            Normalize(b, 0.85f);
            return b;
        }

        private static float[] Scale(float[] b, float k) { for (int i = 0; i < b.Length; i++) b[i] *= k; return b; }

        /// <summary>Síntesis de formantes: una sílaba con vocal (voces de humanos y kolotapires).</summary>
        private static float[] Syllable(int seed, float f0, float f1, float f2, bool nasal, float consonant)
        {
            var r = new Rng(seed);
            float dur = 0.16f + r.Value() * 0.06f;
            var b = Buf(dur);
            double ph = 0;
            // Resonadores (filtro biquad paso banda) para dos formantes
            Biquad bp1 = new Biquad(f1, 6f), bp2 = new Biquad(f2, 9f), nas = new Biquad(250f, 3f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = f0 * (1f + 0.06f * (float)Math.Sin(t * 18f) - 0.12f * t);
                ph += Tau * f / Rate;
                float saw = (float)(2.0 * (ph / Tau - Math.Floor(ph / Tau + 0.5)));
                float src = saw * 0.7f + r.Next() * 0.05f;
                float v = bp1.Process(src) * 1.1f + bp2.Process(src) * 0.6f;
                if (nasal) v = v * 0.5f + nas.Process(src) * 1.4f;
                float env = Env(t - consonant, 0.02f, dur * 0.45f);
                float cons = t < consonant ? r.Next() * Env(t, 0.003f, consonant * 0.5f) * 0.6f : 0f;
                b[i] = v * env + cons;
            }
            HighPass(b, 90f);
            Normalize(b, 0.7f);
            return b;
        }

        private sealed class Biquad
        {
            private readonly float _b0, _b2, _a1, _a2;
            private float _x1, _x2, _y1, _y2;
            public Biquad(float freq, float q)
            {
                double w = Tau * freq / Rate, alpha = Math.Sin(w) / (2 * q), a0 = 1 + alpha;
                _b0 = (float)(alpha / a0); _b2 = (float)(-alpha / a0);
                _a1 = (float)(-2 * Math.Cos(w) / a0); _a2 = (float)((1 - alpha) / a0);
            }
            public float Process(float x)
            {
                float y = _b0 * x + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        /// <summary>Pieza musical breve: pentatónica con pads suaves y notas pulsadas tipo kalimba.</summary>
        private static float[] Music(int seed, float seconds, float bpm, float[] chordRoots, string mood)
        {
            var r = new Rng(seed);
            var b = Buf(seconds);
            float beat = 60f / bpm;
            float[] scale = mood == "triste" ? new[] { 0f, 3f, 5f, 7f, 10f, 12f, 15f } : new[] { 0f, 2f, 4f, 7f, 9f, 12f, 14f };
            float baseHz = 146.83f; // Re
            int bars = (int)(seconds / (beat * 4f));
            // Pads
            for (int bar = 0; bar < bars; bar++)
            {
                float root = chordRoots[bar % chordRoots.Length];
                float start = bar * beat * 4f;
                float len = beat * 4f + 0.6f;
                float[] chord = { 0f, mood == "triste" ? 3f : 4f, 7f };
                foreach (var iv in chord)
                {
                    float f = baseHz * (float)Math.Pow(2, (root + iv) / 12.0) * 0.5f;
                    int s0 = (int)(start * Rate), s1 = Math.Min(b.Length, (int)((start + len) * Rate));
                    double ph = r.Value() * Tau;
                    for (int i = s0; i < s1; i++)
                    {
                        float t = (i - s0) / (float)Rate;
                        ph += Tau * f / Rate;
                        float env = Math.Min(1f, t / 1.2f) * Math.Min(1f, (len - t) / 0.8f);
                        b[i] += (Sin(ph) * 0.6f + Sin(ph * 2.0 + 0.3) * 0.15f) * env * 0.09f;
                    }
                }
            }
            // Melodía pulsada
            int steps = (int)(seconds / (beat * 0.5f));
            int note = 2;
            for (int s = 0; s < steps; s++)
            {
                if (r.Value() < (mood == "calma" ? 0.45f : 0.6f)) continue;
                note = Math.Max(0, Math.Min(scale.Length - 1, note + (int)Math.Round(r.Next() * 2.2f)));
                float f = baseHz * (float)Math.Pow(2, scale[note] / 12.0) * 2f;
                float start = s * beat * 0.5f;
                int s0 = (int)(start * Rate), s1 = Math.Min(b.Length, s0 + (int)(1.6f * Rate));
                double ph = 0;
                for (int i = s0; i < s1; i++)
                {
                    float t = (i - s0) / (float)Rate;
                    ph += Tau * f / Rate;
                    float env = Env(t, 0.005f, 0.45f);
                    b[i] += (Sin(ph) + Sin(ph * 2.01) * 0.3f + Sin(ph * 3.98) * 0.08f) * env * 0.16f;
                }
            }
            Reverb(b, 0.35f, 0.55f);
            LowPass(b, 5000f);
            Normalize(b, 0.7f);
            LoopFade(b, 0.4f);
            return b;
        }

        // ------------------------------------------------------------------ Catálogo

        public static List<ClipData> GenerateAll()
        {
            var list = new List<ClipData>();
            void Add(string n, float[] s, bool loop = false) { list.Add(new ClipData { Name = n, Samples = s, Loop = loop }); }

            // Pasos (tres variantes por superficie)
            for (int v = 0; v < 3; v++)
            {
                Add("paso_hierba_" + v, NoiseBurst(10 + v, 0.18f, 0.01f, 0.05f, 2500f, 300f, 0.7f));
                Add("paso_tierra_" + v, NoiseBurst(20 + v, 0.16f, 0.004f, 0.04f, 1400f, 120f));
                Add("paso_madera_" + v, Mix(NoiseBurst(30 + v, 0.18f, 0.002f, 0.03f, 900f, 80f), Scale(Tone(0.18f, 180f + v * 25f, 0.002f, 0.05f, new[] { 1f, 0.4f }), 0.6f)));
                Add("paso_piedra_" + v, NoiseBurst(40 + v, 0.12f, 0.002f, 0.025f, 4000f, 600f));
            }
            Add("chapoteo", NoiseBurst(50, 0.45f, 0.01f, 0.15f, 3000f, 400f));
            Add("salto", NoiseBurst(51, 0.15f, 0.01f, 0.05f, 1200f, 200f, 0.5f));
            Add("caida", Mix(NoiseBurst(52, 0.3f, 0.003f, 0.08f, 600f, 60f), Scale(Tone(0.25f, 70f, 0.003f, 0.08f, new[] { 1f }), 0.8f)));

            // Combate
            Add("tajo", NoiseBurst(60, 0.28f, 0.06f, 0.06f, 3500f, 900f, 0.8f));
            Add("tajo_fuerte", NoiseBurst(61, 0.42f, 0.12f, 0.09f, 2600f, 500f));
            Add("impacto", Mix(NoiseBurst(62, 0.2f, 0.002f, 0.04f, 1500f, 100f), Tone(0.22f, 95f, 0.002f, 0.06f, new[] { 1f, 0.3f })));
            Add("impacto_eco", Mix(Tone(0.5f, 880f, 0.002f, 0.18f, new[] { 1f, 0.5f, 0.3f, 0.2f }), Scale(NoiseBurst(63, 0.2f, 0.002f, 0.05f, 6000f, 1500f), 0.5f)));
            Add("bloqueo", Mix(Tone(0.5f, 620f, 0.001f, 0.12f, new[] { 1f, 0.2f, 0.6f, 0.1f, 0.4f }), Scale(NoiseBurst(64, 0.1f, 0.001f, 0.02f, 7000f, 2000f), 0.6f)));
            Add("parada", Mix(Tone(1.0f, 990f, 0.001f, 0.35f, new[] { 1f, 0f, 0.5f, 0f, 0.3f }), Tone(1.0f, 1320f, 0.001f, 0.3f, new[] { 0.6f })));
            Add("herida", Mix(NoiseBurst(65, 0.3f, 0.01f, 0.08f, 900f, 150f), Scale(Tone(0.3f, 160f, 0.01f, 0.1f, new[] { 1f, 0.5f }, -0.6f), 0.5f)));
            Add("esquiva", NoiseBurst(66, 0.25f, 0.03f, 0.06f, 2000f, 400f, 0.6f));
            Add("pulso", Mix(Tone(1.2f, 220f, 0.01f, 0.5f, new[] { 1f, 0.6f, 0.3f }, 1.5f), Scale(NoiseBurst(67, 1f, 0.01f, 0.4f, 3000f, 500f), 0.3f)));

            // Objetos e interfaz
            Add("curar", Mix(Tone(0.6f, 520f, 0.05f, 0.2f, new[] { 1f, 0.3f }), Tone(0.6f, 780f, 0.1f, 0.2f, new[] { 0.6f })));
            Add("comer", NoiseBurst(70, 0.35f, 0.01f, 0.06f, 2000f, 300f, 0.6f));
            Add("recoger", Mix(Tone(0.25f, 880f, 0.005f, 0.08f, new[] { 1f, 0.3f }), Tone(0.25f, 1320f, 0.04f, 0.08f, new[] { 0.6f })));
            Add("recolectar", NoiseBurst(71, 0.3f, 0.01f, 0.08f, 3000f, 500f, 0.7f));
            Add("cofre", Mix(NoiseBurst(72, 0.5f, 0.02f, 0.15f, 700f, 80f), Tone(0.6f, 440f, 0.2f, 0.2f, new[] { 0.4f, 0.2f })));
            Add("cortar", NoiseBurst(73, 0.35f, 0.005f, 0.07f, 4000f, 800f));
            Add("examinar", Tone(0.35f, 660f, 0.01f, 0.12f, new[] { 1f, 0.4f, 0.2f }));
            Add("ui_clic", Tone(0.06f, 1200f, 0.001f, 0.015f, new[] { 1f, 0.3f }));
            Add("ui_abrir", Tone(0.25f, 500f, 0.01f, 0.08f, new[] { 1f, 0.3f }, 0.8f));
            Add("ui_cerrar", Tone(0.25f, 700f, 0.01f, 0.08f, new[] { 1f, 0.3f }, -0.5f));
            Add("aviso", Mix(Tone(0.4f, 784f, 0.005f, 0.15f, new[] { 1f, 0.2f }), Tone(0.4f, 1046f, 0.06f, 0.15f, new[] { 0.7f })));
            Add("nivel", Mix(Tone(1.4f, 523f, 0.01f, 0.5f, new[] { 1f, 0.3f }), Tone(1.4f, 659f, 0.12f, 0.5f, new[] { 1f, 0.3f }), Tone(1.4f, 784f, 0.24f, 0.6f, new[] { 1f, 0.3f }), Tone(1.4f, 1046f, 0.36f, 0.6f, new[] { 0.8f })));
            Add("mision", Mix(Tone(1.2f, 392f, 0.02f, 0.5f, new[] { 1f, 0.4f }), Tone(1.2f, 494f, 0.02f, 0.5f, new[] { 0.8f }), Tone(1.2f, 587f, 0.02f, 0.5f, new[] { 0.7f })));
            Add("descubrir", Mix(Tone(1.6f, 1046f, 0.005f, 0.6f, new[] { 1f, 0f, 0.4f }), Tone(1.6f, 1568f, 0.15f, 0.6f, new[] { 0.6f })));

            // Resonancia y Cápsulas (firma sonora diferenciada)
            var chime = Mix(Tone(2.5f, 1318f, 0.3f, 0.9f, new[] { 1f, 0f, 0.3f }), Tone(2.5f, 1760f, 0.8f, 0.7f, new[] { 0.6f }), Tone(2.5f, 1975f, 1.3f, 0.5f, new[] { 0.5f }));
            Reverb(chime, 0.5f, 0.6f); LoopFade(chime, 0.3f);
            Add("res_campanilla", chime, true);
            var drone = Buf(4f);
            { double p1 = 0, p2 = 0, p3 = 0; for (int i = 0; i < drone.Length; i++) { float t = i / (float)Rate; p1 += Tau * 110 / Rate; p2 += Tau * 164.8 * (1 + 0.003 * Math.Sin(t * 2)) / Rate; p3 += Tau * 220.5 / Rate; drone[i] = (Sin(p1) * 0.5f + Sin(p2) * 0.35f + Sin(p3) * 0.2f) * 0.5f; } }
            LowPass(drone, 900f); LoopFade(drone, 0.3f); Normalize(drone, 0.6f);
            Add("res_zumbido", drone, true);
            var fragment = Mix(Tone(2f, 440f, 0.05f, 0.8f, new[] { 1f, 0.5f, 0.25f }, 0.15f), Tone(2f, 554f, 0.2f, 0.8f, new[] { 0.8f }), Tone(2f, 659f, 0.35f, 0.9f, new[] { 0.7f }), Scale(NoiseBurst(80, 2f, 0.4f, 0.6f, 5000f, 2000f), 0.15f));
            Reverb(fragment, 0.55f, 0.6f);
            Add("res_fragmento", fragment);
            var hum = Buf(3f);
            { double p = 0; for (int i = 0; i < hum.Length; i++) { float t = i / (float)Rate; p += Tau * (82.4 + Math.Sin(t * Tau / 3.0) * 2) / Rate; hum[i] = Sin(p) * 0.6f + Sin(p * 2) * 0.25f + Sin(p * 3.01) * 0.1f; } }
            LowPass(hum, 600f); LoopFade(hum, 0.2f); Normalize(hum, 0.5f);
            Add("capsula_zumbido", hum, true);
            var anomaly = Buf(4f);
            { var r = new Rng(81); double p = 0; for (int i = 0; i < anomaly.Length; i++) { float t = i / (float)Rate; p += Tau * (98 + 14 * Math.Sin(t * 5.3) + 6 * Math.Sin(t * 13.1)) / Rate; anomaly[i] = Sin(p) * 0.5f + Sin(p * 1.5) * 0.25f + r.Next() * 0.04f; } }
            LowPass(anomaly, 1200f); LoopFade(anomaly, 0.3f); Normalize(anomaly, 0.5f);
            Add("capsula_anomala", anomaly, true);

            // Criaturas
            var whisper = NoiseBurst(90, 3f, 0.5f, 3f, 3500f, 1200f, 1f);
            for (int i = 0; i < whisper.Length; i++) whisper[i] *= 0.5f + 0.5f * (float)Math.Sin(i / (float)Rate * 3.1f) * (float)Math.Sin(i / (float)Rate * 1.3f);
            LoopFade(whisper, 0.3f); Normalize(whisper, 0.5f);
            Add("eco_susurro", whisper, true);
            Add("eco_aviso", Mix(Tone(0.8f, 300f, 0.5f, 0.2f, new[] { 1f, 0.5f, 0.3f }, 1.4f), Scale(NoiseBurst(91, 0.8f, 0.6f, 0.2f, 5000f, 1500f), 0.4f)));
            Add("eco_ataque", NoiseBurst(92, 0.4f, 0.02f, 0.12f, 5000f, 800f));
            var shatter = Mix(Tone(1.2f, 1760f, 0.002f, 0.4f, new[] { 1f, 0.6f, 0.4f, 0.3f }, -0.3f), Scale(NoiseBurst(93, 0.6f, 0.002f, 0.15f, 8000f, 2500f), 0.6f));
            Reverb(shatter, 0.4f, 0.5f);
            Add("eco_muerte", shatter);
            Add("jabali_resoplido", Mix(NoiseBurst(94, 0.6f, 0.02f, 0.15f, 800f, 120f), Scale(Tone(0.6f, 90f, 0.02f, 0.2f, new[] { 1f, 0.6f }), 0.5f)));
            Add("jabali_carga", Mix(NoiseBurst(95, 1.2f, 0.1f, 0.6f, 400f, 40f), Tone(1.2f, 60f, 0.1f, 0.5f, new[] { 1f, 0.5f })));
            Add("jabali_herido", Mix(Tone(0.5f, 400f, 0.01f, 0.15f, new[] { 1f, 0.6f, 0.4f }, -0.5f), Scale(NoiseBurst(96, 0.4f, 0.01f, 0.1f, 2000f, 300f), 0.5f)));
            Add("guardian_rugido", Mix(Tone(2f, 55f, 0.3f, 0.8f, new[] { 1f, 0.7f, 0.5f, 0.3f }, -0.1f, 0.02f), Scale(NoiseBurst(97, 2f, 0.3f, 0.8f, 600f, 40f), 0.6f)));
            Add("guardian_golpe", Mix(NoiseBurst(98, 1.2f, 0.002f, 0.3f, 300f, 20f), Tone(1.2f, 45f, 0.002f, 0.4f, new[] { 1f, 0.5f })));
            Add("guardian_barrido", NoiseBurst(99, 0.8f, 0.2f, 0.2f, 1800f, 200f));
            Add("muneco", Mix(NoiseBurst(100, 0.3f, 0.002f, 0.06f, 1200f, 150f), Scale(Tone(0.3f, 140f, 0.002f, 0.08f, new[] { 1f, 0.3f }), 0.6f)));

            // Ambientes en bucle
            var wind = NoiseBurst(110, 8f, 2f, 99f, 450f, 40f);
            for (int i = 0; i < wind.Length; i++) wind[i] *= 0.6f + 0.4f * (float)Math.Sin(i / (float)Rate * 0.7f);
            LoopFade(wind, 1f); Normalize(wind, 0.6f);
            Add("viento", wind, true);
            var meadow = NoiseBurst(111, 8f, 2f, 99f, 2500f, 600f, 0.5f);
            for (int i = 0; i < meadow.Length; i++) meadow[i] *= 0.5f + 0.5f * (float)Math.Sin(i / (float)Rate * 1.1f + 1f);
            LoopFade(meadow, 1f); Normalize(meadow, 0.4f);
            Add("pradera", meadow, true);
            var crickets = Buf(4f);
            { double p = 0; var r = new Rng(112); for (int i = 0; i < crickets.Length; i++) { float t = i / (float)Rate; p += Tau * 4300 / Rate; float chirp = (float)Math.Max(0, Math.Sin(t * Tau * 2.3)) * ((t * 23 % 1) < 0.5f ? 1f : 0f); crickets[i] = Sin(p) * chirp * 0.3f + r.Next() * 0.01f; } }
            LoopFade(crickets, 0.2f); Normalize(crickets, 0.35f);
            Add("grillos", crickets, true);
            var fire = Buf(4f);
            { var r = new Rng(113); for (int i = 0; i < fire.Length; i++) fire[i] = r.Next() * 0.15f + (r.Value() > 0.9993f ? r.Next() * 2f : 0f); }
            LowPass(fire, 1800f); LoopFade(fire, 0.2f); Normalize(fire, 0.5f);
            Add("fuego", fire, true);
            var water = NoiseBurst(114, 5f, 1f, 99f, 1500f, 300f);
            for (int i = 0; i < water.Length; i++) water[i] *= 0.7f + 0.3f * (float)Math.Sin(i / (float)Rate * 4.3f);
            LoopFade(water, 0.6f); Normalize(water, 0.5f);
            Add("agua", water, true);
            var chimes = Buf(10f);
            { var r = new Rng(115); float[] notes = { 1046f, 1174f, 1318f, 1568f, 1760f }; for (int n = 0; n < 9; n++) { float start = r.Value() * 9f; float f = notes[(int)(r.Value() * 5) % 5]; var tone = Tone(1.4f, f, 0.003f, 0.5f, new[] { 1f, 0f, 0.4f, 0f, 0.2f }); int s0 = (int)(start * Rate); for (int i = 0; i < tone.Length && s0 + i < chimes.Length; i++) chimes[s0 + i] += tone[i] * 0.25f; } }
            Reverb(chimes, 0.4f, 0.5f); LoopFade(chimes, 0.5f); Normalize(chimes, 0.35f);
            Add("campanillas", chimes, true);
            var ruins = Buf(6f);
            { double p1 = 0, p2 = 0; for (int i = 0; i < ruins.Length; i++) { float t = i / (float)Rate; p1 += Tau * 65.4 / Rate; p2 += Tau * (98 + Math.Sin(t * 0.5) * 1.5) / Rate; ruins[i] = Sin(p1) * 0.4f + Sin(p2) * 0.3f; } }
            LowPass(ruins, 500f); LoopFade(ruins, 0.5f); Normalize(ruins, 0.45f);
            Add("ruinas", ruins, true);
            for (int v = 0; v < 4; v++)
            {
                var r = new Rng(120 + v);
                var bird = Buf(0.6f);
                double p = 0;
                float f0 = 2400f + r.Value() * 1600f;
                for (int i = 0; i < bird.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = f0 * (1f + 0.25f * (float)Math.Sin(t * (30 + v * 7)));
                    p += Tau * f / Rate;
                    float env = (float)Math.Max(0, Math.Sin(t * Math.PI / 0.6f)) * ((t * (6 + v) % 1) < 0.6f ? 1f : 0.1f);
                    bird[i] = Sin(p) * env;
                }
                Normalize(bird, 0.4f);
                Add("pajaro_" + v, bird);
            }

            // Voces: sílabas por tipo de hablante (doc: los kolotapires tienen sonidos vocales identificables)
            float[,] vowels = { { 800f, 1200f }, { 500f, 1800f }, { 320f, 2300f }, { 520f, 900f }, { 340f, 800f } };
            for (int v = 0; v < 6; v++)
                Add("voz_humano_" + v, Syllable(200 + v, 200f, vowels[v % 5, 0], vowels[v % 5, 1], false, 0.02f + (v % 3) * 0.015f));
            for (int v = 0; v < 6; v++)
            {
                var syl = Syllable(300 + v, 105f, vowels[(v + 2) % 5, 0] * 0.8f, vowels[(v + 2) % 5, 1] * 0.75f, true, v % 2 == 0 ? 0.04f : 0.01f);
                if (v == 5) syl = Mix(syl, Scale(NoiseBurst(310, 0.2f, 0.005f, 0.06f, 1200f, 200f), 0.6f));
                Add("voz_kolotapir_" + v, syl);
            }
            Add("miga", Mix(Tone(0.25f, 1900f, 0.005f, 0.06f, new[] { 1f, 0.3f }, 0.8f), Scale(NoiseBurst(320, 0.2f, 0.01f, 0.05f, 8000f, 4000f), 0.3f)));

            // Música (selectiva)
            Add("musica_menu", Music(400, 32f, 68f, new[] { 0f, -2f, -5f, -7f }, "calma"), true);
            Add("musica_despedida", Music(401, 24f, 60f, new[] { 0f, -4f, -2f, -7f }, "triste"));
            Add("musica_revelacion", Music(402, 20f, 64f, new[] { 0f, 3f, -2f, 5f }, "triste"));
            Add("musica_final", Music(403, 28f, 72f, new[] { 0f, -5f, -2f, -7f }, "calma"));
            return list;
        }
    }
}
