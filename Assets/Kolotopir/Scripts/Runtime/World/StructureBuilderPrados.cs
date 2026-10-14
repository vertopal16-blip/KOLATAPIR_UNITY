using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    public static partial class StructureBuilder
    {
        // ================================================================ Prados que Recuerdan

        private static void Mojon(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var rng = new OromRandom(55);
            for (int i = 0; i < 7; i++)
            {
                float s = 1.1f - i * 0.12f;
                mb.For(i % 2 == 0 ? Pal.Stone : Pal.StoneWarm).Rock(new Vector3(-1.5f + rng.Range(-0.1f, 0.1f), 0.25f + i * 0.33f, -1.5f), new Vector3(s, 0.45f, s * 0.9f), Quaternion.Euler(0f, i * 40f, 0f), 800 + i, 0.15f);
            }
            mb.For(MaterialLibrary.Emissive(new Color(0.7f, 0.6f, 0.5f), new Color(0.4f, 0.3f, 0.15f), 0.6f)).Box(new Vector3(-1.5f, 0.9f, -1.0f), new Vector3(0.18f, 0.18f, 0.02f), Quaternion.Euler(0f, 0f, 45f));
            Colliders.Box(cols, new Vector3(-1.5f, 1.1f, -1.5f), new Vector3(1.1f, 2.2f, 1.1f));
            // Refugio de troncos y lona
            var wood = MaterialLibrary.Lit(Pal.Wood, 0.1f);
            mb.For(wood).Box(new Vector3(1.6f, 1.0f, -1.6f), new Vector3(0.14f, 2f, 0.14f), Quaternion.identity);
            mb.For(wood).Box(new Vector3(3.6f, 1.0f, -1.6f), new Vector3(0.14f, 2f, 0.14f), Quaternion.identity);
            mb.For(MaterialLibrary.Lit(new Color(0.7f, 0.6f, 0.45f), 0.05f)).Box(new Vector3(2.6f, 1.45f, -0.5f), new Vector3(2.4f, 0.06f, 2.6f), Quaternion.Euler(-35f, 0f, 0f));
            // Hoguera
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                mb.For(Pal.StoneDark).Rock(new Vector3(Mathf.Cos(a) * 0.8f, 0.1f, Mathf.Sin(a) * 0.8f), new Vector3(0.4f, 0.28f, 0.35f), Quaternion.identity, 820 + i, 0.25f, false);
            }
            mb.Build(t, "Mojon");
            FX.Fire(t, new Vector3(0f, 0.1f, 0f), 0.8f);
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 1f, 0f)), Pal.Fire, 11f, 1.8f, false, 3, true);
            FirePoint.Register(t.position, 7f);
            AudioManager.AttachLoop(t.gameObject, "fuego", 0.5f, 14f);
        }

        private static void BoroCamp(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            // Tienda remendada
            var tent = new Vector3(-4f, 0f, 2.5f);
            mb.For(MaterialLibrary.Lit(new Color(0.78f, 0.66f, 0.48f), 0.05f)).Cone(tent, 2.4f, 3.2f, 8, Quaternion.identity, false);
            mb.For(MaterialLibrary.Lit(new Color(0.6f, 0.36f, 0.3f), 0.05f)).Box(tent + new Vector3(1.0f, 1.3f, 1.1f), new Vector3(0.7f, 0.6f, 0.05f), Quaternion.Euler(-30f, 40f, 0f));
            mb.For(MaterialLibrary.Lit(new Color(0.42f, 0.5f, 0.6f), 0.05f)).Box(tent + new Vector3(-0.9f, 1.5f, 0.9f), new Vector3(0.5f, 0.5f, 0.05f), Quaternion.Euler(-30f, -40f, 0f));
            Colliders.Capsule(cols, tent + Vector3.up * 1.2f, 1.6f, 2.6f);
            // Carro con la colección de objetos inútiles
            var cart = new Vector3(3.2f, 0f, 1.8f);
            var wood = MaterialLibrary.Lit(Pal.Wood, 0.1f);
            mb.For(wood).Box(cart + new Vector3(0f, 0.9f, 0f), new Vector3(2.8f, 0.15f, 1.5f), Quaternion.identity);
            mb.For(wood).Box(cart + new Vector3(0f, 1.2f, 0.72f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.identity);
            mb.For(wood).Box(cart + new Vector3(0f, 1.2f, -0.72f), new Vector3(2.8f, 0.5f, 0.08f), Quaternion.identity);
            foreach (float x in new[] { -0.9f, 0.9f })
                foreach (float z in new[] { -0.85f, 0.85f })
                    mb.For(MaterialLibrary.Lit(Pal.WoodDark, 0.1f)).Cylinder(cart + new Vector3(x, 0.55f, z), 0.55f, 0.55f, 0.1f, 10, Quaternion.Euler(90f, 0f, 0f));
            var rng = new OromRandom(808);
            Color[] junk = { new Color(0.7f, 0.7f, 0.68f), new Color(0.6f, 0.45f, 0.25f), new Color(0.75f, 0.6f, 0.3f), new Color(0.4f, 0.5f, 0.6f), new Color(0.65f, 0.3f, 0.25f) };
            for (int i = 0; i < 22; i++)
            {
                var p = cart + new Vector3(rng.Range(-1.2f, 1.2f), 1.05f + rng.Range(0f, 0.4f), rng.Range(-0.55f, 0.55f));
                var c = junk[i % junk.Length];
                switch (i % 4)
                {
                    case 0: mb.For(MaterialLibrary.Lit(c, 0.5f, 0.6f)).Cylinder(p, 0.12f, 0.08f, 0.22f, 7, Quaternion.Euler(rng.Range(0f, 90f), 0f, 0f)); break;
                    case 1: mb.For(MaterialLibrary.Lit(c, 0.3f)).Box(p, new Vector3(0.25f, 0.18f, 0.2f), Quaternion.Euler(0f, rng.Range(0f, 90f), 0f)); break;
                    case 2: mb.For(MaterialLibrary.Lit(c, 0.6f, 0.7f)).Sphere(p, Vector3.one * 0.12f, 6, 4, Quaternion.identity); break;
                    default: mb.For(MaterialLibrary.Lit(c, 0.6f, 0.7f)).Lathe(p, new[] { new Vector2(0.12f, 0f), new Vector2(0.14f, 0.15f), new Vector2(0.06f, 0.25f) }, 6, Quaternion.identity); break;
                }
            }
            Colliders.Box(cols, cart + Vector3.up * 0.8f, new Vector3(2.9f, 1.6f, 1.6f));
            // Banco de trabajo de Boro
            var bench = new Vector3(-0.5f, 0f, 4.2f);
            mb.For(MaterialLibrary.Lit(Pal.WoodLight, 0.1f)).Box(bench + Vector3.up * 0.8f, new Vector3(1.8f, 0.1f, 0.8f), Quaternion.identity);
            for (int i = 0; i < 4; i++) mb.For(wood).Box(bench + new Vector3(i % 2 == 0 ? -0.8f : 0.8f, 0.4f, i < 2 ? -0.32f : 0.32f), new Vector3(0.1f, 0.8f, 0.1f), Quaternion.identity);
            Colliders.Box(cols, bench + Vector3.up * 0.4f, new Vector3(1.8f, 0.8f, 0.8f));
            // Hoguera con tetera
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                mb.For(Pal.StoneDark).Rock(new Vector3(Mathf.Cos(a) * 0.8f, 0.1f, Mathf.Sin(a) * 0.8f), new Vector3(0.4f, 0.28f, 0.35f), Quaternion.identity, 840 + i, 0.25f, false);
            }
            mb.For(MaterialLibrary.Lit(new Color(0.3f, 0.32f, 0.3f), 0.5f, 0.6f)).Lathe(new Vector3(0.5f, 0.15f, 0.2f), new[] { new Vector2(0.14f, 0f), new Vector2(0.18f, 0.12f), new Vector2(0.08f, 0.24f) }, 8, Quaternion.identity);
            mb.For(Pal.Wood).Cylinder(new Vector3(-1.8f, 0.25f, -0.8f), 0.25f, 0.25f, 2f, 8, Quaternion.Euler(0f, 30f, 90f));
            // Cuerda con ropa tendida
            mb.For(Pal.Rope).Cylinder(new Vector3(-6f, 1.9f, -1f), 0.015f, 0.015f, 5f, 3, Quaternion.Euler(0f, 0f, -90f), false, false);
            mb.For(MaterialLibrary.Lit(new Color(0.75f, 0.45f, 0.3f))).Box(new Vector3(-4.8f, 1.6f, -1f), new Vector3(0.6f, 0.6f, 0.03f), Quaternion.identity);
            mb.For(MaterialLibrary.Lit(new Color(0.5f, 0.6f, 0.4f))).Box(new Vector3(-3.6f, 1.55f, -1f), new Vector3(0.5f, 0.7f, 0.03f), Quaternion.identity);
            mb.Build(t, "CampamentoBoro");
            FX.Fire(t, new Vector3(0f, 0.1f, 0f), 0.8f);
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 1f, 0f)), Pal.Fire, 12f, 1.8f, false, 3, true);
            FirePoint.Register(t.position, 8f);
            AudioManager.AttachLoop(t.gameObject, "fuego", 0.5f, 14f);
            var station = new GameObject("BancoBoro");
            station.transform.SetParent(t, false);
            station.transform.localPosition = bench + Vector3.up * 0.95f;
            station.AddComponent<CraftingStation>().Owner = "Boro";
            // La cuenta de memoria brilla en el carro (memoria privada)
            var bead = K.Child(t, "Cuenta", cart + new Vector3(0.6f, 1.4f, 0.1f));
            var bmb = new ModelBuilder();
            bmb.For(MaterialLibrary.Emissive(new Color(0.8f, 0.65f, 0.95f), Pal.Resonance, 1.5f)).Sphere(Vector3.zero, Vector3.one * 0.08f, 8, 5, Quaternion.identity);
            bmb.Build(bead, "Cuenta", false);
            bead.gameObject.AddComponent<HideWhen>().Condition = s => s.HasItem("cuenta_boro") || s.Heard("res_boro_cuenta");
        }

        /// <summary>Arco de piedra: trepar por la enredadera y cruzar por arriba manteniendo el equilibrio.</summary>
        private static void StoneArch(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.Lit(new Color(0.62f, 0.56f, 0.48f), 0.05f);
            float h = WorldLayout.StoneArchHeight;
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                float baseY = WorldShape.Height(t.position.x + t.right.x * x, t.position.z + t.right.z * x) - t.position.y;
                float ph = h - baseY;
                for (int k = 0; k < 4; k++)
                    mb.For(k % 2 == 0 ? stone : MaterialLibrary.Lit(new Color(0.56f, 0.5f, 0.44f), 0.05f))
                        .Rock(new Vector3(x, baseY + ph * (k + 0.5f) / 4f, 0f), new Vector3(1.9f, ph / 4f + 0.4f, 1.8f), Quaternion.Euler(0f, k * 23f, 0f), 1200 + k + (int)x, 0.08f);
                Tag(Colliders.Box(cols, new Vector3(x, baseY + ph * 0.5f - 0.3f, 0f), new Vector3(1.6f, ph - 0.6f, 1.6f), Quaternion.identity, "Pilar"), SurfaceKind.Piedra);
                // Superficie plana sobre el pilar
                Tag(Colliders.Box(cols, new Vector3(x, h - 0.15f, 0f), new Vector3(1.6f, 0.3f, 1.6f), Quaternion.identity, "Cima"), SurfaceKind.Piedra);
            }
            // Arco curvo bajo la pasarela
            int segs = 10;
            for (int i = 0; i < segs; i++)
            {
                float u0 = i / (float)segs, u1 = (i + 1) / (float)segs;
                float x0 = Mathf.Lerp(-4.5f, 4.5f, u0), x1 = Mathf.Lerp(-4.5f, 4.5f, u1);
                float y0 = h - 2.2f + Mathf.Sin(u0 * Mathf.PI) * 1.4f, y1 = h - 2.2f + Mathf.Sin(u1 * Mathf.PI) * 1.4f;
                var mid = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
                var rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg);
                mb.For(stone).Box(mid, new Vector3(Mathf.Abs(x1 - x0) + 0.2f, 1.2f, 1.1f), rot);
            }
            // Pasarela estrecha (0,7 m): equilibrio
            mb.For(MaterialLibrary.Lit(new Color(0.68f, 0.62f, 0.52f), 0.05f)).Box(new Vector3(0f, h - 0.25f, 0f), new Vector3(8.2f, 0.5f, 0.7f), Quaternion.identity);
            var beam = Colliders.Box(cols, new Vector3(0f, h - 0.25f, 0f), new Vector3(7.4f, 0.5f, 0.7f), Quaternion.identity, "Pasarela");
            beam.gameObject.AddComponent<BalanceBeam>();
            Tag(beam, SurfaceKind.Piedra);
            // Raíces que salen de la base (el Custodio "sale de entre las raíces del arco")
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -4.5f : 4.5f;
                mb.For(Pal.Bark).Cylinder(new Vector3(side + (i % 3 - 1) * 0.6f, 0.2f, 0.9f), 0.12f, 0.04f, 2.4f, 5, Quaternion.Euler(70f, (i % 3 - 1) * 30f, 0f));
            }
            mb.Build(t, "ArcoPiedra");
        }

        /// <summary>La Parada Antigua: ruinas, columnas (una con el rombo del Reverso) y la Cápsula dañada.</summary>
        private static void Ruins(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.Lit(new Color(0.66f, 0.62f, 0.56f), 0.05f);
            var stoneDark = MaterialLibrary.Lit(new Color(0.5f, 0.47f, 0.43f), 0.05f);
            var moss = MaterialLibrary.Lit(Pal.Moss, 0.02f);
            // Estrado circular
            mb.For(stone).Cylinder(new Vector3(0f, -0.2f, 0f), 5.6f, 5.2f, 0.7f, 18, Quaternion.identity);
            mb.For(stoneDark).Cylinder(new Vector3(0f, 0.45f, 0f), 4.4f, 4.4f, 0.06f, 18, Quaternion.identity);
            Tag(Colliders.Box(cols, new Vector3(0f, 0.15f, 0f), new Vector3(10.4f, 0.6f, 10.4f), Quaternion.identity, "Estrado"), SurfaceKind.Piedra);
            Tag(Colliders.Box(cols, new Vector3(0f, 0.15f, 0f), new Vector3(10.4f, 0.6f, 10.4f), Quaternion.Euler(0f, 45f, 0f), "Estrado2"), SurfaceKind.Piedra);
            // Cuatro pedestales con los símbolos: agua, raíz, piedra, cielo
            string[] symbols = { "Agua", "Raíz", "Piedra", "Cielo" };
            Color[] symColors = { new Color(0.4f, 0.65f, 0.95f), new Color(0.55f, 0.4f, 0.25f), new Color(0.65f, 0.62f, 0.58f), new Color(0.85f, 0.85f, 1f) };
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, i * 90f + 45f, 0f);
                var p = rot * new Vector3(0f, 0f, 4.2f);
                mb.For(stone).Box(p + Vector3.up * 0.9f, new Vector3(0.6f, 1.1f, 0.6f), rot);
                mb.For(MaterialLibrary.Emissive(symColors[i], symColors[i], 0.9f)).Box(p + Vector3.up * 1.15f + rot * Vector3.forward * 0.31f, new Vector3(0.28f, 0.28f, 0.02f), rot * Quaternion.Euler(0f, 0f, i * 22f));
                Colliders.Box(cols, p + Vector3.up * 0.9f, new Vector3(0.6f, 1.1f, 0.6f), rot);
            }
            // Anillo de columnas (algunas rotas)
            for (int i = 0; i < 10; i++)
            {
                var wp = WorldLayout.RuinPillar(i);
                var lp = t.InverseTransformPoint(new Vector3(wp.X, t.position.y, wp.Z));
                lp.y = WorldShape.Height(wp.X, wp.Z) - t.position.y;
                float height = i % 3 == 1 ? 2.2f : (i % 3 == 2 ? 3.8f : 5.6f);
                if (i == WorldLayout.MarkedPillar) height = 4.6f;
                mb.For(stone).Cylinder(lp - Vector3.up * 0.3f, 0.75f, 0.68f, height + 0.3f, 8, Quaternion.identity);
                mb.For(stoneDark).Box(lp + Vector3.up * 0.15f, new Vector3(1.8f, 0.3f, 1.8f), Quaternion.identity);
                if (height > 5f) mb.For(stoneDark).Box(lp + Vector3.up * (height + 0.15f), new Vector3(1.7f, 0.3f, 1.7f), Quaternion.identity);
                mb.For(moss).Rock(lp + Vector3.up * (height * 0.6f), new Vector3(1.6f, 0.5f, 1.6f), Quaternion.identity, 1300 + i, 0.3f, false);
                Tag(Colliders.Capsule(cols, lp + Vector3.up * height * 0.5f, 0.75f, height, "Columna"), SurfaceKind.Piedra);
                if (i == WorldLayout.MarkedPillar)
                {
                    // Rombo del Reverso tallado junto a los símbolos kolotapir
                    var toCenter = (-lp).normalized;
                    var face = lp + toCenter * 0.72f + Vector3.up * 1.4f;
                    var frot = Quaternion.LookRotation(-toCenter);
                    mb.For(MaterialLibrary.Emissive(new Color(0.8f, 0.68f, 0.45f), new Color(0.55f, 0.4f, 0.2f), 0.8f)).Box(face, new Vector3(0.3f, 0.3f, 0.03f), frot * Quaternion.Euler(0f, 0f, 45f));
                }
            }
            // Columnas caídas y muretes
            var rng = new OromRandom(1400);
            for (int i = 0; i < 6; i++)
            {
                float a = rng.Range(0f, Mathf.PI * 2f);
                float r = rng.Range(16f, 24f);
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                p.y = WorldShape.Height(t.position.x + p.x, t.position.z + p.z) - t.position.y + 0.5f;
                var rot = Quaternion.Euler(0f, rng.Range(0f, 180f), 90f);
                mb.For(stone).Cylinder(p, 0.6f, 0.6f, rng.Range(2.5f, 4f), 8, rot);
                Colliders.Capsule(cols, p, 0.6f, 1.2f, "Caida");
            }
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 72f + 20f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 20f, 0f, Mathf.Sin(a) * 20f);
                p.y = WorldShape.Height(t.position.x + p.x, t.position.z + p.z) - t.position.y + 0.4f;
                var rot = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                mb.For(stoneDark).Box(p, new Vector3(4f, 0.9f, 0.7f), rot);
                Colliders.Box(cols, p, new Vector3(4f, 0.9f, 0.7f), rot);
            }
            mb.Build(t, "ParadaAntigua");
            var cap = new GameObject("CapsulaParada");
            cap.transform.SetParent(t, false);
            cap.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            cap.AddComponent<CapsuleController>().Init("cap_parada", false);
            AudioManager.AttachLoop(t.gameObject, "ruinas", 0.35f, 45f);
        }

        private static void RuinShelter(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.Lit(new Color(0.6f, 0.56f, 0.5f), 0.05f);
            float w = 4.2f, d = 3.8f, h = 2.5f;
            mb.For(stone).Box(new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.4f), Quaternion.identity);
            mb.For(stone).Box(new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), Quaternion.identity);
            mb.For(stone).Box(new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d), Quaternion.identity);
            mb.For(MaterialLibrary.Lit(new Color(0.52f, 0.48f, 0.43f), 0.05f)).Box(new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.6f, 0.3f, d + 0.6f), Quaternion.Euler(-4f, 0f, 0f));
            Colliders.Box(cols, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.45f));
            Colliders.Box(cols, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d));
            Colliders.Box(cols, new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.45f, h, d));
            Colliders.Box(cols, new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.6f, 0.3f, d + 0.6f));
            // Letras rayadas en la pared del fondo
            var scratch = MaterialLibrary.Lit(new Color(0.42f, 0.38f, 0.34f), 0.05f);
            for (int i = 0; i < 9; i++)
                mb.For(scratch).Box(new Vector3(-0.8f + i * 0.2f, 1.45f + (i % 2) * 0.05f, -d * 0.5f + 0.21f), new Vector3(0.12f, 0.02f, 0.01f), Quaternion.Euler(0f, 0f, (i * 37 % 60) - 30f));
            mb.For(MaterialLibrary.Lit(new Color(0.55f, 0.45f, 0.35f))).Box(new Vector3(1.1f, 0.1f, -0.8f), new Vector3(1.6f, 0.15f, 0.9f), Quaternion.identity);
            mb.Build(t, "Refugio");
            FirePoint.Register(t.position, 5f);
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 1.8f, -0.5f)), Pal.Warm, 6f, 1f, true, 1);
        }

        private static void Lookout(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            float h = WorldLayout.LookoutHeight;
            Color[] rocks = { new Color(0.5f, 0.47f, 0.44f), new Color(0.56f, 0.52f, 0.47f) };
            for (int k = 0; k < 5; k++)
            {
                float y = h * (k + 0.5f) / 5f;
                float r = 3.6f - k * 0.12f;
                mb.For(rocks[k % 2]).Rock(new Vector3(0f, y, 0f), new Vector3(r * 2f, h / 5f + 0.8f, r * 2f), Quaternion.Euler(0f, k * 31f, 0f), 1500 + k, 0.07f);
            }
            mb.For(MaterialLibrary.Lit(new Color(0.58f, 0.55f, 0.5f), 0.05f)).Cylinder(new Vector3(0f, h - 0.3f, 0f), 3.3f, 3.1f, 0.3f, 10, Quaternion.identity);
            mb.For(Pal.Moss).Rock(new Vector3(-1f, h, 1f), new Vector3(1.4f, 0.3f, 1.2f), Quaternion.identity, 1510, 0.3f, false);
            mb.Build(t, "Mirador");
            Tag(Colliders.Box(cols, new Vector3(0f, h * 0.5f, 0f), new Vector3(6.6f, h, 6.6f), Quaternion.identity, "Roca"), SurfaceKind.Piedra);
            Tag(Colliders.Box(cols, new Vector3(0f, h * 0.5f, 0f), new Vector3(6.6f, h, 6.6f), Quaternion.Euler(0f, 45f, 0f), "Roca2"), SurfaceKind.Piedra);
            var seat = new GameObject("Asiento");
            seat.transform.SetParent(t, false);
            seat.transform.localPosition = new Vector3(0f, h + 0.45f, 1.5f);
            seat.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            seat.AddComponent<Seat>();
        }

        private static void CloudRock(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            mb.For(new Color(0.55f, 0.52f, 0.48f)).Rock(new Vector3(0f, 0.3f, 0f), new Vector3(4.5f, 1.2f, 3.2f), Quaternion.Euler(0f, 20f, 0f), 1600, 0.1f);
            mb.Build(t, "RocaNubes");
            Colliders.Box(cols, new Vector3(0f, 0.35f, 0f), new Vector3(3.8f, 0.7f, 2.6f), Quaternion.Euler(0f, 20f, 0f));
            var seat = new GameObject("Asiento");
            seat.transform.SetParent(t, false);
            seat.transform.localPosition = new Vector3(1.2f, 1.05f, 0f);
            seat.transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
            seat.AddComponent<Seat>();
        }

        private static void KolotapirCamp(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            Color[] mats = { new Color(0.55f, 0.65f, 0.52f), new Color(0.75f, 0.55f, 0.35f) };
            for (int i = 0; i < 2; i++)
            {
                var p = new Vector3(4f + i * 1.6f, 0.06f, 3f);
                mb.For(mats[i]).Box(p, new Vector3(1.2f, 0.12f, 2.6f), Quaternion.Euler(0f, 15f, 0f));
                mb.For(MaterialLibrary.Lit(new Color(0.85f, 0.8f, 0.7f))).Box(p + new Vector3(0f, 0.1f, 1f), new Vector3(0.8f, 0.15f, 0.4f), Quaternion.Euler(0f, 15f, 0f));
            }
            // Secadero de hierbas
            var wood = MaterialLibrary.Lit(Pal.Wood, 0.1f);
            mb.For(wood).Box(new Vector3(-3f, 0.9f, 2f), new Vector3(0.1f, 1.8f, 0.1f), Quaternion.identity);
            mb.For(wood).Box(new Vector3(-1.2f, 0.9f, 2f), new Vector3(0.1f, 1.8f, 0.1f), Quaternion.identity);
            mb.For(wood).Box(new Vector3(-2.1f, 1.75f, 2f), new Vector3(2f, 0.08f, 0.08f), Quaternion.identity);
            for (int i = 0; i < 6; i++) mb.For(new Color(0.42f, 0.55f, 0.3f)).Cone(new Vector3(-2.9f + i * 0.32f, 1.7f, 2f), 0.1f, -0.45f, 5, Quaternion.identity);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                mb.For(Pal.StoneDark).Rock(new Vector3(Mathf.Cos(a) * 0.8f, 0.1f, Mathf.Sin(a) * 0.8f), new Vector3(0.4f, 0.28f, 0.35f), Quaternion.identity, 860 + i, 0.25f, false);
            }
            Props.Pot(mb, new Vector3(1.4f, 0f, -0.6f), new Color(0.6f, 0.4f, 0.3f));
            mb.Build(t, "CampamentoKolotapir");
            FX.Fire(t, new Vector3(0f, 0.1f, 0f), 0.7f);
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 1f, 0f)), Pal.Fire, 11f, 1.6f, false, 3, true);
            FirePoint.Register(t.position, 7f);
        }

        private static void EastGate(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.Lit(new Color(0.7f, 0.64f, 0.55f), 0.05f);
            // Arco de piedra sobre el camino (el frente +Z mira al este)
            foreach (float x in new[] { -3.5f, 3.5f })
            {
                mb.For(stone).Box(new Vector3(x, 2.5f, 0f), new Vector3(1f, 5f, 1f), Quaternion.identity);
                Colliders.Box(cols, new Vector3(x, 2.5f, 0f), new Vector3(1f, 5f, 1f));
            }
            mb.For(stone).Box(new Vector3(0f, 5.2f, 0f), new Vector3(8.2f, 0.6f, 1.1f), Quaternion.identity);
            mb.For(MaterialLibrary.Emissive(new Color(0.6f, 0.7f, 0.95f), new Color(0.35f, 0.45f, 0.9f), 0.9f)).Box(new Vector3(0f, 5.2f, 0.58f), new Vector3(0.5f, 0.5f, 0.03f), Quaternion.Euler(0f, 0f, 45f));
            // Faroles azules de la Liga
            foreach (float x in new[] { -5f, 5f })
            {
                mb.For(Pal.WoodDark).Box(new Vector3(x, 1.2f, 1.5f), new Vector3(0.14f, 2.4f, 0.14f), Quaternion.identity);
                mb.For(MaterialLibrary.Emissive(new Color(0.5f, 0.65f, 1f), new Color(0.3f, 0.5f, 1f), 1.6f)).Sphere(new Vector3(x, 2.5f, 1.5f), new Vector3(0.18f, 0.25f, 0.18f), 6, 4, Quaternion.identity);
            }
            mb.Build(t, "PuertaEste");
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 3f, 1.5f)), new Color(0.5f, 0.65f, 1f), 12f, 1.5f, true, 2);
            // Límite del prototipo: más allá, la Ciudad de las Mil Cápsulas
            var wall = new GameObject("LimiteEste");
            wall.transform.SetParent(t.parent, false);
            wall.transform.position = new Vector3(988f, t.position.y + 5f, 600f);
            var wc = wall.AddComponent<BoxCollider>();
            wc.size = new Vector3(2f, 30f, 60f);
        }

        private static void BoarDen(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 50f - 60f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a) * 3.2f, 0.4f, -Mathf.Cos(a) * 3.2f);
                Props.Rock(mb, p, new Vector3(1.8f, 1.4f, 1.5f), 1700 + i, true);
                Colliders.Sphere(cols, p + Vector3.up * 0.2f, 0.75f);
            }
            for (int i = 0; i < 5; i++)
                mb.For(new Color(0.85f, 0.82f, 0.75f)).Cylinder(new Vector3(i * 0.3f - 0.6f, 0.05f, 0.6f + (i % 2) * 0.3f), 0.04f, 0.03f, 0.5f, 4, Quaternion.Euler(0f, i * 40f, 88f));
            mb.Build(t, "Guarida");
        }

        private static void Islet(Transform t)
        {
            var mb = new ModelBuilder();
            Props.Tree(mb, new Vector3(-2f, -0.1f, -1.5f), 0.9f, 1800, TreeKind.Memoria);
            Props.Rock(mb, new Vector3(2.5f, 0f, -2f), new Vector3(1.6f, 1f, 1.4f), 1801, true);
            mb.Build(t, "Islote");
            Colliders.Capsule(K.Child(t, "Col"), new Vector3(-2f, 1.5f, -1.5f), 0.35f, 3f);
        }
    }

    /// <summary>Oculta un objeto cuando se cumple una condición del estado (p. ej. la cuenta de Boro ya entregada).</summary>
    public sealed class HideWhen : MonoBehaviour
    {
        public System.Func<GameState, bool> Condition;
        private float _t;

        private void Update()
        {
            _t -= Time.deltaTime;
            if (_t > 0f) return;
            _t = 1f;
            if (Game.State != null && Condition != null && Condition(Game.State)) gameObject.SetActive(false);
        }
    }
}
