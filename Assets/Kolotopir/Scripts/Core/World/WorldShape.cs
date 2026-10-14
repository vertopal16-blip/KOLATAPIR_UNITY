using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    public enum Region
    {
        TribuDelReverso,
        Abismo,
        PradosQueRecuerdan,
        ParadaAntigua,
        CaminoDelEste,
        BordeDelMundo
    }

    public struct Lake
    {
        public P2 Center;
        public float Radius;
        public float WaterLevel;
        public float IsletRadius;
        public Lake(float x, float z, float r, float level, float islet = 0f)
        {
            Center = new P2(x, z); Radius = r; WaterLevel = level; IsletRadius = islet;
        }
    }

    public struct FlattenPad
    {
        public P2 Center;
        public float Radius;
        public float Offset;
        public FlattenPad(float x, float z, float r, float offset = 0f) { Center = new P2(x, z); Radius = r; Offset = offset; }
    }

    /// <summary>
    /// Forma del terreno de la porción jugable de Orom (prototipo: Tribu del Reverso + Prados que Recuerdan).
    /// La meseta de la tribu ocupa la esquina suroeste y está separada de los Prados por un abismo que se
    /// cruza mediante un puente colgante. Todo es una función pura: el terreno, la colocación de objetos y
    /// la altura de los NPC usan exactamente la misma fuente.
    /// </summary>
    public static class WorldShape
    {
        // --- Escala (doc: sectores de 250 x 250 m como referencia inicial) ---
        public const float WorldSize = 1000f;
        public const float SectorSize = 250f;
        public const int SectorsPerSide = 4;
        public const float TerrainBaseY = -95f;
        public const float TerrainHeightRange = 165f;
        public const float CloudSeaY = -62f;
        public const float FallRecoveryY = -18f;

        // --- Meseta de la tribu (superelipse con origen en la esquina suroeste) ---
        public const float PlateauExtent = 340f;
        public const float PlateauTop = 44f;
        public const float ChasmWidth = 24f;
        public const float ChasmBottom = -75f;
        public const float MeadowRimTop = 34f;

        public static readonly P2 BridgeWest = new P2(282.5f, 282.5f);
        public static readonly P2 BridgeEast = new P2(314.5f, 314.5f);

        public static readonly Lake[] Lakes =
        {
            new Lake(520f, 515f, 50f, 11f, 6.5f),   // Lago de Boro, con islote central
            new Lake(762f, 702f, 22f, 15.5f),       // Estanque de la Parada
        };

        public static readonly FlattenPad[] Pads =
        {
            new FlattenPad(165f, 165f, 16f),   // plaza y fuego comunal
            new FlattenPad(136.8f, 175.3f, 7f), // casa de Gina
            new FlattenPad(135.5f, 159.8f, 7f), // casa de Suri y Kiko
            new FlattenPad(154.7f, 136.8f, 7f), // casa de Oren
            new FlattenPad(172.8f, 136f, 7f),   // casa de Haro
            new FlattenPad(186.2f, 143.8f, 7f), // casa de Tomé
            new FlattenPad(159.8f, 194.5f, 7f), // casa de Lía
            new FlattenPad(197.8f, 156.2f, 8f), // taller
            new FlattenPad(196.9f, 173.5f, 7f), // almacén
            new FlattenPad(112f, 215f, 20f),    // campos de cultivo
            new FlattenPad(110f, 110f, 12f),    // Piedras de la Migración
            new FlattenPad(232f, 232f, 6f),     // puerta del asentamiento
            new FlattenPad(266f, 266f, 9f),     // puesto de guardia
            new FlattenPad(108f, 280f, 7f),     // taller de Eira y Daren
            new FlattenPad(335f, 345f, 6f),     // mojón del puente
            new FlattenPad(470f, 575f, 12f),    // campamento de Boro
            new FlattenPad(700f, 760f, 24f, 0.5f), // Parada Antigua
            new FlattenPad(668f, 795f, 6f),     // refugio de la Parada
            new FlattenPad(840f, 340f, 9f),     // campamento de Bruma y Copo
        };

        public static readonly P2 CapsuleHollow = new P2(92f, 290f);

        /// <summary>Caminos (polilíneas). Se pintan en tierra y guían la vista sin marcadores en pantalla.</summary>
        public static readonly P2[][] Paths =
        {
            new[] { new P2(165, 165), new P2(200, 200), new P2(232, 232), new P2(266, 266), new P2(283, 283) },
            new[] { new P2(165, 165), new P2(148, 172), new P2(141, 174) },
            new[] { new P2(165, 165), new P2(138, 192), new P2(112, 215), new P2(104, 250), new P2(100, 278) },
            new[] { new P2(165, 165), new P2(138, 138), new P2(110, 110) },
            new[] { new P2(165, 165), new P2(185, 150), new P2(197, 156) },
            new[] { new P2(165, 165), new P2(172, 140) },
            new[] { new P2(165, 165), new P2(196, 172) },
            new[] { new P2(214, 214), new P2(212, 230) },
            new[] { new P2(314, 314), new P2(335, 345), new P2(390, 410), new P2(430, 500), new P2(470, 572) },
            new[] { new P2(390, 410), new P2(520, 420), new P2(640, 430), new P2(740, 420), new P2(800, 380), new P2(840, 342) },
            new[] { new P2(470, 572), new P2(560, 640), new P2(650, 700), new P2(700, 755) },
            new[] { new P2(560, 640), new P2(700, 600), new P2(860, 600), new P2(990, 600) },
            new[] { new P2(640, 430), new P2(700, 600) },
            new[] { new P2(700, 760), new P2(668, 790) },
        };

        private static readonly OromNoise Noise = new OromNoise(1337);
        private static readonly OromNoise Detail = new OromNoise(4242);
        private static readonly OromNoise EdgeNoise = new OromNoise(777);

        // ------------------------------------------------------------------ Altura

        /// <summary>Norma de superelipse (p = 4) de la meseta: 1 en el borde, &lt;1 dentro.</summary>
        public static float PlateauNorm(float x, float z)
        {
            float ax = Math.Max(0f, x) / PlateauExtent, az = Math.Max(0f, z) / PlateauExtent;
            return (float)Math.Pow(ax * ax * ax * ax + az * az * az * az, 0.25);
        }

        /// <summary>Distancia aproximada (m) fuera del borde de la meseta. Negativa dentro de ella.</summary>
        public static float PlateauDistance(float x, float z) => (PlateauNorm(x, z) - 1f) * PlateauExtent;

        public static float EdgeDistance(float x, float z)
        {
            float e = Math.Min(Math.Min(x, z), Math.Min(WorldSize - x, WorldSize - z));
            e += EdgeNoise.Fbm(x * 0.007f, z * 0.007f, 3) * 20f + EdgeNoise.Fbm(x * 0.03f, z * 0.03f, 2) * 4f;
            // Puente natural del este: el camino continúa hacia la Ciudad de las Mil Cápsulas.
            if (x > 880f && Math.Abs(z - 600f) < 22f)
            {
                float t = OromMath.SmoothStep(22f, 12f, Math.Abs(z - 600f));
                e = OromMath.Lerp(e, 200f, t);
            }
            return e;
        }

        private static float PlateauSurface(float x, float z)
        {
            float h = PlateauTop + Detail.Fbm(x * 0.02f, z * 0.02f, 3) * 1.6f;
            // Leve elevación hacia el árbol de los Mayores.
            h += 1.8f * OromMath.Gaussian(P2.Distance(new P2(x, z), new P2(215, 240)), 30f);
            return h;
        }

        private static float MeadowSurface(float x, float z)
        {
            float h = 16f + Noise.Fbm(x / 190f, z / 190f, 4) * 7f + Detail.Fbm(x * 0.05f, z * 0.05f, 2) * 0.8f;
            h += (z - 500f) * 0.004f;
            var p = new P2(x, z);
            h += 22f * OromMath.Gaussian(P2.Distance(p, new P2(780, 420)), 48f);  // Colina de las Nubes
            h += 6f * OromMath.Gaussian(P2.Distance(p, new P2(700, 760)), 55f);   // elevación de la Parada
            h += 9f * OromMath.Gaussian(P2.Distance(p, new P2(600, 830)), 22f);   // Peña del Mirador (base)
            h += 4f * OromMath.Gaussian(P2.Distance(p, new P2(880, 760)), 60f);
            return h;
        }

        /// <summary>Altura del terreno en metros (y mundial).</summary>
        public static float Height(float x, float z)
        {
            float d = PlateauDistance(x, z);
            float h;
            if (d < 0f)
            {
                h = PlateauSurface(x, z);
            }
            else if (d < 3f)
            {
                h = OromMath.Lerp(PlateauSurface(x, z), ChasmBottom, OromMath.SmoothStep(0f, 3f, d));
            }
            else if (d < ChasmWidth - 5f)
            {
                h = ChasmBottom;
            }
            else
            {
                float rim = MeadowRimTop + Detail.Fbm(x * 0.03f, z * 0.03f, 2) * 1.5f;
                float meadow = MeadowSurface(x, z);
                float side = OromMath.Lerp(rim, meadow, OromMath.SmoothStep(ChasmWidth + 4f, ChasmWidth + 150f, d));
                h = d < ChasmWidth ? OromMath.Lerp(ChasmBottom, side, OromMath.SmoothStep(ChasmWidth - 5f, ChasmWidth, d)) : side;
            }

            // Hondonada de la Cápsula sellada.
            float dh = P2.Distance(new P2(x, z), CapsuleHollow);
            if (dh < 16f) h -= 3.2f * (1f - OromMath.SmoothStep(5f, 16f, dh));

            // Plataformas aplanadas (casas, plaza, campamentos).
            for (int i = 0; i < Pads.Length; i++)
            {
                var pad = Pads[i];
                float pd = P2.Distance(new P2(x, z), pad.Center);
                if (pd < pad.Radius + 6f)
                {
                    float target = PadHeight(pad);
                    h = OromMath.Lerp(h, target, 1f - OromMath.SmoothStep(pad.Radius, pad.Radius + 6f, pd));
                }
            }

            // Lagos.
            for (int i = 0; i < Lakes.Length; i++)
            {
                var lake = Lakes[i];
                float ld = P2.Distance(new P2(x, z), lake.Center);
                if (ld < lake.Radius + 22f)
                {
                    float shore = 1f - OromMath.SmoothStep(lake.Radius, lake.Radius + 22f, ld);
                    h = OromMath.Lerp(h, Math.Min(h, lake.WaterLevel + 1.1f), shore);
                    float inside = 1f - OromMath.SmoothStep(lake.Radius * 0.6f, lake.Radius * 1.12f, ld);
                    h = OromMath.Lerp(h, lake.WaterLevel - 3.2f, inside);
                    if (lake.IsletRadius > 0f)
                    {
                        float islet = 1f - OromMath.SmoothStep(lake.IsletRadius * 0.5f, lake.IsletRadius * 1.6f, ld);
                        h = OromMath.Lerp(h, lake.WaterLevel + 1.4f, islet);
                    }
                }
            }

            // Borde del mundo: acantilados que caen al mar de nubes.
            float e = EdgeDistance(x, z);
            if (e < 34f) h = OromMath.Lerp(h, -92f, OromMath.SmoothStep(34f, 16f, e));
            return h;
        }

        private static float PadHeight(FlattenPad pad)
        {
            float d = PlateauDistance(pad.Center.X, pad.Center.Z);
            float baseH = d < 0f ? PlateauTop : MeadowSurface(pad.Center.X, pad.Center.Z);
            return baseH + pad.Offset;
        }

        public static float SlopeDegrees(float x, float z)
        {
            const float e = 1f;
            float dx = Height(x + e, z) - Height(x - e, z);
            float dz = Height(x, z + e) - Height(x, z - e);
            float grad = (float)Math.Sqrt(dx * dx + dz * dz) / (2f * e);
            return (float)(Math.Atan(grad) * OromMath.Rad2Deg);
        }

        // ------------------------------------------------------------------ Consultas

        public static Region RegionAt(float x, float z)
        {
            if (EdgeDistance(x, z) < 20f) return Region.BordeDelMundo;
            float d = PlateauDistance(x, z);
            if (d < 0f) return Region.TribuDelReverso;
            if (d < ChasmWidth) return Region.Abismo;
            if (P2.Distance(new P2(x, z), new P2(700, 760)) < 95f) return Region.ParadaAntigua;
            if (x > 880f && Math.Abs(z - 600f) < 40f) return Region.CaminoDelEste;
            return Region.PradosQueRecuerdan;
        }

        public static string RegionName(Region r)
        {
            switch (r)
            {
                case Region.TribuDelReverso: return "Tribu del Reverso";
                case Region.Abismo: return "El Abismo";
                case Region.PradosQueRecuerdan: return "Prados que Recuerdan";
                case Region.ParadaAntigua: return "La Parada Antigua";
                case Region.CaminoDelEste: return "Camino del Este";
                default: return "Borde de Orom";
            }
        }

        /// <summary>Devuelve el lago en el punto (si lo hay) y si el punto está sobre el agua.</summary>
        public static bool TryGetLake(float x, float z, out Lake lake)
        {
            for (int i = 0; i < Lakes.Length; i++)
            {
                if (P2.Distance(new P2(x, z), Lakes[i].Center) < Lakes[i].Radius * 1.15f)
                {
                    lake = Lakes[i];
                    return true;
                }
            }
            lake = default;
            return false;
        }

        public static bool IsUnderwater(float x, float z)
        {
            return TryGetLake(x, z, out var lake) && Height(x, z) < lake.WaterLevel - 0.05f;
        }

        public static float DistanceToPath(float x, float z, float maxDistance = 30f)
        {
            float best = maxDistance;
            var p = new P2(x, z);
            for (int i = 0; i < Paths.Length; i++)
            {
                var path = Paths[i];
                for (int j = 0; j < path.Length - 1; j++)
                {
                    var a = path[j];
                    var b = path[j + 1];
                    // descarte rápido por caja
                    float minX = Math.Min(a.X, b.X) - best, maxX = Math.Max(a.X, b.X) + best;
                    float minZ = Math.Min(a.Z, b.Z) - best, maxZ = Math.Max(a.Z, b.Z) + best;
                    if (x < minX || x > maxX || z < minZ || z > maxZ) continue;
                    float dd = OromMath.DistanceToSegment(p, a, b);
                    if (dd < best) best = dd;
                }
            }
            return best;
        }

        /// <summary>
        /// Pesos de las cuatro capas del terreno: 0 hierba corta, 1 pradera florida, 2 tierra/camino, 3 roca.
        /// </summary>
        public static void SplatWeights(float x, float z, float[] w)
        {
            float h = Height(x, z);
            float slope = SlopeDegrees(x, z);
            Region region = RegionAt(x, z);
            float n = Noise.Fbm(x * 0.03f, z * 0.03f, 2);

            float rock = OromMath.SmoothStep(30f, 44f, slope);
            if (h < 0f) rock = Math.Max(rock, OromMath.SmoothStep(0f, -8f, h));
            float path = 1f - OromMath.SmoothStep(1.4f, 3.2f, DistanceToPath(x, z, 6f));
            float meadow = region == Region.TribuDelReverso ? OromMath.SmoothStep(0.15f, 0.55f, n) * 0.35f : OromMath.SmoothStep(-0.25f, 0.25f, n);

            float shore = 0f;
            for (int i = 0; i < Lakes.Length; i++)
            {
                float ld = P2.Distance(new P2(x, z), Lakes[i].Center);
                shore = Math.Max(shore, 1f - OromMath.SmoothStep(Lakes[i].Radius * 0.95f, Lakes[i].Radius + 7f, ld));
            }
            float plaza = 1f - OromMath.SmoothStep(9f, 14f, P2.Distance(new P2(x, z), new P2(165, 165)));
            float ruins = 1f - OromMath.SmoothStep(14f, 26f, P2.Distance(new P2(x, z), new P2(700, 760)));

            float dirt = Math.Max(Math.Max(path, shore * 0.8f), Math.Max(plaza * 0.85f, ruins * 0.55f));
            w[3] = rock;
            float rest = 1f - rock;
            w[2] = dirt * rest;
            rest -= w[2];
            w[1] = meadow * rest;
            w[0] = rest - w[1];
        }

        // ------------------------------------------------------------------ Sectores

        public static int SectorIndex(int sx, int sz) => sz * SectorsPerSide + sx;

        public static void SectorOf(float x, float z, out int sx, out int sz)
        {
            sx = OromMath.Clamp((int)Math.Floor(x / SectorSize), 0, SectorsPerSide - 1);
            sz = OromMath.Clamp((int)Math.Floor(z / SectorSize), 0, SectorsPerSide - 1);
        }

        public static bool InWorld(float x, float z) => x >= 0f && z >= 0f && x <= WorldSize && z <= WorldSize;

        /// <summary>¿Es este punto suelo seguro para recordar como posición de recuperación tras una caída?</summary>
        public static bool IsSafeGround(float x, float z)
        {
            if (!InWorld(x, z)) return false;
            if (EdgeDistance(x, z) < 40f) return false;
            float d = PlateauDistance(x, z);
            if (d > -8f && d < ChasmWidth + 8f && !IsOnBridgeCorridor(x, z)) return false;
            if (IsUnderwater(x, z)) return false;
            return Height(x, z) > 0f;
        }

        public static bool IsOnBridgeCorridor(float x, float z)
        {
            return OromMath.DistanceToSegment(new P2(x, z), BridgeWest, BridgeEast) < 1.6f;
        }

        public static List<P2> SamplePolyline(P2[] points, float spacing)
        {
            var list = new List<P2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                float len = P2.Distance(points[i], points[i + 1]);
                int steps = Math.Max(1, (int)(len / spacing));
                for (int s = 0; s < steps; s++) list.Add(P2.Lerp(points[i], points[i + 1], s / (float)steps));
            }
            list.Add(points[points.Length - 1]);
            return list;
        }
    }
}
