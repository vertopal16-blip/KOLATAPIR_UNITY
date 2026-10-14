using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Esqueleto simple de un personaje (pivotes para la animación procedural).</summary>
    public sealed class HumanoidRig
    {
        public Transform Root, Body, Torso, Head, ArmL, ArmR, LegL, LegR, EarL, EarR, Snout, Prop;
        public Species Species;
        public float Height;
        public float HipHeight;
        public float Breath;
    }

    /// <summary>
    /// Construye humanos de la Tribu del Reverso y kolotapires humanoides a partir de su apariencia
    /// (lámina 1: hocico de tapir, orejas expresivas, cuerpo robusto, 1,8–2,2 m; lámina 2: ropa reparada y funcional).
    /// </summary>
    public static class HumanoidBuilder
    {
        public static HumanoidRig Build(Transform parent, Species species, Appearance look, Material overrideMat = null, bool isNilo = false)
        {
            var rig = new HumanoidRig { Species = species, Height = look.Height };
            var root = new GameObject(species == Species.Kolotapir ? "Kolotapir" : "Humano").transform;
            root.SetParent(parent, false);
            rig.Root = root;
            Material M(string hex, float s = 0.12f) => overrideMat != null ? overrideMat : MaterialLibrary.Lit(K.Hex(hex), s);
            Material MC(Color c, float s = 0.12f) => overrideMat != null ? overrideMat : MaterialLibrary.Lit(c, s);
            if (species == Species.Kolotapir) BuildKolotapir(rig, look, M, MC);
            else BuildHuman(rig, look, M, MC, isNilo);
            return rig;
        }

        private delegate Material MatHex(string hex, float s = 0.12f);
        private delegate Material MatCol(Color c, float s = 0.12f);

        private static void BuildHuman(HumanoidRig rig, Appearance look, MatHex M, MatCol MC, bool isNilo)
        {
            float H = look.Height, b = look.Build;
            float leg = 0.47f * H, torso = 0.3f * H, headR = 0.068f * H;
            rig.HipHeight = leg;
            var skin = M(look.Skin, 0.25f);
            var cloth = M(look.Cloth);
            var cloth2 = M(look.Cloth2);
            var accent = M(look.Accent);
            var pants = M(look.Pants);
            var hair = M(look.Hair, 0.3f);
            var boot = MC(new Color(0.28f, 0.2f, 0.14f), 0.2f);

            rig.Body = K.Child(rig.Root, "Cadera", new Vector3(0f, leg, 0f));
            // Piernas
            float hipW = 0.055f * H * b;
            foreach (float side in new[] { -1f, 1f })
            {
                var l = K.Child(rig.Body, side < 0 ? "PiernaI" : "PiernaD", new Vector3(side * hipW, 0f, 0f));
                var mb = new ModelBuilder();
                mb.For(pants).Cylinder(new Vector3(0f, -leg + 0.08f * H, 0f), 0.034f * H * b, 0.045f * H * b, leg - 0.08f * H, 7, Quaternion.identity);
                mb.For(boot).Cylinder(new Vector3(0f, -leg, 0.01f), 0.04f * H, 0.038f * H, 0.1f * H, 7, Quaternion.identity);
                mb.For(boot).Box(new Vector3(0f, -leg + 0.02f * H, 0.045f * H), new Vector3(0.07f * H, 0.04f * H, 0.1f * H), Quaternion.identity);
                mb.Build(l, "Pierna");
                if (side < 0) rig.LegL = l; else rig.LegR = l;
            }
            // Torso y ropa (túnica hasta medio muslo)
            rig.Torso = K.Child(rig.Body, "Torso");
            var tb = new ModelBuilder();
            float w = 0.12f * H * b;
            tb.For(cloth).Lathe(new Vector3(0f, -0.2f * H, 0f), new[]
            {
                new Vector2(w * 1.2f, 0f), new Vector2(w * 1.05f, 0.12f * H), new Vector2(w * 0.92f, 0.22f * H), new Vector2(w * 1.02f, 0.38f * H),
                new Vector2(w * 0.95f, 0.47f * H), new Vector2(w * 0.4f, 0.5f * H), new Vector2(0.03f * H, 0.505f * H),
            }, 10, Quaternion.identity, true, true);
            tb.For(accent).Cylinder(new Vector3(0f, 0.01f * H, 0f), w * 0.97f, w * 0.97f, 0.035f * H, 10, Quaternion.identity);
            tb.For(cloth2).Box(new Vector3(0f, 0.15f * H, w * 0.92f), new Vector3(w * 0.5f, 0.2f * H, 0.01f * H), Quaternion.Euler(-4f, 0f, 0f));
            if (look.Shawl)
                tb.For(accent).Lathe(new Vector3(0f, 0.2f * H, 0f), new[] { new Vector2(w * 1.35f, 0f), new Vector2(w * 1.15f, 0.06f * H), new Vector2(w * 0.55f, 0.11f * H) }, 10, Quaternion.identity, false, false);
            if (look.Apron)
                tb.For(MC(new Color(0.85f, 0.8f, 0.7f))).Box(new Vector3(0f, -0.04f * H, w * 1.05f), new Vector3(w * 1.3f, 0.24f * H, 0.01f * H), Quaternion.Euler(-6f, 0f, 0f));
            if (look.Backpack)
                tb.For(cloth2).Box(new Vector3(0f, 0.16f * H, -w * 1.15f), new Vector3(w * 1.3f, 0.2f * H, 0.1f * H), Quaternion.identity);
            if (isNilo)
            {
                // Correa y amuleto familiar (lámina de Nilo)
                tb.For(MC(new Color(0.36f, 0.25f, 0.16f))).Box(new Vector3(0f, 0.17f * H, w * 0.75f), new Vector3(0.02f * H, 0.3f * H, 0.01f * H), Quaternion.Euler(0f, 0f, 35f));
                tb.For(MC(Pal.Gold, 0.6f)).Cylinder(new Vector3(0f, 0.25f * H, w * 0.98f), 0.022f * H, 0.022f * H, 0.006f * H, 8, Quaternion.Euler(90f, 0f, 0f));
            }
            tb.Build(rig.Torso, "Torso");

            float shoulderY = 0.3f * H;
            // Cabeza
            rig.Head = K.Child(rig.Torso, "Cabeza", new Vector3(0f, shoulderY + 0.035f * H, 0f));
            var hb = new ModelBuilder();
            hb.For(skin).Cylinder(Vector3.zero, 0.028f * H, 0.025f * H, 0.04f * H, 7, Quaternion.identity);
            hb.For(skin).Sphere(new Vector3(0f, headR + 0.03f * H, 0.005f * H), new Vector3(headR * 0.92f, headR * 1.05f, headR * 0.98f), 10, 7, Quaternion.identity);
            hb.For(skin).Sphere(new Vector3(0f, headR + 0.02f * H, headR * 0.98f), new Vector3(0.012f * H, 0.016f * H, 0.012f * H), 5, 3, Quaternion.identity);
            var eye = isNilo ? MC(Pal.NiloEyes, 0.8f) : M(look.Eyes, 0.7f);
            var white = MC(new Color(0.95f, 0.94f, 0.9f), 0.5f);
            foreach (float side in new[] { -1f, 1f })
            {
                var ep = new Vector3(side * headR * 0.38f, headR + 0.045f * H, headR * 0.84f);
                hb.For(white).Sphere(ep, Vector3.one * 0.013f * H, 6, 4, Quaternion.identity);
                hb.For(eye).Sphere(ep + new Vector3(0f, 0f, 0.008f * H), Vector3.one * 0.0085f * H, 6, 4, Quaternion.identity);
            }
            // Pelo
            float hy = headR + 0.03f * H;
            switch (look.HairStyle)
            {
                case "mono":
                    hb.For(hair).Sphere(new Vector3(0f, hy + headR * 0.25f, -headR * 0.1f), new Vector3(headR * 1.0f, headR * 0.85f, headR * 1.02f), 9, 6, Quaternion.identity);
                    hb.For(hair).Sphere(new Vector3(0f, hy + headR * 0.6f, -headR * 0.85f), Vector3.one * headR * 0.45f, 8, 5, Quaternion.identity);
                    break;
                case "largo":
                    hb.For(hair).Sphere(new Vector3(0f, hy + headR * 0.2f, -headR * 0.15f), new Vector3(headR * 1.05f, headR * 0.9f, headR * 1.05f), 9, 6, Quaternion.identity);
                    hb.For(hair).Box(new Vector3(0f, hy - headR * 0.9f, -headR * 0.7f), new Vector3(headR * 1.7f, headR * 2.2f, headR * 0.4f), Quaternion.identity);
                    break;
                case "trenzas":
                    hb.For(hair).Sphere(new Vector3(0f, hy + headR * 0.22f, -headR * 0.12f), new Vector3(headR * 1.02f, headR * 0.85f, headR * 1.02f), 9, 6, Quaternion.identity);
                    foreach (float side in new[] { -1f, 1f })
                        hb.For(hair).Cylinder(new Vector3(side * headR * 0.75f, hy - headR * 0.3f, -headR * 0.2f), 0.012f * H, 0.008f * H, 0.22f * H, 6, Quaternion.Euler(180f, 0f, side * 8f));
                    break;
                case "rizado":
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        hb.For(hair).Sphere(new Vector3(Mathf.Cos(a) * headR * 0.65f, hy + headR * 0.55f, Mathf.Sin(a) * headR * 0.6f - headR * 0.1f), Vector3.one * headR * 0.48f, 6, 4, Quaternion.identity);
                    }
                    break;
                case "panuelo":
                    hb.For(accent).Sphere(new Vector3(0f, hy + headR * 0.25f, -headR * 0.1f), new Vector3(headR * 1.08f, headR * 0.9f, headR * 1.1f), 9, 6, Quaternion.identity);
                    hb.For(accent).Sphere(new Vector3(0f, hy - headR * 0.2f, -headR * 1.0f), Vector3.one * headR * 0.4f, 6, 4, Quaternion.identity);
                    break;
                case "capucha":
                    hb.For(cloth2).Sphere(new Vector3(0f, hy + headR * 0.15f, -headR * 0.15f), new Vector3(headR * 1.25f, headR * 1.25f, headR * 1.3f), 9, 6, Quaternion.identity);
                    break;
                default:
                    // Despeinado: mechones irregulares (Nilo, Kiko)
                    hb.For(hair).Sphere(new Vector3(0f, hy + headR * 0.28f, -headR * 0.08f), new Vector3(headR * 1.03f, headR * 0.82f, headR * 1.03f), 9, 6, Quaternion.identity);
                    var rng = new OromRandom(OromRandom.HashString(look.Skin + look.Hair));
                    for (int i = 0; i < 9; i++)
                    {
                        float a = rng.Range(0f, Mathf.PI * 2f);
                        var p = new Vector3(Mathf.Cos(a) * headR * 0.75f, hy + headR * rng.Range(0.35f, 0.85f), Mathf.Sin(a) * headR * 0.75f - headR * 0.1f);
                        hb.For(hair).Cone(p, headR * 0.28f, headR * rng.Range(0.35f, 0.6f), 4, Quaternion.FromToRotation(Vector3.up, (p - new Vector3(0f, hy, 0f)).normalized + Vector3.up * 0.4f));
                    }
                    break;
            }
            if (look.Beard) hb.For(hair).Sphere(new Vector3(0f, hy - headR * 0.75f, headR * 0.55f), new Vector3(headR * 0.62f, headR * 0.55f, headR * 0.45f), 7, 5, Quaternion.identity);
            if (look.Hat && look.HairStyle != "panuelo") hb.For(cloth2).Cylinder(new Vector3(0f, hy + headR * 0.7f, 0f), headR * 1.6f, headR * 0.7f, headR * 0.6f, 10, Quaternion.identity);
            hb.Build(rig.Head, "Cabeza");

            // Brazos
            float shoulderX = 0.14f * H * b;
            float arm = 0.37f * H;
            foreach (float side in new[] { -1f, 1f })
            {
                var a = K.Child(rig.Torso, side < 0 ? "BrazoI" : "BrazoD", new Vector3(side * shoulderX, shoulderY - 0.02f * H, 0f));
                var ab = new ModelBuilder();
                ab.For(cloth).Cylinder(new Vector3(0f, -arm * 0.62f, 0f), 0.032f * H * b, 0.04f * H * b, arm * 0.62f, 7, Quaternion.identity);
                ab.For(skin).Cylinder(new Vector3(0f, -arm * 0.95f, 0f), 0.024f * H, 0.03f * H, arm * 0.36f, 6, Quaternion.identity);
                ab.For(skin).Sphere(new Vector3(0f, -arm, 0.005f * H), new Vector3(0.03f * H, 0.038f * H, 0.022f * H), 6, 4, Quaternion.identity);
                ab.Build(a, "Brazo");
                if (side < 0) rig.ArmL = a; else rig.ArmR = a;
            }
            AddProps(rig, look, M, MC, arm);
        }

        private static void BuildKolotapir(HumanoidRig rig, Appearance look, MatHex M, MatCol MC)
        {
            float H = look.Height, b = look.Build;
            float leg = 0.3f * H;
            rig.HipHeight = leg;
            var skin = M(look.Skin, 0.15f);
            var skinDark = MC(K.Mul(K.Hex(look.Skin), 0.7f), 0.15f);
            var pink = MC(new Color(0.85f, 0.6f, 0.58f), 0.2f);
            var cloth = M(look.Cloth);
            var cloth2 = M(look.Cloth2);
            var accent = M(look.Accent);
            var pants = M(look.Pants);
            var nail = MC(new Color(0.15f, 0.13f, 0.12f), 0.4f);

            rig.Body = K.Child(rig.Root, "Cadera", new Vector3(0f, leg, 0f));
            float hipW = 0.08f * H * b;
            foreach (float side in new[] { -1f, 1f })
            {
                var l = K.Child(rig.Body, side < 0 ? "PiernaI" : "PiernaD", new Vector3(side * hipW, 0f, 0f));
                var mb = new ModelBuilder();
                mb.For(pants).Cylinder(new Vector3(0f, -leg + 0.07f * H, 0f), 0.06f * H * b, 0.07f * H * b, leg - 0.07f * H, 8, Quaternion.identity);
                mb.For(skinDark).Cylinder(new Vector3(0f, -leg, 0.01f * H), 0.06f * H * b, 0.058f * H * b, 0.08f * H, 8, Quaternion.identity);
                for (int t = -1; t <= 1; t++)
                    mb.For(nail).Sphere(new Vector3(t * 0.025f * H, -leg + 0.012f * H, 0.06f * H * b), new Vector3(0.017f * H, 0.014f * H, 0.02f * H), 5, 3, Quaternion.identity);
                mb.Build(l, "Pierna");
                if (side < 0) rig.LegL = l; else rig.LegR = l;
            }
            rig.Torso = K.Child(rig.Body, "Torso");
            var tb = new ModelBuilder();
            float w = 0.17f * H * b;
            // Cuerpo robusto en forma de pera, con túnica hasta las rodillas
            tb.For(cloth).Lathe(new Vector3(0f, -0.16f * H, 0f), new[]
            {
                new Vector2(w * 1.15f, 0f), new Vector2(w * 1.18f, 0.12f * H), new Vector2(w * 1.12f, 0.27f * H), new Vector2(w * 0.95f, 0.42f * H),
                new Vector2(w * 0.7f, 0.5f * H), new Vector2(w * 0.35f, 0.53f * H), new Vector2(0.03f * H, 0.535f * H),
            }, 11, Quaternion.identity, true, true);
            tb.For(accent).Cylinder(new Vector3(0f, 0.04f * H, 0f), w * 1.17f, w * 1.17f, 0.04f * H, 11, Quaternion.identity);
            tb.For(cloth2).Box(new Vector3(0f, 0.12f * H, w * 1.12f), new Vector3(w * 0.45f, 0.26f * H, 0.01f * H), Quaternion.Euler(-8f, 0f, 0f));
            // Bufanda o capa (variante por rol)
            tb.For(cloth2).Lathe(new Vector3(0f, 0.31f * H, 0f), new[] { new Vector2(w * 1.05f, 0f), new Vector2(w * 0.85f, 0.06f * H), new Vector2(w * 0.45f, 0.08f * H) }, 11, Quaternion.identity, false, false);
            // Símbolos comunes (rombos) en la ropa
            tb.For(accent).Box(new Vector3(w * 0.5f, 0.2f * H, w * 1.02f), new Vector3(0.04f * H, 0.04f * H, 0.005f * H), Quaternion.Euler(0f, 0f, 45f));
            tb.For(accent).Box(new Vector3(-w * 0.5f, 0.2f * H, w * 1.02f), new Vector3(0.04f * H, 0.04f * H, 0.005f * H), Quaternion.Euler(0f, 0f, 45f));
            if (look.Apron) tb.For(MC(new Color(0.88f, 0.84f, 0.74f))).Box(new Vector3(0f, -0.02f * H, w * 1.2f), new Vector3(w * 1.4f, 0.26f * H, 0.01f * H), Quaternion.Euler(-6f, 0f, 0f));
            if (look.Backpack)
            {
                // Mochila enorme con objetos colgando (Boro)
                tb.For(MC(new Color(0.5f, 0.38f, 0.26f))).Box(new Vector3(0f, 0.22f * H, -w * 1.3f), new Vector3(w * 1.6f, 0.34f * H, 0.16f * H), Quaternion.identity);
                tb.For(MC(new Color(0.42f, 0.5f, 0.6f))).Cylinder(new Vector3(0f, 0.4f * H, -w * 1.3f), 0.07f * H, 0.07f * H, 0.25f * H, 8, Quaternion.Euler(0f, 0f, 90f));
                for (int i = 0; i < 5; i++)
                    tb.For(MC(i % 2 == 0 ? new Color(0.75f, 0.75f, 0.72f) : new Color(0.78f, 0.6f, 0.3f), 0.6f)).Sphere(new Vector3(-w * 0.7f + i * w * 0.35f, 0.02f * H, -w * 1.42f), Vector3.one * 0.022f * H, 5, 3, Quaternion.identity);
            }
            tb.Build(rig.Torso, "Torso");

            float shoulderY = 0.34f * H;
            rig.Head = K.Child(rig.Torso, "Cabeza", new Vector3(0f, shoulderY + 0.02f * H, 0.02f * H));
            var hb = new ModelBuilder();
            float hr = 0.095f * H;
            hb.For(skin).Cylinder(Vector3.zero, 0.06f * H, 0.055f * H, 0.05f * H, 8, Quaternion.identity);
            hb.For(skin).Sphere(new Vector3(0f, hr + 0.03f * H, 0f), new Vector3(hr, hr * 0.95f, hr * 1.05f), 10, 7, Quaternion.identity);
            var eyeDark = MC(new Color(0.06f, 0.04f, 0.03f), 0.8f);
            var eyeShine = MC(new Color(0.42f, 0.28f, 0.16f), 0.8f);
            foreach (float side in new[] { -1f, 1f })
            {
                var ep = new Vector3(side * hr * 0.55f, hr + 0.05f * H, hr * 0.72f);
                hb.For(eyeShine).Sphere(ep, Vector3.one * 0.016f * H, 6, 4, Quaternion.identity);
                hb.For(eyeDark).Sphere(ep + new Vector3(0f, 0f, 0.007f * H), Vector3.one * 0.011f * H, 6, 4, Quaternion.identity);
            }
            hb.Build(rig.Head, "Cabeza");
            // Hocico de tapir: largo, móvil y algo caído
            rig.Snout = K.Child(rig.Head, "Hocico", new Vector3(0f, hr + 0.01f * H, hr * 0.8f));
            var sb = new ModelBuilder();
            sb.For(skin).Cylinder(Vector3.zero, 0.055f * H, 0.04f * H, 0.11f * H, 9, Quaternion.Euler(80f, 0f, 0f));
            sb.For(skin).Cylinder(new Vector3(0f, -0.02f * H, 0.105f * H), 0.04f * H, 0.03f * H, 0.07f * H, 8, Quaternion.Euler(115f, 0f, 0f));
            sb.For(skinDark).Sphere(new Vector3(0f, -0.05f * H, 0.165f * H), new Vector3(0.032f * H, 0.024f * H, 0.02f * H), 7, 4, Quaternion.identity);
            sb.For(MC(new Color(0.2f, 0.15f, 0.14f))).Box(new Vector3(0f, -0.045f * H, 0.02f * H), new Vector3(0.06f * H, 0.004f * H, 0.03f * H), Quaternion.Euler(10f, 0f, 0f));
            sb.Build(rig.Snout, "Hocico");
            // Orejas expresivas, rosadas por dentro
            foreach (float side in new[] { -1f, 1f })
            {
                var e = K.Child(rig.Head, side < 0 ? "OrejaI" : "OrejaD", new Vector3(side * hr * 0.75f, hr * 1.7f + 0.03f * H, -hr * 0.1f));
                var eb = new ModelBuilder();
                eb.For(skin).Sphere(new Vector3(side * 0.02f * H, 0.035f * H, 0f), new Vector3(0.035f * H, 0.05f * H, 0.014f * H), 7, 5, Quaternion.Euler(0f, 0f, -side * 25f));
                eb.For(pink).Sphere(new Vector3(side * 0.02f * H, 0.035f * H, 0.009f * H), new Vector3(0.025f * H, 0.038f * H, 0.006f * H), 7, 5, Quaternion.Euler(0f, 0f, -side * 25f));
                eb.Build(e, "Oreja");
                if (side < 0) rig.EarL = e; else rig.EarR = e;
            }
            float shoulderX = 0.2f * H * b;
            float arm = 0.33f * H;
            foreach (float side in new[] { -1f, 1f })
            {
                var a = K.Child(rig.Torso, side < 0 ? "BrazoI" : "BrazoD", new Vector3(side * shoulderX, shoulderY - 0.04f * H, 0f));
                var ab = new ModelBuilder();
                ab.For(cloth).Cylinder(new Vector3(0f, -arm * 0.6f, 0f), 0.05f * H * b, 0.06f * H * b, arm * 0.6f, 8, Quaternion.identity);
                ab.For(skin).Cylinder(new Vector3(0f, -arm * 0.95f, 0f), 0.04f * H * b, 0.048f * H * b, arm * 0.38f, 7, Quaternion.identity);
                ab.For(skinDark).Sphere(new Vector3(0f, -arm, 0.01f * H), new Vector3(0.05f * H, 0.05f * H, 0.04f * H), 7, 5, Quaternion.identity);
                ab.Build(a, "Brazo");
                if (side < 0) rig.ArmL = a; else rig.ArmR = a;
            }
            AddProps(rig, look, M, MC, arm);
        }

        private static void AddProps(HumanoidRig rig, Appearance look, MatHex M, MatCol MC, float arm)
        {
            float H = look.Height;
            if (look.Staff)
            {
                // Bastón con orbe luminoso (ancianos, guardianes de recuerdos)
                rig.Prop = K.Child(rig.ArmR, "Baston", new Vector3(0f, -arm, 0.03f * H));
                var pb = new ModelBuilder();
                pb.For(MC(new Color(0.42f, 0.3f, 0.2f))).Cylinder(new Vector3(0f, -0.45f * H, 0f), 0.014f * H, 0.012f * H, 1.05f * H, 6, Quaternion.identity);
                pb.For(MC(new Color(0.42f, 0.3f, 0.2f))).Lathe(new Vector3(0f, 0.6f * H, 0f), new[] { new Vector2(0.05f * H, 0f), new Vector2(0.06f * H, 0.05f * H), new Vector2(0.05f * H, 0.1f * H) }, 8, Quaternion.identity, false, false);
                pb.For(MaterialLibrary.Emissive(Pal.Warm, Pal.Warm, 1.3f)).Sphere(new Vector3(0f, 0.65f * H, 0f), Vector3.one * 0.03f * H, 8, 5, Quaternion.identity);
                pb.Build(rig.Prop, "Baston", false);
            }
            if (look.Basket)
            {
                var basket = K.Child(rig.ArmL, "Cesta", new Vector3(0f, -arm * 0.85f, 0.06f * H));
                var bb = new ModelBuilder();
                Props.Basket(bb, Vector3.zero, new Color(0.45f, 0.62f, 0.3f));
                bb.Build(basket, "Cesta", false);
                basket.localScale = Vector3.one * (H / 1.7f);
            }
            if (look.Hat && rig.Species == Species.Kolotapir)
            {
                var hat = new ModelBuilder();
                float hr = 0.095f * H;
                hat.For(MC(new Color(0.52f, 0.4f, 0.28f))).Cylinder(new Vector3(0f, hr * 1.75f + 0.03f * H, -hr * 0.1f), hr * 1.7f, hr * 1.6f, hr * 0.12f, 12, Quaternion.identity);
                hat.For(MC(new Color(0.52f, 0.4f, 0.28f))).Cylinder(new Vector3(0f, hr * 1.85f + 0.03f * H, -hr * 0.1f), hr * 0.85f, hr * 0.7f, hr * 0.7f, 10, Quaternion.identity);
                hat.For(M(look.Accent)).Cylinder(new Vector3(0f, hr * 1.9f + 0.03f * H, -hr * 0.1f), hr * 0.86f, hr * 0.84f, hr * 0.15f, 10, Quaternion.identity);
                hat.Build(rig.Head, "Sombrero", false);
            }
        }
    }

    /// <summary>
    /// Animación procedural: andar, actividades de la rutina, gestos al hablar y emociones legibles
    /// "incluso sin diálogo" (doc: ojos, orejas, hocico e inclinación corporal comunican intención).
    /// </summary>
    public static class HumanoidAnimator
    {
        public static void Pose(HumanoidRig rig, float time, float speed, Activity activity, Emotion emotion, bool talking, Vector3? lookAt, float dt = 0.016f)
        {
            if (rig == null || rig.Body == null) return;
            float H = rig.Height;
            float walk = Mathf.Clamp01(speed / 1.3f);
            float phase = time * (speed > 0.05f ? 5.2f * Mathf.Max(0.8f, speed / 1.3f) * 1.7f / H : 0f);
            float legSwing = Mathf.Sin(phase) * 28f * walk;
            float armSwing = Mathf.Sin(phase) * 22f * walk;
            float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.025f * H * walk;
            float breath = Mathf.Sin(time * 1.6f) * 0.01f;

            Vector3 bodyPos = new Vector3(0f, rig.HipHeight + bob, 0f);
            Quaternion torso = Quaternion.identity, head = Quaternion.identity;
            Quaternion legL = Quaternion.Euler(legSwing, 0f, 0f), legR = Quaternion.Euler(-legSwing, 0f, 0f);
            Quaternion armL = Quaternion.Euler(-armSwing, 0f, -4f), armR = Quaternion.Euler(armSwing, 0f, 4f);

            if (speed < 0.05f)
            {
                switch (activity)
                {
                    case Activity.Sentarse:
                    case Activity.Comer:
                    case Activity.Contar:
                    case Activity.Mirar:
                        bodyPos.y = rig.HipHeight * 0.55f;
                        legL = Quaternion.Euler(-80f, -6f, 0f);
                        legR = Quaternion.Euler(-80f, 6f, 0f);
                        armL = Quaternion.Euler(-30f, 0f, -8f);
                        armR = Quaternion.Euler(-30f, 0f, 8f);
                        if (activity == Activity.Comer && Mathf.Repeat(time, 4f) < 1.2f) armR = Quaternion.Euler(-120f, 0f, -20f);
                        if (activity == Activity.Contar) { armR = Quaternion.Euler(-60f + Mathf.Sin(time * 2.5f) * 25f, 0f, 15f); armL = Quaternion.Euler(-45f - Mathf.Sin(time * 2f) * 15f, 0f, -12f); }
                        if (activity == Activity.Mirar) head = Quaternion.Euler(-32f, Mathf.Sin(time * 0.3f) * 15f, 0f);
                        break;
                    case Activity.Trabajar:
                        armR = Quaternion.Euler(-70f + Mathf.Abs(Mathf.Sin(time * 4f)) * -50f, 0f, 10f);
                        armL = Quaternion.Euler(-45f, 0f, -10f);
                        torso = Quaternion.Euler(12f, 0f, 0f);
                        head = Quaternion.Euler(18f, 0f, 0f);
                        break;
                    case Activity.Cocinar:
                        armR = Quaternion.Euler(-55f, Mathf.Sin(time * 3f) * 25f, Mathf.Cos(time * 3f) * 15f);
                        torso = Quaternion.Euler(8f, 0f, 0f);
                        head = Quaternion.Euler(20f, 0f, 0f);
                        break;
                    case Activity.Recolectar:
                    {
                        float bend = Mathf.Clamp01(Mathf.Sin(time * 0.9f) * 1.4f);
                        torso = Quaternion.Euler(45f * bend, 0f, 0f);
                        armL = Quaternion.Euler(-40f * bend, 0f, -6f);
                        armR = Quaternion.Euler(-55f * bend + Mathf.Sin(time * 4f) * 10f * bend, 0f, 6f);
                        break;
                    }
                    case Activity.Meditar:
                        head = Quaternion.Euler(22f, 0f, 0f);
                        armL = Quaternion.Euler(-60f, 0f, 30f);
                        armR = Quaternion.Euler(-60f, 0f, -30f);
                        break;
                    case Activity.Conversar:
                        armR = Quaternion.Euler(-25f + Mathf.Sin(time * 2.2f) * 20f, 0f, 12f);
                        break;
                    case Activity.Vigilar:
                    case Activity.Patrullar:
                        head = Quaternion.Euler(0f, Mathf.Sin(time * 0.5f) * 35f, 0f);
                        armL = Quaternion.Euler(-10f, 0f, -12f);
                        armR = Quaternion.Euler(-10f, 0f, 12f);
                        break;
                }
            }
            if (talking)
            {
                armR = Quaternion.Euler(-40f + Mathf.Sin(time * 3.1f) * 25f, Mathf.Sin(time * 1.7f) * 15f, 18f);
                head *= Quaternion.Euler(Mathf.Sin(time * 5f) * 4f, 0f, 0f);
            }

            // Emociones
            float earPitch = 0f, earRoll = 0f, snout = Mathf.Sin(time * 1.3f) * 4f;
            switch (emotion)
            {
                case Emotion.Alegre: head *= Quaternion.Euler(-6f, 0f, 0f); earPitch = -15f; snout += 8f; break;
                case Emotion.Triste: head *= Quaternion.Euler(14f, 0f, 0f); earRoll = 45f; snout -= 10f; break;
                case Emotion.Curioso: head *= Quaternion.Euler(0f, 0f, 12f); earPitch = -25f; snout += Mathf.Sin(time * 9f) * 6f; break;
                case Emotion.Sorprendido: head *= Quaternion.Euler(-12f, 0f, 0f); earRoll = -15f; earPitch = -10f; break;
                case Emotion.Enfadado: head *= Quaternion.Euler(6f, 0f, 0f); earPitch = 40f; snout -= 5f; break;
                case Emotion.Preocupado: head *= Quaternion.Euler(6f, 0f, -5f); earRoll = 25f; break;
                case Emotion.Determinado: head *= Quaternion.Euler(-4f, 0f, 0f); earPitch = -5f; break;
            }

            if (lookAt.HasValue && rig.Head != null)
            {
                var local = rig.Root.InverseTransformPoint(lookAt.Value);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(local.y - H * 0.9f, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -30f, 30f);
                if (Mathf.Abs(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg) < 110f) head = Quaternion.Euler(pitch, yaw, 0f) * head;
            }

            float k = 1f - Mathf.Exp(-10f * dt);
            rig.Body.localPosition = Vector3.Lerp(rig.Body.localPosition, bodyPos, k);
            rig.Torso.localRotation = Quaternion.Slerp(rig.Torso.localRotation, torso, k);
            rig.Torso.localScale = new Vector3(1f, 1f + breath, 1f);
            rig.Head.localRotation = Quaternion.Slerp(rig.Head.localRotation, head, k);
            rig.LegL.localRotation = Quaternion.Slerp(rig.LegL.localRotation, legL, k * 1.5f);
            rig.LegR.localRotation = Quaternion.Slerp(rig.LegR.localRotation, legR, k * 1.5f);
            rig.ArmL.localRotation = Quaternion.Slerp(rig.ArmL.localRotation, armL, k);
            rig.ArmR.localRotation = Quaternion.Slerp(rig.ArmR.localRotation, armR, k);
            if (rig.EarL != null)
            {
                float twitch = Mathf.PerlinNoise(time * 0.7f, rig.Height * 10f) > 0.8f ? Mathf.Sin(time * 30f) * 10f : 0f;
                rig.EarL.localRotation = Quaternion.Slerp(rig.EarL.localRotation, Quaternion.Euler(earPitch, 0f, earRoll + twitch), k);
                rig.EarR.localRotation = Quaternion.Slerp(rig.EarR.localRotation, Quaternion.Euler(earPitch, 0f, -earRoll - twitch), k);
            }
            if (rig.Snout != null) rig.Snout.localRotation = Quaternion.Slerp(rig.Snout.localRotation, Quaternion.Euler(snout + (talking ? Mathf.Sin(time * 11f) * 6f : 0f), 0f, 0f), k);
        }
    }
}
