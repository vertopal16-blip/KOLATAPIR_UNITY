using System;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Construye las estructuras del prototipo a partir de <see cref="StructureDef"/>. Todo se modela en
    /// coordenadas locales (frente = +Z) y se agrupa por material.
    /// </summary>
    public static partial class StructureBuilder
    {
        public static void Build(StructureDef def, Transform parent)
        {
            float gy = WorldShape.Height(def.Pos.X, def.Pos.Z);
            var root = new GameObject(def.Kind + (def.Param != null ? "_" + def.Param : ""));
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(def.Pos.X, gy, def.Pos.Z);
            root.transform.rotation = K.Yaw(def.Rot);
            var t = root.transform;
            switch (def.Kind)
            {
                case "house": House(t, def.Param); break;
                case "workshop": Workshop(t); break;
                case "storehouse": Storehouse(t); break;
                case "plaza": Plaza(t); break;
                case "fields": Fields(t); break;
                case "giant_tree": GiantTree(t); break;
                case "migration_stones": MigrationStones(t); break;
                case "gate": Gate(t); break;
                case "guard_post": GuardPost(t); break;
                case "training_dummy": TrainingDummy.Create(t); break;
                case "rope_bridge": RopeBridge(t); break;
                case "study_hut": StudyHut(t); break;
                case "capsule_hollow": CapsuleHollow(t); break;
                case "grotto": Grotto(t); break;
                case "waterfall": Waterfall(t); break;
                case "lantern": Lantern(t); break;
                case "mojon": Mojon(t); break;
                case "boro_camp": BoroCamp(t); break;
                case "stone_arch": StoneArch(t); break;
                case "ruins": Ruins(t); break;
                case "ruin_shelter": RuinShelter(t); break;
                case "lookout": Lookout(t); break;
                case "cloud_rock": CloudRock(t); break;
                case "kolotapir_camp": KolotapirCamp(t); break;
                case "east_gate": EastGate(t); break;
                case "boar_den": BoarDen(t); break;
                case "islet": Islet(t); break;
                default: Debug.LogWarning("KOLOTOPIR: estructura desconocida " + def.Kind); break;
            }
        }

        private static void Tag(Component c, SurfaceKind k) => c.gameObject.AddComponent<SurfaceTag>().Kind = k;

        // ================================================================ Tribu del Reverso

        private static Color OwnerColor(string owner)
        {
            switch (owner)
            {
                case "gina": return new Color(0.56f, 0.23f, 0.27f);
                case "oren": return new Color(0.34f, 0.41f, 0.29f);
                case "haro": return new Color(0.55f, 0.18f, 0.18f);
                case "tome": return new Color(0.31f, 0.42f, 0.55f);
                case "suri": return new Color(0.71f, 0.32f, 0.23f);
                case "lia": return new Color(0.31f, 0.49f, 0.42f);
                default: return Pal.Cloth;
            }
        }

        /// <summary>Casa redonda de piedra y adobe con tejado rojo (lámina de la tribu): sentirse usada y mantenida.</summary>
        private static void House(Transform t, string owner)
        {
            var mb = new ModelBuilder();
            const float r = 4f, wallH = 2.7f, thick = 0.42f;
            int seg = 18;
            var stone = MaterialLibrary.World(Pal.StoneWarm, 0.05f);
            var plaster = MaterialLibrary.World(new Color(0.84f, 0.76f, 0.62f), 0.05f);
            var cols = K.Child(t, "Colisiones");
            for (int i = 0; i < seg; i++)
            {
                float a = (i + 0.5f) * 360f / seg;
                float delta = Mathf.DeltaAngle(a, 0f);
                if (Mathf.Abs(delta) < 22f) continue; // hueco de la puerta (+Z)
                var rot = Quaternion.Euler(0f, a, 0f);
                var c = rot * new Vector3(0f, 0f, r);
                float segLen = 2f * Mathf.PI * r / seg + 0.08f;
                mb.For(stone).Box(c + Vector3.up * 0.55f, new Vector3(segLen, 1.1f, thick + 0.06f), rot);
                mb.For(plaster).Box(c + Vector3.up * (1.1f + (wallH - 1.1f) * 0.5f), new Vector3(segLen, wallH - 1.1f, thick), rot);
                Tag(Colliders.Box(cols, c + Vector3.up * wallH * 0.5f, new Vector3(segLen, wallH, thick + 0.1f), rot, "Muro"), SurfaceKind.Piedra);
            }
            // Marco de la puerta
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            var doorL = Quaternion.Euler(0f, -21f, 0f) * new Vector3(0f, 0f, r);
            var doorR = Quaternion.Euler(0f, 21f, 0f) * new Vector3(0f, 0f, r);
            mb.For(wood).Box(doorL + Vector3.up * 1.3f, new Vector3(0.22f, 2.6f, 0.5f), Quaternion.Euler(0f, -21f, 0f));
            mb.For(wood).Box(doorR + Vector3.up * 1.3f, new Vector3(0.22f, 2.6f, 0.5f), Quaternion.Euler(0f, 21f, 0f));
            mb.For(wood).Box(new Vector3(0f, 2.55f, r - 0.05f), new Vector3(3.2f, 0.25f, 0.5f), Quaternion.identity);
            // Cortina de tela en la puerta (recogida a un lado)
            mb.For(OwnerColor(owner)).Box(doorL + new Vector3(0.35f, 1.4f, 0.18f), new Vector3(0.45f, 2.2f, 0.05f), Quaternion.Euler(0f, -21f, 0f));
            // Tejado rojo en capas
            var roof = MaterialLibrary.World(Pal.Roof, 0.08f);
            var roofDark = MaterialLibrary.World(Pal.RoofDark, 0.08f);
            mb.For(roof).Lathe(new Vector3(0f, wallH - 0.05f, 0f), new[] { new Vector2(5.3f, 0f), new Vector2(3.8f, 1.0f), new Vector2(2.0f, 2.2f), new Vector2(0.3f, 3.2f), new Vector2(0f, 3.3f) }, 14, Quaternion.identity, true, false);
            mb.For(roofDark).Lathe(new Vector3(0f, wallH - 0.25f, 0f), new[] { new Vector2(5.45f, 0f), new Vector2(5.35f, 0.25f) }, 14, Quaternion.identity, false, false);
            mb.For(wood).Cylinder(new Vector3(0f, wallH + 3.1f, 0f), 0.12f, 0.06f, 0.8f, 6, Quaternion.identity);
            // Suelo de madera y alfombra
            mb.For(MaterialLibrary.World(Pal.WoodLight, 0.1f)).Cylinder(new Vector3(0f, 0f, 0f), r - 0.1f, r - 0.1f, 0.06f, 16, Quaternion.identity);
            mb.For(OwnerColor(owner)).Cylinder(new Vector3(0.4f, 0.07f, 0.3f), 1.4f, 1.4f, 0.02f, 12, Quaternion.identity);
            // Cama al fondo
            var bedC = new Vector3(0f, 0f, -2.2f);
            mb.For(wood).Box(bedC + new Vector3(0f, 0.2f, 0f), new Vector3(2.1f, 0.4f, 1.05f), Quaternion.identity);
            mb.For(MaterialLibrary.World(new Color(0.86f, 0.82f, 0.72f), 0.05f)).Box(bedC + new Vector3(0f, 0.45f, 0f), new Vector3(1.95f, 0.14f, 0.95f), Quaternion.identity);
            mb.For(OwnerColor(owner)).Box(bedC + new Vector3(0.3f, 0.54f, 0f), new Vector3(1.3f, 0.06f, 0.98f), Quaternion.identity);
            mb.For(MaterialLibrary.World(new Color(0.92f, 0.9f, 0.84f), 0.05f)).Box(bedC + new Vector3(-0.78f, 0.58f, 0f), new Vector3(0.38f, 0.14f, 0.6f), Quaternion.identity);
            // Mesa, banquetas y estantería
            mb.For(wood).Cylinder(new Vector3(1.8f, 0f, 0.6f), 0.08f, 0.08f, 0.72f, 6, Quaternion.identity);
            mb.For(MaterialLibrary.World(Pal.WoodLight, 0.15f)).Cylinder(new Vector3(1.8f, 0.72f, 0.6f), 0.6f, 0.6f, 0.06f, 10, Quaternion.identity);
            mb.For(wood).Cylinder(new Vector3(1.2f, 0f, 1.2f), 0.2f, 0.2f, 0.42f, 7, Quaternion.identity);
            mb.For(wood).Box(new Vector3(-2.6f, 1.0f, 0.8f), new Vector3(0.35f, 2f, 1.4f), Quaternion.Euler(0f, 30f, 0f));
            Props.Pot(mb, new Vector3(-2.5f, 2.02f, 0.7f), new Color(0.7f, 0.4f, 0.28f));
            Props.Pot(mb, new Vector3(1.75f, 0.78f, 0.5f), new Color(0.6f, 0.45f, 0.3f));
            Colliders.Box(cols, bedC + Vector3.up * 0.3f, new Vector3(2.1f, 0.6f, 1.05f));
            Colliders.Box(cols, new Vector3(1.8f, 0.4f, 0.6f), new Vector3(1.1f, 0.8f, 1.1f));
            // Exterior: leña, barril, cestas, maceta
            var outside = Quaternion.Euler(0f, 120f, 0f) * new Vector3(0f, 0f, r + 0.9f);
            Props.Firewood(mb, outside, 120f);
            Props.Barrel(mb, Quaternion.Euler(0f, -110f, 0f) * new Vector3(0f, 0f, r + 0.7f), 0f);
            Props.Basket(mb, Quaternion.Euler(0f, -60f, 0f) * new Vector3(0f, 0f, r + 0.6f), new Color(0.75f, 0.6f, 0.3f));
            Colliders.Box(cols, outside + Vector3.up * 0.35f, new Vector3(1.2f, 0.7f, 1.1f), Quaternion.Euler(0f, 120f, 0f));
            OwnerDecor(mb, cols, owner);
            mb.Build(t, "Casa");
            FX.Smoke(t, new Vector3(0f, wallH + 3.4f, 0f));
            // Luz cálida interior por la noche
            var lamp = K.Child(t, "Candil", new Vector3(-1.6f, 1.9f, -1.0f));
            LightSource.Create(lamp, Pal.Warm, 6f, 1.4f, true, 1);
            var lampMb = new ModelBuilder();
            lampMb.For(MaterialLibrary.Emissive(Pal.Warm, Pal.Warm, 1.5f)).Sphere(Vector3.zero, Vector3.one * 0.12f, 8, 5, Quaternion.identity);
            lampMb.Build(lamp, "Candil", false);
        }

        private static void OwnerDecor(ModelBuilder mb, Transform cols, string owner)
        {
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            switch (owner)
            {
                case "gina":
                    // Telar en el porche y macetas con flores.
                    var loom = new Vector3(2.2f, 0f, 5.2f);
                    mb.For(wood).Box(loom + new Vector3(-0.6f, 0.75f, 0f), new Vector3(0.1f, 1.5f, 0.1f), Quaternion.identity);
                    mb.For(wood).Box(loom + new Vector3(0.6f, 0.75f, 0f), new Vector3(0.1f, 1.5f, 0.1f), Quaternion.identity);
                    mb.For(wood).Box(loom + new Vector3(0f, 1.45f, 0f), new Vector3(1.3f, 0.08f, 0.08f), Quaternion.identity);
                    mb.For(new Color(0.42f, 0.18f, 0.55f)).Box(loom + new Vector3(0f, 0.95f, 0f), new Vector3(1.1f, 0.9f, 0.03f), Quaternion.identity);
                    for (int i = 0; i < 3; i++)
                    {
                        var p = new Vector3(-2.4f + i * 0.5f, 0f, 4.8f);
                        Props.Pot(mb, p, new Color(0.65f, 0.38f, 0.26f));
                        mb.For(new Color(0.95f, 0.75f, 0.3f)).Sphere(p + Vector3.up * 0.52f, Vector3.one * 0.12f, 6, 4, Quaternion.identity);
                    }
                    break;
                case "oren":
                    mb.For(MaterialLibrary.World(new Color(0.88f, 0.84f, 0.72f))).Cylinder(new Vector3(1.6f, 0.78f, 0.7f), 0.05f, 0.05f, 0.5f, 6, Quaternion.Euler(0f, 0f, 90f));
                    mb.For(MaterialLibrary.World(new Color(0.6f, 0.4f, 0.25f))).Cylinder(new Vector3(-1.4f, 0f, 1.8f), 0.3f, 0.32f, 0.5f, 10, Quaternion.identity);
                    break;
                case "haro":
                    for (int i = 0; i < 3; i++)
                        mb.For(wood).Cylinder(new Vector3(-1.2f + i * 0.25f, 0f, 3.1f), 0.03f, 0.03f, 2.1f, 5, Quaternion.Euler(-12f, 0f, 0f));
                    mb.For(MaterialLibrary.World(new Color(0.7f, 0.72f, 0.75f), 0.6f, 0.7f)).Box(new Vector3(-0.95f, 2.2f, 3.55f), new Vector3(0.08f, 0.3f, 0.02f), Quaternion.identity);
                    break;
                case "tome":
                    mb.For(wood).Box(new Vector3(2.2f, 0.45f, -0.8f), new Vector3(0.9f, 0.9f, 0.5f), Quaternion.Euler(0f, 60f, 0f));
                    break;
                case "lia":
                    Props.Basket(mb, new Vector3(1.4f, 0f, 5.0f), new Color(0.4f, 0.6f, 0.3f));
                    Props.Basket(mb, new Vector3(2.0f, 0f, 4.6f), new Color(0.75f, 0.68f, 0.35f));
                    break;
                case "suri":
                    Props.Pot(mb, new Vector3(-1.8f, 0f, 1.6f), new Color(0.5f, 0.3f, 0.2f));
                    Props.Pot(mb, new Vector3(-1.4f, 0f, 2.0f), new Color(0.55f, 0.35f, 0.22f));
                    mb.For(new Color(0.4f, 0.55f, 0.25f)).Box(new Vector3(0f, 2.3f, 0f), new Vector3(1.6f, 0.05f, 0.05f), Quaternion.identity);
                    break;
            }
        }

        private static void Workshop(Transform t)
        {
            var mb = new ModelBuilder();
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            var cols = K.Child(t, "Colisiones");
            float w = 6f, d = 5f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * w * 0.5f, z = (i < 2 ? -1 : 1) * d * 0.5f;
                float h = z < 0 ? 3.2f : 2.6f;
                mb.For(wood).Box(new Vector3(x, h * 0.5f, z), new Vector3(0.22f, h, 0.22f), Quaternion.identity);
                Colliders.Box(cols, new Vector3(x, h * 0.5f, z), new Vector3(0.25f, h, 0.25f));
            }
            mb.For(MaterialLibrary.World(Pal.Thatch, 0.05f)).Box(new Vector3(0f, 3.0f, 0f), new Vector3(w + 1.2f, 0.18f, d + 1.4f), Quaternion.Euler(-7f, 0f, 0f));
            mb.For(MaterialLibrary.World(Pal.WoodDark, 0.1f)).Box(new Vector3(0f, 0.55f, -2.6f), new Vector3(w, 1.1f, 0.12f), Quaternion.identity);
            // Mesa de trabajo
            var bench = new Vector3(0f, 0f, -0.9f);
            mb.For(MaterialLibrary.World(Pal.WoodLight, 0.15f)).Box(bench + Vector3.up * 0.85f, new Vector3(2.4f, 0.12f, 1f), Quaternion.identity);
            for (int i = 0; i < 4; i++) mb.For(wood).Box(bench + new Vector3((i % 2 == 0 ? -1.05f : 1.05f), 0.4f, (i < 2 ? -0.4f : 0.4f)), new Vector3(0.12f, 0.8f, 0.12f), Quaternion.identity);
            mb.For(MaterialLibrary.World(new Color(0.65f, 0.65f, 0.68f), 0.6f, 0.7f)).Box(bench + new Vector3(-0.6f, 0.95f, 0f), new Vector3(0.35f, 0.04f, 0.08f), Quaternion.Euler(0f, 25f, 0f));
            mb.For(Pal.Wood).Box(bench + new Vector3(0.5f, 0.98f, 0.1f), new Vector3(0.5f, 0.12f, 0.25f), Quaternion.Euler(0f, -10f, 0f));
            mb.For(MaterialLibrary.World(new Color(0.55f, 0.36f, 0.22f))).Box(bench + new Vector3(0.9f, 0.93f, -0.2f), new Vector3(0.4f, 0.03f, 0.3f), Quaternion.identity);
            Colliders.Box(cols, bench + Vector3.up * 0.45f, new Vector3(2.4f, 0.9f, 1f));
            // Yunque de piedra y troncos
            mb.For(MaterialLibrary.World(Pal.StoneDark, 0.2f)).Box(new Vector3(2f, 0.4f, 1.2f), new Vector3(0.7f, 0.8f, 0.5f), Quaternion.Euler(0f, 20f, 0f));
            Props.Firewood(mb, new Vector3(-2.4f, 0f, 1.6f), 90f);
            Props.Crate(mb, new Vector3(2.4f, 0f, -1.8f), 10f);
            mb.Build(t, "Taller");
            var station = new GameObject("MesaDeTrabajo");
            station.transform.SetParent(t, false);
            station.transform.localPosition = bench + Vector3.up * 1f;
            station.AddComponent<CraftingStation>().Owner = "Tomé";
        }

        private static void Storehouse(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            var planks = MaterialLibrary.World(Pal.WoodLight, 0.08f);
            float w = 6f, d = 4.4f, h = 2.6f;
            // paredes con hueco de puerta delante
            mb.For(planks).Box(new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.18f), Quaternion.identity);
            mb.For(planks).Box(new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, d), Quaternion.identity);
            mb.For(planks).Box(new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, d), Quaternion.identity);
            mb.For(planks).Box(new Vector3(-1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.18f), Quaternion.identity);
            mb.For(planks).Box(new Vector3(1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.18f), Quaternion.identity);
            Colliders.Box(cols, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.25f));
            Colliders.Box(cols, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.25f, h, d));
            Colliders.Box(cols, new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.25f, h, d));
            Colliders.Box(cols, new Vector3(-1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.25f));
            Colliders.Box(cols, new Vector3(1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.25f));
            // Tejado a dos aguas con vigas de bisagra (vestigio de las casas plegables)
            var roof = MaterialLibrary.World(Pal.Roof, 0.08f);
            mb.For(roof).Box(new Vector3(0f, h + 0.7f, -1.25f), new Vector3(w + 0.8f, 0.14f, 3.0f), Quaternion.Euler(28f, 0f, 0f));
            mb.For(roof).Box(new Vector3(0f, h + 0.7f, 1.25f), new Vector3(w + 0.8f, 0.14f, 3.0f), Quaternion.Euler(-28f, 0f, 0f));
            mb.For(wood).Box(new Vector3(0f, h + 1.4f, 0f), new Vector3(w + 1f, 0.2f, 0.2f), Quaternion.identity);
            mb.For(MaterialLibrary.World(new Color(0.35f, 0.35f, 0.36f), 0.5f, 0.7f)).Box(new Vector3(-w * 0.5f + 0.3f, h - 0.1f, 0f), new Vector3(0.1f, 0.3f, 0.3f), Quaternion.identity);
            for (int i = 0; i < 3; i++) Props.Barrel(mb, new Vector3(-2.2f + i * 0.75f, 0f, -1.4f), i * 30f);
            Props.Crate(mb, new Vector3(1.8f, 0f, -1.5f), 0f, 0.7f);
            Props.Crate(mb, new Vector3(1.8f, 0.7f, -1.5f), 25f, 0.55f);
            Props.Basket(mb, new Vector3(1.2f, 0f, 2.9f), new Color(0.82f, 0.68f, 0.3f));
            Props.Basket(mb, new Vector3(-1.4f, 0f, 3.0f), new Color(0.6f, 0.2f, 0.3f));
            mb.Build(t, "Almacen");
        }

        private static void Plaza(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.World(Pal.StoneDark, 0.1f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                mb.For(stone).Rock(new Vector3(Mathf.Cos(a) * 1.25f, 0.12f, Mathf.Sin(a) * 1.25f), new Vector3(0.6f, 0.4f, 0.5f), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), 30 + i, 0.25f, false);
            }
            mb.For(MaterialLibrary.World(new Color(0.15f, 0.12f, 0.1f))).Cylinder(Vector3.zero, 1f, 1f, 0.05f, 12, Quaternion.identity);
            for (int i = 0; i < 5; i++)
                mb.For(Pal.WoodDark).Cylinder(new Vector3(0f, 0.12f, 0f), 0.08f, 0.06f, 1.1f, 5, Quaternion.Euler(70f, i * 72f, 0f));
            Colliders.Box(cols, new Vector3(0f, 0.3f, 0f), new Vector3(2.4f, 0.6f, 2.4f));
            // Bancos de tronco alrededor del fuego
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 15f;
                var rot = Quaternion.Euler(0f, a, 0f);
                var p = rot * new Vector3(0f, 0f, 4.6f);
                mb.For(Pal.Wood).Cylinder(p + rot * new Vector3(-1.1f, 0.28f, 0f), 0.28f, 0.28f, 2.2f, 8, rot * Quaternion.Euler(0f, 0f, -90f));
                var seat = new GameObject("Banco");
                seat.transform.SetParent(t, false);
                seat.transform.localPosition = p + Vector3.up * 0.55f;
                seat.transform.localRotation = rot * Quaternion.Euler(0f, 180f, 0f);
                seat.AddComponent<Seat>();
                Colliders.Box(cols, p + Vector3.up * 0.28f, new Vector3(2.2f, 0.56f, 0.56f), rot);
            }
            // Trípode y olla de Suri
            var cook = new Vector3(4.5f, 0f, -4.5f);
            for (int i = 0; i < 3; i++)
                mb.For(Pal.WoodDark).Cylinder(cook + Quaternion.Euler(0f, i * 120f, 0f) * new Vector3(0.6f, 0f, 0f), 0.05f, 0.04f, 1.6f, 5, Quaternion.Euler(0f, i * 120f, 0f) * Quaternion.Euler(0f, 0f, 20f));
            mb.For(MaterialLibrary.World(new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.6f)).Lathe(cook + Vector3.up * 0.4f, new[] { new Vector2(0.2f, 0f), new Vector2(0.38f, 0.2f), new Vector2(0.34f, 0.45f) }, 12, Quaternion.identity, true, false);
            mb.Build(t, "Plaza");
            FX.Fire(t, new Vector3(0f, 0.15f, 0f), 1.1f);
            FX.Fire(t, cook + Vector3.up * 0.15f, 0.4f);
            LightSource.Create(K.Child(t, "LuzFuego", new Vector3(0f, 1.2f, 0f)), Pal.Fire, 14f, 2.2f, false, 3, true);
            FirePoint.Register(t.position, 8f);
        }

        private static void Fields(Transform t)
        {
            var mb = new ModelBuilder();
            var soil = MaterialLibrary.World(new Color(0.36f, 0.26f, 0.17f), 0.05f);
            var crop = MaterialLibrary.World(new Color(0.4f, 0.62f, 0.24f), 0.1f);
            var grain = MaterialLibrary.World(new Color(0.82f, 0.7f, 0.32f), 0.1f);
            for (int row = 0; row < 8; row++)
            {
                float z = -9f + row * 2.4f;
                mb.For(soil).Box(new Vector3(0f, 0.12f, z), new Vector3(26f, 0.3f, 1.1f), Quaternion.identity);
                for (int i = 0; i < 18; i++)
                {
                    var p = new Vector3(-12f + i * 1.42f, 0.25f, z);
                    if (row < 4) mb.For(crop).Cone(p, 0.22f, 0.5f + (i % 3) * 0.1f, 5, Quaternion.identity);
                    else mb.For(grain).Cylinder(p, 0.05f, 0.03f, 0.9f, 4, Quaternion.Euler((i % 2) * 6f, 0f, 0f));
                }
            }
            // Espantapájaros
            mb.For(Pal.Wood).Box(new Vector3(6f, 1.1f, 0.3f), new Vector3(0.1f, 2.2f, 0.1f), Quaternion.identity);
            mb.For(Pal.Wood).Box(new Vector3(6f, 1.7f, 0.3f), new Vector3(1.4f, 0.08f, 0.08f), Quaternion.identity);
            mb.For(new Color(0.42f, 0.18f, 0.55f)).Box(new Vector3(6f, 1.45f, 0.3f), new Vector3(0.7f, 0.7f, 0.3f), Quaternion.identity);
            mb.For(Pal.Thatch).Sphere(new Vector3(6f, 2.3f, 0.3f), Vector3.one * 0.25f, 7, 5, Quaternion.identity);
            mb.Build(t, "Campos");
            var fence = new ModelBuilder();
            Vector3 L(float x, float z) => new Vector3(x, 0f, z);
            Props.Fence(fence, L(-14f, -11f), L(14f, -11f));
            Props.Fence(fence, L(14f, -11f), L(14f, 11f));
            Props.Fence(fence, L(-14f, 11f), L(-3f, 11f));
            Props.Fence(fence, L(3f, 11f), L(14f, 11f));
            Props.Fence(fence, L(-14f, -11f), L(-14f, 11f));
            fence.Build(t, "Valla");
        }

        /// <summary>El Árbol de los Mayores: hito visible desde los Prados.</summary>
        private static void GiantTree(Transform t)
        {
            var mb = new ModelBuilder { SmoothAngle = 50f, Detail = 1.6f };
            var bark = MaterialLibrary.Surface(DetailKind.Bark, new Color(0.33f, 0.25f, 0.2f), 0.05f);
            mb.For(bark).Lathe(Vector3.zero, new[] { new Vector2(5.5f, -0.5f), new Vector2(3.6f, 1.5f), new Vector2(2.9f, 5f), new Vector2(2.4f, 11f), new Vector2(1.8f, 16f), new Vector2(1.2f, 19f) }, 12, Quaternion.identity, false, true);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 51f;
                var rot = Quaternion.Euler(0f, a, 0f);
                mb.For(bark).Cylinder(rot * new Vector3(0f, 0f, 2.6f) + Vector3.down * 0.2f, 1.1f, 0.3f, 6f, 6, rot * Quaternion.Euler(78f, 0f, 0f));
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f + 20f;
                var rot = Quaternion.Euler(0f, a, 0f);
                mb.For(bark).Cylinder(new Vector3(0f, 12f + i * 1.3f, 0f), 0.9f, 0.3f, 10f, 6, rot * Quaternion.Euler(60f, 0f, 0f));
            }
            var rng = new OromRandom(2024);
            Color[] greens = { new Color(0.28f, 0.47f, 0.2f), new Color(0.35f, 0.55f, 0.23f), new Color(0.42f, 0.58f, 0.28f) };
            for (int i = 0; i < 14; i++)
            {
                float a = rng.Range(0f, Mathf.PI * 2f);
                float r = rng.Range(2f, 11f);
                var p = new Vector3(Mathf.Cos(a) * r, 19f + rng.Range(-2.5f, 5f), Mathf.Sin(a) * r);
                var size = new Vector3(9f, 6.5f, 9f) * rng.Range(0.8f, 1.2f);
                mb.For(MaterialLibrary.Surface(DetailKind.Foliage, greens[i % 3] * 0.65f, 0.05f)).Rock(p, size * 0.85f, Quaternion.identity, 700 + i, 0.2f, false);
                var leaves = Props.Leaves(greens[i % 3]);
                if (leaves != null) Props.LeafShell(mb.For(leaves), p, size * 0.5f, 26, 3.2f, rng, 0.3f);
            }
            // Faroles colgados de las ramas y cintas
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 6.5f, 10.5f - (i % 2) * 1.2f, Mathf.Sin(a) * 6.5f);
                mb.For(Pal.Rope).Cylinder(p, 0.02f, 0.02f, 1.5f, 3, Quaternion.identity, false, false);
                mb.For(MaterialLibrary.Emissive(Pal.Warm, Pal.Warm, 1.2f)).Sphere(p, new Vector3(0.22f, 0.3f, 0.22f), 6, 4, Quaternion.identity);
            }
            mb.Build(t, "ArbolMayores");
            var cols = K.Child(t, "Colisiones");
            Tag(Colliders.Capsule(cols, new Vector3(0f, 8f, 0f), 3.3f, 16f, "Tronco"), SurfaceKind.Madera);
            Colliders.Capsule(cols, new Vector3(0f, 1f, 0f), 4.6f, 2f, "Base");
            LightSource.Create(K.Child(t, "Luz", new Vector3(0f, 9.5f, 0f)), Pal.Warm, 18f, 1.2f, true, 2);
        }

        private static void MigrationStones(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var glyph = MaterialLibrary.Emissive(new Color(0.75f, 0.65f, 0.5f), new Color(0.6f, 0.45f, 0.25f), 0.6f);
            int n = 14;
            int[] gaps = { 3, 7, 11 };
            for (int i = 0; i < n; i++)
            {
                if (Array.IndexOf(gaps, i) >= 0) continue;
                float a = i * Mathf.PI * 2f / n;
                var p = new Vector3(Mathf.Cos(a) * 7f, 0f, Mathf.Sin(a) * 7f);
                var rot = Quaternion.LookRotation(-p.normalized);
                float h = 1.6f + (i % 4) * 0.35f;
                mb.For(Pal.Stone).Rock(p + Vector3.up * h * 0.45f, new Vector3(0.9f, h, 0.55f), rot, 300 + i, 0.12f);
                mb.For(glyph).Box(p + Vector3.up * h * 0.55f - p.normalized * 0.3f, new Vector3(0.22f, 0.22f, 0.03f), rot * Quaternion.Euler(0f, 0f, 45f));
                Tag(Colliders.Box(cols, p + Vector3.up * h * 0.5f, new Vector3(0.8f, h, 0.5f), rot), SurfaceKind.Piedra);
            }
            // Huecos: hierba distinta donde hubo piedras
            foreach (int g in gaps)
            {
                float a = g * Mathf.PI * 2f / n;
                mb.For(new Color(0.5f, 0.55f, 0.3f)).Cylinder(new Vector3(Mathf.Cos(a) * 7f, 0.01f, Mathf.Sin(a) * 7f), 0.6f, 0.6f, 0.02f, 8, Quaternion.identity);
            }
            mb.Build(t, "PiedrasMigracion");
        }

        private static void Gate(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            foreach (float x in new[] { -2.4f, 2.4f })
            {
                mb.For(wood).Cylinder(new Vector3(x, 0f, 0f), 0.26f, 0.2f, 4.4f, 7, Quaternion.identity);
                Colliders.Box(cols, new Vector3(x, 2.2f, 0f), new Vector3(0.5f, 4.4f, 0.5f));
            }
            mb.For(wood).Box(new Vector3(0f, 4.2f, 0f), new Vector3(6f, 0.3f, 0.35f), Quaternion.identity);
            mb.For(MaterialLibrary.World(Pal.Roof, 0.08f)).Box(new Vector3(0f, 4.5f, 0f), new Vector3(6.6f, 0.15f, 1f), Quaternion.identity);
            // Amuletos y cintas colgantes
            Color[] ribbon = { new Color(0.42f, 0.18f, 0.55f), new Color(0.71f, 0.32f, 0.23f), new Color(0.79f, 0.63f, 0.29f) };
            for (int i = 0; i < 7; i++)
            {
                float x = -2f + i * 0.66f;
                mb.For(Pal.Rope).Cylinder(new Vector3(x, 3.4f, 0f), 0.015f, 0.015f, 0.8f, 3, Quaternion.identity, false, false);
                mb.For(ribbon[i % 3]).Box(new Vector3(x, 3.2f, 0f), new Vector3(0.12f, 0.5f, 0.02f), Quaternion.identity);
                mb.For(Pal.Gold).Box(new Vector3(x, 3.45f, 0.02f), new Vector3(0.14f, 0.14f, 0.02f), Quaternion.Euler(0f, 0f, 45f));
            }
            // Muretes de piedra a ambos lados
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    var p = new Vector3(side * (3.4f + i * 1.6f), 0.35f, 0f);
                    mb.For(Pal.StoneWarm).Rock(p, new Vector3(1.7f, 0.9f, 0.9f), Quaternion.identity, 900 + i * side, 0.15f);
                    Colliders.Box(cols, p, new Vector3(1.6f, 0.9f, 0.8f));
                }
            mb.Build(t, "Puerta");
        }

        private static void GuardPost(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            // Atalaya pequeña
            var tower = new Vector3(-4f, 0f, -2f);
            for (int i = 0; i < 4; i++)
            {
                var p = tower + new Vector3((i % 2 == 0 ? -1f : 1f), 0f, (i < 2 ? -1f : 1f));
                mb.For(wood).Box(p + Vector3.up * 1.9f, new Vector3(0.18f, 3.8f, 0.18f), Quaternion.identity);
                Colliders.Box(cols, p + Vector3.up * 1.9f, new Vector3(0.2f, 3.8f, 0.2f));
            }
            mb.For(MaterialLibrary.World(Pal.WoodLight, 0.1f)).Box(tower + Vector3.up * 2.2f, new Vector3(2.4f, 0.15f, 2.4f), Quaternion.identity);
            mb.For(MaterialLibrary.World(Pal.Roof, 0.08f)).Lathe(tower + Vector3.up * 3.8f, new[] { new Vector2(1.9f, 0f), new Vector2(0.1f, 1.2f), new Vector2(0f, 1.25f) }, 4, Quaternion.Euler(0f, 45f, 0f), true, false);
            Colliders.Box(cols, tower + Vector3.up * 2.2f, new Vector3(2.4f, 0.2f, 2.4f));
            // Soporte de lanzas y banco
            for (int i = 0; i < 3; i++)
                mb.For(wood).Cylinder(new Vector3(2.5f + i * 0.3f, 0f, -2.5f), 0.03f, 0.03f, 2.3f, 5, Quaternion.Euler(-10f, 0f, 0f));
            mb.For(wood).Box(new Vector3(2.8f, 0.9f, -2.65f), new Vector3(1.2f, 0.08f, 0.1f), Quaternion.identity);
            mb.For(Pal.Wood).Cylinder(new Vector3(-1.2f, 0.25f, 3f), 0.25f, 0.25f, 2f, 8, Quaternion.Euler(0f, 0f, 90f));
            Props.Barrel(mb, new Vector3(-2.4f, 0f, 2.2f), 0f);
            mb.Build(t, "PuestoGuardia");
            // Bloqueo del puente (hasta la despedida de Gina)
            var block = new GameObject("BloqueoPuente");
            block.transform.SetParent(t.parent, false);
            var west = WorldShape.BridgeWest;
            block.transform.position = new Vector3(west.X - 1.6f, WorldShape.Height(west.X - 1.6f, west.Z - 1.6f) + 1.5f, west.Z - 1.6f);
            block.transform.rotation = K.Yaw(45f);
            var bc = block.AddComponent<BoxCollider>();
            bc.size = new Vector3(4.5f, 3.5f, 0.6f);
            block.AddComponent<BridgeBlocker>();
        }

        /// <summary>Puente colgante: el límite del territorio del Reverso.</summary>
        private static void RopeBridge(Transform t)
        {
            // Este constructor trabaja en coordenadas de mundo: el padre se reinicia.
            t.rotation = Quaternion.identity;
            t.position = Vector3.zero;
            var west = WorldShape.BridgeWest;
            var east = WorldShape.BridgeEast;
            var a = new Vector3(west.X, WorldShape.Height(west.X, west.Z) + 0.05f, west.Z);
            var b = new Vector3(east.X, WorldShape.Height(east.X, east.Z) + 0.05f, east.Z);
            // Extender un poco sobre tierra firme en ambos extremos
            var dir = (b - a);
            var flat = new Vector3(dir.x, 0f, dir.z).normalized;
            a -= flat * 2.5f; a.y = WorldShape.Height(a.x, a.z) + 0.05f;
            b += flat * 2.5f; b.y = WorldShape.Height(b.x, b.z) + 0.05f;
            float length = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
            var right = Vector3.Cross(Vector3.up, flat);
            const float sag = 1.6f;
            Vector3 Point(float u)
            {
                var p = Vector3.Lerp(a, b, u);
                p.y -= sag * 4f * u * (1f - u);
                return p;
            }
            var mb = new ModelBuilder();
            var plank = MaterialLibrary.World(new Color(0.55f, 0.4f, 0.26f), 0.08f);
            var plankDark = MaterialLibrary.World(new Color(0.45f, 0.32f, 0.21f), 0.08f);
            var rope = MaterialLibrary.World(Pal.Rope, 0.05f);
            int planks = Mathf.CeilToInt(length / 0.42f);
            for (int i = 0; i <= planks; i++)
            {
                float u = i / (float)planks;
                var p = Point(u);
                var p2 = Point(Mathf.Min(1f, u + 0.01f));
                var fwd = (p2 - p).normalized;
                if (fwd.sqrMagnitude < 0.01f) fwd = flat;
                var rot = Quaternion.LookRotation(fwd, Vector3.up);
                mb.For(i % 3 == 0 ? plankDark : plank).Box(p, new Vector3(1.7f, 0.07f, 0.34f), rot * Quaternion.Euler(0f, 0f, (i * 37 % 5 - 2) * 0.6f));
            }
            // Cuerdas laterales (pasamanos) y tirantes
            int ropeSeg = 24;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < ropeSeg; i++)
                {
                    float u0 = i / (float)ropeSeg, u1 = (i + 1) / (float)ropeSeg;
                    var p0 = Point(u0) + right * side * 0.9f + Vector3.up * 1.05f;
                    var p1 = Point(u1) + right * side * 0.9f + Vector3.up * 1.05f;
                    var seg = p1 - p0;
                    mb.For(rope).Cylinder(p0, 0.035f, 0.035f, seg.magnitude, 4, Quaternion.FromToRotation(Vector3.up, seg.normalized), false, false);
                    var low = Point(u0) + right * side * 0.85f;
                    mb.For(rope).Cylinder(low, 0.02f, 0.02f, 1.05f, 3, Quaternion.identity, false, false);
                }
                // Postes en los extremos
                foreach (var end in new[] { a, b })
                {
                    var post = end + right * side * 1.0f;
                    mb.For(MaterialLibrary.World(Pal.WoodDark, 0.1f)).Cylinder(post + Vector3.down * 0.5f, 0.18f, 0.14f, 2.2f, 7, Quaternion.identity);
                }
            }
            mb.Build(t, "PuenteColgante");
            // Colisiones: tramos inclinados que siguen la curva + barandillas invisibles
            var cols = K.Child(t, "Colisiones");
            int segs = 14;
            for (int i = 0; i < segs; i++)
            {
                float u0 = i / (float)segs, u1 = (i + 1) / (float)segs;
                var p0 = Point(u0); var p1 = Point(u1);
                var mid = (p0 + p1) * 0.5f;
                var rot = Quaternion.LookRotation((p1 - p0).normalized, Vector3.up);
                float len = Vector3.Distance(p0, p1) + 0.12f;
                var floor = Colliders.Box(cols, mid + rot * Vector3.down * 0.06f, new Vector3(1.7f, 0.12f, len), rot, "Tablas");
                Tag(floor, SurfaceKind.Madera);
                Colliders.Box(cols, mid + right * 0.98f + Vector3.up * 0.8f, new Vector3(0.1f, 1.8f, len), rot, "Barandilla");
                Colliders.Box(cols, mid - right * 0.98f + Vector3.up * 0.8f, new Vector3(0.1f, 1.8f, len), rot, "Barandilla");
            }
            var sway = t.gameObject.AddComponent<BridgeSway>();
            sway.A = a; sway.B = b;
        }

        /// <summary>Taller de Eira y Daren: "una habitación, tres pistas y dos recuerdos".</summary>
        private static void StudyHut(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var stone = MaterialLibrary.World(Pal.Stone, 0.05f);
            var wood = MaterialLibrary.World(Pal.Wood, 0.1f);
            float w = 6f, d = 5f, h = 2.5f;
            mb.For(stone).Box(new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.35f), Quaternion.identity);
            mb.For(stone).Box(new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.35f, h, d), Quaternion.identity);
            mb.For(stone).Box(new Vector3(w * 0.5f, h * 0.4f, 0.4f), new Vector3(0.35f, h * 0.8f, d - 0.8f), Quaternion.identity);
            mb.For(stone).Box(new Vector3(-1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.35f), Quaternion.identity);
            mb.For(stone).Box(new Vector3(2.1f, h * 0.35f, d * 0.5f), new Vector3(1.8f, h * 0.7f, 0.35f), Quaternion.identity);
            Colliders.Box(cols, new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.4f));
            Colliders.Box(cols, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.4f, h, d));
            Colliders.Box(cols, new Vector3(w * 0.5f, h * 0.4f, 0.4f), new Vector3(0.4f, h * 0.8f, d - 0.8f));
            Colliders.Box(cols, new Vector3(-1.9f, h * 0.5f, d * 0.5f), new Vector3(2.2f, h, 0.4f));
            Colliders.Box(cols, new Vector3(2.1f, h * 0.35f, d * 0.5f), new Vector3(1.8f, h * 0.7f, 0.4f));
            // Tejado medio hundido
            for (int i = 0; i < 9; i++)
            {
                if (i == 5 || i == 6) continue;
                mb.For(i % 2 == 0 ? wood : MaterialLibrary.World(Pal.WoodDark, 0.1f)).Box(new Vector3(-2.7f + i * 0.68f, h + 0.25f, 0f), new Vector3(0.6f, 0.1f, d + 0.6f), Quaternion.Euler(0f, 0f, i == 7 ? -18f : 0f));
            }
            // Mesa (página de Eira) y estantería (gubia de Daren)
            var table = new Vector3(-1.3f, 0f, -1.1f);
            mb.For(MaterialLibrary.World(Pal.WoodLight, 0.12f)).Box(table + Vector3.up * 0.78f, new Vector3(1.6f, 0.08f, 0.9f), Quaternion.identity);
            for (int i = 0; i < 4; i++) mb.For(wood).Box(table + new Vector3(i % 2 == 0 ? -0.7f : 0.7f, 0.38f, i < 2 ? -0.38f : 0.38f), new Vector3(0.08f, 0.76f, 0.08f), Quaternion.identity);
            mb.For(MaterialLibrary.World(new Color(0.9f, 0.86f, 0.74f))).Box(table + new Vector3(0.4f, 0.83f, 0.1f), new Vector3(0.3f, 0.02f, 0.4f), Quaternion.Euler(0f, -12f, 0f));
            mb.For(MaterialLibrary.World(new Color(0.15f, 0.12f, 0.2f))).Cylinder(table + new Vector3(-0.5f, 0.82f, 0.2f), 0.05f, 0.04f, 0.1f, 6, Quaternion.identity);
            Colliders.Box(cols, table + Vector3.up * 0.4f, new Vector3(1.6f, 0.8f, 0.9f));
            var shelf = new Vector3(2.3f, 0f, -0.4f);
            mb.For(wood).Box(shelf + Vector3.up * 1f, new Vector3(0.4f, 2f, 1.6f), Quaternion.identity);
            for (int i = 0; i < 4; i++)
                mb.For(MaterialLibrary.Emissive(new Color(0.6f, 0.5f, 0.8f), Pal.Resonance, 0.4f)).Lathe(shelf + new Vector3(-0.05f, 1.42f, -0.55f + i * 0.35f), new[] { new Vector2(0.07f, 0f), new Vector2(0.08f, 0.16f), new Vector2(0.04f, 0.22f) }, 6, Quaternion.identity);
            // Dibujos de tiza en la pared del fondo
            var chalk = MaterialLibrary.Emissive(new Color(0.85f, 0.85f, 0.8f), new Color(0.15f, 0.15f, 0.15f), 1f);
            mb.For(chalk).Box(new Vector3(0.5f, 1.6f, -d * 0.5f + 0.19f), new Vector3(1.2f, 0.03f, 0.01f), Quaternion.Euler(0f, 0f, 20f));
            mb.For(chalk).Box(new Vector3(0.6f, 1.4f, -d * 0.5f + 0.19f), new Vector3(0.6f, 0.6f, 0.01f), Quaternion.Euler(0f, 0f, 45f));
            mb.For(MaterialLibrary.World(new Color(0.5f, 0.42f, 0.32f))).Box(new Vector3(1.4f, 0.08f, 1.3f), new Vector3(1.8f, 0.12f, 0.8f), Quaternion.Euler(0f, 20f, 0f));
            mb.Build(t, "TallerPadres");
        }

        /// <summary>Hondonada de la Cápsula sellada (estado inicial: anómala).</summary>
        private static void CapsuleHollow(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 6.2f, 0f, Mathf.Sin(a) * 6.2f);
                p.y = WorldShape.Height(t.position.x + p.x, t.position.z + p.z) - t.position.y;
                mb.For(Pal.StoneDark).Rock(p + Vector3.up * 0.5f, new Vector3(0.9f, 1.3f, 0.8f), Quaternion.identity, 400 + i, 0.2f);
                mb.For(Pal.Rope).Cylinder(p + Vector3.up * 1.0f, 0.03f, 0.03f, 3.9f, 3, Quaternion.Euler(0f, -(i * 36f) - 72f, 90f), false, false);
                mb.For(new Color(0.42f, 0.18f, 0.55f)).Box(p + new Vector3(0f, 0.7f, 0f), new Vector3(0.1f, 0.4f, 0.02f), Quaternion.identity);
            }
            // Fragmentos descartados (de ahí surge Miga)
            var frag = MaterialLibrary.Emissive(new Color(0.5f, 0.42f, 0.6f), Pal.Resonance, 0.4f);
            var rng = new OromRandom(31);
            for (int i = 0; i < 12; i++)
            {
                var p = new Vector3(rng.Range(-4f, 4f), 0f, rng.Range(-4.5f, -2f));
                p.y = WorldShape.Height(t.position.x + p.x, t.position.z + p.z) - t.position.y + 0.05f;
                mb.For(frag).Cylinder(p, 0.08f, 0f, rng.Range(0.15f, 0.35f), 4, Quaternion.Euler(rng.Range(-60f, 60f), rng.Range(0f, 360f), rng.Range(-60f, 60f)));
            }
            mb.Build(t, "Hondonada");
            var cap = new GameObject("CapsulaSellada");
            cap.transform.SetParent(t, false);
            cap.transform.localPosition = Vector3.zero;
            cap.AddComponent<CapsuleController>().Init("cap_sellada", true);
        }

        /// <summary>Gruta de los Nómadas: entrada baja (agacharse) tapada por enredaderas.</summary>
        private static void Grotto(Transform t)
        {
            var mb = new ModelBuilder();
            var cols = K.Child(t, "Colisiones");
            var rock = MaterialLibrary.World(new Color(0.42f, 0.4f, 0.37f), 0.05f);
            // Anillo de rocas (cámara de radio 4,5) con hueco hacia +Z para el túnel
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 20f) continue;
                var rot = Quaternion.Euler(0f, a, 0f);
                var p = rot * new Vector3(0f, 1.6f, 4.8f);
                mb.For(rock).Rock(p, new Vector3(3.2f, 3.6f, 1.8f), rot, 600 + i, 0.15f);
                Tag(Colliders.Box(cols, p, new Vector3(2.8f, 3.6f, 1.4f), rot, "Roca"), SurfaceKind.Piedra);
            }
            // Techo
            mb.For(rock).Rock(new Vector3(0f, 3.6f, 0f), new Vector3(11f, 1.6f, 11f), Quaternion.identity, 650, 0.12f);
            Colliders.Box(cols, new Vector3(0f, 3.4f, 0f), new Vector3(10f, 0.8f, 10f));
            // Túnel bajo (1,15 m): hay que agacharse
            for (int side = -1; side <= 1; side += 2)
            {
                var p = new Vector3(side * 1.25f, 0.9f, 6.2f);
                mb.For(rock).Rock(p, new Vector3(1.2f, 1.9f, 3.4f), Quaternion.identity, 660 + side, 0.12f);
                Colliders.Box(cols, p, new Vector3(0.9f, 1.9f, 3.4f));
            }
            mb.For(rock).Box(new Vector3(0f, 1.15f + 0.45f, 6.2f), new Vector3(2.6f, 0.9f, 3.4f), Quaternion.identity);
            Colliders.Box(cols, new Vector3(0f, 1.15f + 0.45f, 6.2f), new Vector3(2.6f, 0.9f, 3.4f));
            mb.For(MaterialLibrary.World(new Color(0.3f, 0.28f, 0.26f))).Cylinder(Vector3.zero, 4.3f, 4.3f, 0.04f, 14, Quaternion.identity);
            // Restos de un campamento antiguo
            mb.For(MaterialLibrary.World(new Color(0.12f, 0.1f, 0.09f))).Cylinder(new Vector3(1.2f, 0f, 0.5f), 0.6f, 0.6f, 0.04f, 10, Quaternion.identity);
            Props.Pot(mb, new Vector3(-1.5f, 0f, 1.5f), new Color(0.45f, 0.3f, 0.22f));
            mb.Build(t, "Gruta");
            // Cortina de enredaderas en la boca del túnel
            var vines = new GameObject("Enredaderas");
            vines.transform.SetParent(t, false);
            vines.transform.localPosition = new Vector3(0f, 0f, 7.7f);
            var vmb = new ModelBuilder();
            var leaf = MaterialLibrary.World(new Color(0.24f, 0.42f, 0.2f), 0.1f);
            var vr = new OromRandom(5);
            for (int i = 0; i < 10; i++)
            {
                float x = -1.1f + i * 0.24f;
                vmb.For(leaf).Box(new Vector3(x, 0.6f, 0f), new Vector3(0.07f, 1.2f, 0.05f), Quaternion.Euler(0f, 0f, vr.Range(-6f, 6f)));
                for (float y = 0.15f; y < 1.15f; y += 0.25f)
                    vmb.For(leaf).DoubleTri(new Vector3(x, y, 0f), new Vector3(x + 0.16f, y + 0.08f, 0.04f), new Vector3(x + 0.04f, y + 0.2f, 0.03f));
            }
            vmb.Build(vines.transform, "Enredaderas", false);
            var vc = vines.AddComponent<BoxCollider>();
            vc.center = new Vector3(0f, 0.6f, 0f);
            vc.size = new Vector3(2.4f, 1.2f, 0.3f);
            vines.AddComponent<VineCurtain>();
            LightSource.Create(K.Child(t, "LuzGruta", new Vector3(0f, 2f, 0f)), new Color(0.6f, 0.5f, 0.8f), 7f, 0.8f, false, 1);
        }

        private static void Waterfall(Transform t)
        {
            // Manantial que cae al abismo o al mar de nubes.
            var mb = new ModelBuilder();
            mb.For(Pal.StoneDark).Rock(Vector3.zero, new Vector3(3f, 1.4f, 2.6f), Quaternion.identity, 990, 0.2f);
            mb.Build(t, "Manantial");
            Vector3 outward;
            var p = t.position;
            if (WorldShape.PlateauDistance(p.x, p.z) > -10f && WorldShape.EdgeDistance(p.x, p.z) > 40f)
            {
                outward = new Vector3(p.x, 0f, p.z).normalized;
            }
            else outward = p.x < 100f ? Vector3.left : Vector3.back;
            var fall = K.Child(t, "Caida", outward * 2.5f + Vector3.up * 0.4f);
            FX.Waterfall(fall, Vector3.zero, 2.6f, 90f);
            AudioManager.AttachLoop(fall.gameObject, "agua", 0.6f, 28f);
        }

        private static void Lantern(Transform t)
        {
            var mb = new ModelBuilder();
            mb.For(Pal.WoodDark).Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.14f, 2.4f, 0.14f), Quaternion.identity);
            mb.For(Pal.WoodDark).Box(new Vector3(0.3f, 2.35f, 0f), new Vector3(0.7f, 0.1f, 0.1f), Quaternion.identity);
            mb.For(Pal.Rope).Cylinder(new Vector3(0.55f, 2f, 0f), 0.012f, 0.012f, 0.35f, 3, Quaternion.identity, false, false);
            mb.Build(t, "Farol");
            var lamp = K.Child(t, "Lampara", new Vector3(0.55f, 1.85f, 0f));
            var lmb = new ModelBuilder();
            lmb.For(MaterialLibrary.Emissive(Pal.Warm, Pal.Warm, 1.6f)).Lathe(Vector3.down * 0.15f, new[] { new Vector2(0.08f, 0f), new Vector2(0.14f, 0.12f), new Vector2(0.08f, 0.28f) }, 6, Quaternion.identity);
            lmb.Build(lamp, "Lampara", false);
            LightSource.Create(lamp, Pal.Warm, 9f, 1.3f, true, 1);
            Colliders.Box(K.Child(t, "Col"), new Vector3(0f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 0.2f));
        }
    }

    /// <summary>Balanceo sutil del puente colgante cuando Nilo lo cruza.</summary>
    public sealed class BridgeSway : MonoBehaviour
    {
        public Vector3 A, B;

        private void Update()
        {
            var cam = Game.Cam;
            if (cam == null || Game.Player == null) return;
            var p = Game.Player.transform.position;
            bool on = WorldShape.IsOnBridgeCorridor(p.x, p.z) || OromMath.DistanceToSegment(new P2(p.x, p.z), new P2(A.x, A.z), new P2(B.x, B.z)) < 1.2f;
            cam.BridgeSway = on ? 1f : 0f;
        }
    }

    /// <summary>Haro no deja cruzar el puente antes de la despedida de Gina.</summary>
    public sealed class BridgeBlocker : MonoBehaviour
    {
        private Collider _col;
        private float _barkCooldown;

        private void Start() => _col = GetComponent<Collider>();

        private void Update()
        {
            var s = Game.State;
            if (s == null || _col == null) return;
            bool open = s.Has("gina_despedida");
            if (_col.enabled == open) _col.enabled = !open;
            _barkCooldown -= Time.deltaTime;
            if (!open && Game.Player != null && _barkCooldown <= 0f && Vector3.Distance(Game.Player.transform.position, transform.position) < 3.2f)
            {
                _barkCooldown = 8f;
                string line = s.Has("haro_espada_dada")
                    ? "Despídete antes de tu abuela, chico. Te espera en la puerta."
                    : "El puente no es un camino, Nilo: es una frontera. Habla primero con los tuyos.";
                if (s.AtStep("mq_silencio", "entrenar")) line = "Primero el muñeco. Tres golpes, uno fuerte, y un bloqueo.";
                Game.UI?.Subtitle("Haro", line, 4f);
            }
        }
    }
}
