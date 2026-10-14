using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    public sealed class LocationDef
    {
        public string Id;
        public string Name;
        public P2 Pos;
        public float Radius = 12f;
        public int Xp = 20;
        public string Text;
        public bool Landmark;
        public LocationDef(string id, string name, float x, float z, float radius, string text, bool landmark = false, int xp = 20)
        {
            Id = id; Name = name; Pos = new P2(x, z); Radius = radius; Text = text; Landmark = landmark; Xp = xp;
        }
    }

    /// <summary>Lugar lejano visible en el horizonte (fuera del prototipo). Se registra en el Atlas al observarlo con el catalejo.</summary>
    public sealed class VistaDef
    {
        public string Id;
        public string Name;
        public P2 Pos;
        public float Elevation;
        public string Text;
        public VistaDef(string id, string name, float x, float z, float elevation, string text)
        {
            Id = id; Name = name; Pos = new P2(x, z); Elevation = elevation; Text = text;
        }
    }

    public sealed class StructureDef
    {
        public string Kind;
        public P2 Pos;
        public float Rot;
        public string Param;
        public StructureDef(string kind, float x, float z, float rot = 0f, string param = null)
        {
            Kind = kind; Pos = new P2(x, z); Rot = rot; Param = param;
        }
    }

    public sealed class NavNodeDef
    {
        public string Id;
        public P2 Pos;
        public NavNodeDef(string id, P2 pos) { Id = id; Pos = pos; }
    }

    public sealed class SpawnDef
    {
        public string Id;
        public string Kind;
        public P2 Pos;
        public int Count = 1;
        public float Radius = 6f;
        public bool Unique;
        public bool NightOnly;
        public SpawnDef(string id, string kind, float x, float z, int count = 1, float radius = 6f, bool unique = false, bool nightOnly = false)
        {
            Id = id; Kind = kind; Pos = new P2(x, z); Count = count; Radius = radius; Unique = unique; NightOnly = nightOnly;
        }
    }

    public sealed class PickupDef
    {
        public string Id;
        public string ItemId;
        public int Count = 1;
        public P2 Pos;
        public float YOffset;
        public PickupDef(string id, string itemId, float x, float z, float yOffset = 0f, int count = 1)
        {
            Id = id; ItemId = itemId; Pos = new P2(x, z); YOffset = yOffset; Count = count;
        }
    }

    public sealed class ChestDef
    {
        public string Id;
        public string Name;
        public P2 Pos;
        public float Rot;
        public List<ItemAmount> Contents = new List<ItemAmount>();
        public ChestDef(string id, string name, float x, float z, float rot, params ItemAmount[] items)
        {
            Id = id; Name = name; Pos = new P2(x, z); Rot = rot; Contents.AddRange(items);
        }
    }

    public sealed class RestPointDef
    {
        public string Id;
        public string Name;
        public P2 Pos;
        public float YOffset;
        public string Kind;
        public RestPointDef(string id, string name, float x, float z, string kind, float yOffset = 0f)
        {
            Id = id; Name = name; Pos = new P2(x, z); Kind = kind; YOffset = yOffset;
        }
    }

    public sealed class ResonanceSpotDef
    {
        public string ResonanceId;
        public P2 Pos;
        public float YOffset = 1f;
        public float Radius = 2.6f;
        public Func<GameState, bool> Active;
        public ResonanceSpotDef(string res, float x, float z, float yOffset = 1f, float radius = 2.6f, Func<GameState, bool> active = null)
        {
            ResonanceId = res; Pos = new P2(x, z); YOffset = yOffset; Radius = radius; Active = active;
        }
        public ResonanceSpotDef(string res, P2 p, float yOffset = 1f, float radius = 2.6f, Func<GameState, bool> active = null)
            : this(res, p.X, p.Z, yOffset, radius, active) { }
    }

    public sealed class ExamineDef
    {
        public string Id;
        public string Title;
        public string Text;
        public P2 Pos;
        public float YOffset = 0.9f;
        public string Prompt = "Examinar";
        public string Visual;
        public Func<GameState, bool> Active;
        public Action<IGameContext> OnExamine;
        /// <summary>Si se examina una sola vez desaparece (p. ej. recoger una pista).</summary>
        public bool ConsumeOnce;
    }

    public sealed class ClimbDef
    {
        public P2 Pos;
        public float Rot;
        public float Bottom;
        /// <summary>Altura de la cima medida desde el suelo de <see cref="TopRef"/> (centro de la estructura).</summary>
        public float Height;
        public P2 TopRef;
        public ClimbDef(float x, float z, float rot, float bottom, float height, P2 topRef)
        {
            Pos = new P2(x, z); Rot = rot; Bottom = bottom; Height = height; TopRef = topRef;
        }
    }

    /// <summary>
    /// Disposición del prototipo: lugares, estructuras, rutas de NPC, criaturas, objetos y puntos de Resonancia.
    /// Todo está fijado con coordenadas en metros para que el mundo sea idéntico entre partidas y guardados.
    /// </summary>
    public static class WorldLayout
    {
        public static readonly P2 Plaza = new P2(165f, 165f);
        public static readonly P2 GinaHouse = new P2(136.8f, 175.3f);
        public static readonly P2 SuriHouse = new P2(135.5f, 159.8f);
        public static readonly P2 OrenHouse = new P2(154.7f, 136.8f);
        public static readonly P2 HaroHouse = new P2(172.8f, 136f);
        public static readonly P2 TomeHouse = new P2(186.2f, 143.8f);
        public static readonly P2 LiaHouse = new P2(159.8f, 194.5f);
        public static readonly P2 Workshop = new P2(197.8f, 156.2f);
        public static readonly P2 Storehouse = new P2(196.9f, 173.5f);
        public static readonly P2 Fields = new P2(112f, 215f);
        public static readonly P2 GiantTree = new P2(215f, 240f);
        public static readonly P2 MigrationStones = new P2(110f, 110f);
        public static readonly P2 Gate = new P2(232f, 232f);
        public static readonly P2 GuardPost = new P2(266f, 266f);
        public static readonly P2 TrainingDummy = new P2(258f, 274f);
        public static readonly P2 StudyHut = new P2(108f, 280f);
        public static readonly P2 SealedCapsule = new P2(92f, 291f);
        public static readonly P2 Grotto = new P2(64f, 205f);
        public static readonly P2 Mojon = new P2(335f, 345f);
        public static readonly P2 BoroCamp = new P2(470f, 575f);
        public static readonly P2 StoneArch = new P2(650f, 700f);
        public const float StoneArchRot = 35f;
        public const float StoneArchHeight = 9.2f;
        public static readonly P2 Ruins = new P2(700f, 760f);
        public static readonly P2 RuinShelter = new P2(668f, 795f);
        public static readonly P2 Lookout = new P2(600f, 830f);
        public const float LookoutHeight = 11f;
        public static readonly P2 CloudHill = new P2(780f, 420f);
        public static readonly P2 KolotapirCamp = new P2(840f, 340f);
        public static readonly P2 EastGate = new P2(962f, 600f);
        public static readonly P2 Islet = new P2(520f, 515f);

        public static readonly float StudyHutRot = FacingDeg(StudyHut, new P2(130, 262));
        public static readonly float ShelterRot = FacingDeg(RuinShelter, Ruins);
        public const float GrottoRot = 90f;
        public const float BoroCampRot = 140f;

        public static float FacingDeg(P2 from, P2 to) => OromMath.HeadingDeg(to.X - from.X, to.Z - from.Z);

        /// <summary>
        /// Convierte coordenadas locales (lx = derecha, lz = frente) de una estructura con rumbo <paramref name="rotDeg"/>
        /// a coordenadas del mundo. Mismo convenio que Quaternion.Euler(0, rot, 0) en Unity.
        /// </summary>
        public static P2 LocalToWorld(P2 origin, float rotDeg, float lx, float lz)
        {
            double r = rotDeg * OromMath.Deg2Rad;
            float c = (float)Math.Cos(r), sn = (float)Math.Sin(r);
            return new P2(origin.X + lx * c + lz * sn, origin.Z - lx * sn + lz * c);
        }

        /// <summary>Columnas de la Parada Antigua (anillo de 10). La 6 lleva las marcas del Reverso.</summary>
        public static P2 RuinPillar(int i)
        {
            double a = i * 36.0 * OromMath.Deg2Rad;
            return new P2(Ruins.X + 14f * (float)Math.Cos(a), Ruins.Z + 14f * (float)Math.Sin(a));
        }

        public const int MarkedPillar = 6;

        // Mobiliario con posición compartida entre el contenido (Core) y la geometría (Runtime).
        public static P2 HutTable => LocalToWorld(StudyHut, StudyHutRot, -1.3f, -1.1f);
        public static P2 HutShelf => LocalToWorld(StudyHut, StudyHutRot, 2.3f, -0.4f);
        public static P2 ShelterCarving => LocalToWorld(RuinShelter, ShelterRot, 0f, -1.45f);
        public static P2 ArchClimb => LocalToWorld(StoneArch, StoneArchRot, -4.5f, 1.05f);
        public static P2 ArchInscription => LocalToWorld(StoneArch, StoneArchRot, 4.5f, 1.3f);
        public static P2 LookoutClimb => new P2(Lookout.X, Lookout.Z - 3.75f);
        public static P2 BoroCart => LocalToWorld(BoroCamp, BoroCampRot, 3.2f, 1.8f);
        public static P2 GrottoChest => LocalToWorld(Grotto, GrottoRot, 0f, -2.3f);
        public static P2 GrottoVines => LocalToWorld(Grotto, GrottoRot, 0f, 7.6f);
        public static P2 GrottoPainting => LocalToWorld(Grotto, GrottoRot, 2.6f, -1.2f);

        public static P2 DoorPoint(P2 house, P2 toward, float distance = 5.6f)
        {
            var d = toward - house;
            float len = d.Length;
            return len < 0.01f ? house : house + d * (distance / len);
        }

        // ------------------------------------------------------------------ Lugares (Atlas)

        public static readonly List<LocationDef> Locations = new List<LocationDef>
        {
            new LocationDef("loc_reverso", "Tribu del Reverso", 165, 165, 55, "Asentamiento estable de la tribu. Hogares de piedra y madera, campos, almacén y el fuego comunal. Los mayores dicen que sus antepasados caminaron durante generaciones antes de detenerse aquí.", true, 0),
            new LocationDef("loc_arbol", "Árbol de los Mayores", 215, 240, 22, "Un árbol inmenso. Los niños aprenden aquí las canciones de la migración. Sus raíces parecen rodear algo enterrado.", true),
            new LocationDef("loc_piedras", "Piedras de la Migración", 110, 110, 15, "Cada piedra recuerda una parada del viaje de los antepasados. Faltan piedras: alguien dejó huecos a propósito."),
            new LocationDef("loc_hondonada", "Hondonada de la Cápsula sellada", 92, 291, 18, "Una depresión rodeada de cuerdas y amuletos. La tribu prohíbe acercarse a la Cápsula que duerme aquí."),
            new LocationDef("loc_taller", "Taller de Eira y Daren", 108, 280, 9, "La cabaña donde los padres de Nilo estudiaban la Cápsula. Nadie la ha ordenado desde que se fueron."),
            new LocationDef("loc_gruta", "Gruta de los Nómadas", 64, 205, 9, "Una cueva baja escondida tras enredaderas. Huele a ceniza antigua."),
            new LocationDef("loc_puesto", "Puesto del puente", 266, 266, 12, "Haro vigila aquí el único paso hacia el exterior."),
            new LocationDef("loc_puente", "Puente colgante", 298, 298, 10, "El límite del territorio del Reverso. Abajo solo hay nubes."),
            new LocationDef("loc_mojon", "Mojón del puente", 335, 345, 12, "Un montón de piedras con un pequeño refugio. Los viajeros dejan aquí lo que les sobra."),
            new LocationDef("loc_lago", "Lago de Boro", 520, 515, 60, "Aguas tranquilas con un islote en el centro. Hay quien jura que el agua repite las conversaciones de la orilla."),
            new LocationDef("loc_boro", "Campamento de Boro", 470, 575, 15, "Un carro lleno de objetos 'inútiles', una tienda remendada y un fuego que nunca se apaga del todo."),
            new LocationDef("loc_arco", "Arco de piedra", 650, 700, 16, "Un arco natural tallado por manos antiguas. Hay una inscripción en lengua kolotapir en la base.", true),
            new LocationDef("loc_parada", "La Parada Antigua", 700, 760, 32, "Ruinas de una parada de viajeros. En el centro duerme una Cápsula dañada.", true, 30),
            new LocationDef("loc_refugio", "Refugio de la Parada", 668, 795, 9, "Un refugio de piedra todavía en pie. Alguien grabó algo en la pared."),
            new LocationDef("loc_mirador", "Peña del Mirador", 600, 830, 8, "Desde lo alto se ven el Bosque del Revés y, muy lejos, el Umbral Dormido.", true, 30),
            new LocationDef("loc_colina", "Colina de las Nubes", 780, 420, 26, "El mejor lugar de los Prados para mirar el cielo. Y para discutir sobre él.", true),
            new LocationDef("loc_bc", "Campamento de Bruma y Copo", 840, 340, 12, "Dos esterillas, una olla y demasiadas opiniones."),
            new LocationDef("loc_este", "Camino del Este", 962, 600, 24, "Un puente natural se adentra en la bruma hacia la Ciudad de las Mil Cápsulas.", true, 30),
            new LocationDef("loc_guarida_oeste", "Guarida del jabalí de musgo", 420, 760, 16, "Territorio marcado con zarpazos. Mejor no quedarse mucho tiempo."),
            new LocationDef("loc_guarida_este", "Guarida del jabalí del este", 880, 760, 16, "Otra guarida. Un jabalí de musgo vigila su comida."),
        };

        public static readonly List<VistaDef> Vistas = new List<VistaDef>
        {
            new VistaDef("vista_ciudad", "Ciudad de las Mil Cápsulas", 1750, 900, 60, "Cúpulas, torres y puentes que brillan al atardecer. Centro social y cultural de Orom, donde viven la Liga y sus archivos."),
            new VistaDef("vista_bosque", "Bosque del Revés", 450, 1600, 80, "Árboles gigantes, algunos con las raíces hacia el cielo. Dicen que allí los caminos recuerdan los pasos."),
            new VistaDef("vista_gargantas", "Gargantas Silentes", 1300, -260, 30, "Cañones rojos y puentes antiguos. Una propuesta de la lámina del mapa todavía por confirmar."),
            new VistaDef("vista_umbral", "Umbral Dormido", 1250, 2350, 320, "Rocas flotantes de luz violeta en el extremo norte. El origen de todo, según las canciones."),
            new VistaDef("vista_mar", "Mar de las Cosas No Vividas", 2300, 1750, 120, "Islas que flotan sobre aguas de recuerdos posibles."),
            new VistaDef("vista_valle", "Valle de las Cataratas", 1650, 250, 40, "Grandes saltos de agua y santuarios. Su lugar en el mapa aún no está decidido."),
        };

        // ------------------------------------------------------------------ Estructuras

        public static readonly List<StructureDef> Structures = BuildStructures();

        private static List<StructureDef> BuildStructures()
        {
            var s = new List<StructureDef>
            {
                new StructureDef("house", GinaHouse.X, GinaHouse.Z, FacingDeg(GinaHouse, Plaza), "gina"),
                new StructureDef("house", SuriHouse.X, SuriHouse.Z, FacingDeg(SuriHouse, Plaza), "suri"),
                new StructureDef("house", OrenHouse.X, OrenHouse.Z, FacingDeg(OrenHouse, Plaza), "oren"),
                new StructureDef("house", HaroHouse.X, HaroHouse.Z, FacingDeg(HaroHouse, Plaza), "haro"),
                new StructureDef("house", TomeHouse.X, TomeHouse.Z, FacingDeg(TomeHouse, Plaza), "tome"),
                new StructureDef("house", LiaHouse.X, LiaHouse.Z, FacingDeg(LiaHouse, Plaza), "lia"),
                new StructureDef("workshop", Workshop.X, Workshop.Z, FacingDeg(Workshop, Plaza)),
                new StructureDef("storehouse", Storehouse.X, Storehouse.Z, FacingDeg(Storehouse, Plaza)),
                new StructureDef("plaza", Plaza.X, Plaza.Z),
                new StructureDef("fields", Fields.X, Fields.Z, 40f),
                new StructureDef("giant_tree", GiantTree.X, GiantTree.Z),
                new StructureDef("migration_stones", MigrationStones.X, MigrationStones.Z),
                new StructureDef("gate", Gate.X, Gate.Z, 45f),
                new StructureDef("guard_post", GuardPost.X, GuardPost.Z, 45f),
                new StructureDef("training_dummy", TrainingDummy.X, TrainingDummy.Z, 200f),
                new StructureDef("rope_bridge", WorldShape.BridgeWest.X, WorldShape.BridgeWest.Z, 45f),
                new StructureDef("study_hut", StudyHut.X, StudyHut.Z, FacingDeg(StudyHut, new P2(130, 262))),
                new StructureDef("capsule_hollow", SealedCapsule.X, SealedCapsule.Z),
                new StructureDef("grotto", Grotto.X, Grotto.Z, 90f),
                new StructureDef("waterfall", 318, 232, 0f),
                new StructureDef("waterfall", 232, 318, 0f),
                new StructureDef("waterfall", 30, 150, 0f),
                new StructureDef("mojon", Mojon.X, Mojon.Z, 225f),
                new StructureDef("boro_camp", BoroCamp.X, BoroCamp.Z, 140f),
                new StructureDef("stone_arch", StoneArch.X, StoneArch.Z, StoneArchRot),
                new StructureDef("ruins", Ruins.X, Ruins.Z),
                new StructureDef("ruin_shelter", RuinShelter.X, RuinShelter.Z, FacingDeg(RuinShelter, Ruins)),
                new StructureDef("lookout", Lookout.X, Lookout.Z, 180f),
                new StructureDef("cloud_rock", CloudHill.X, CloudHill.Z),
                new StructureDef("kolotapir_camp", KolotapirCamp.X, KolotapirCamp.Z, 300f),
                new StructureDef("east_gate", EastGate.X, EastGate.Z, 90f),
                new StructureDef("boar_den", 420, 760, 30f),
                new StructureDef("boar_den", 880, 760, 200f),
                new StructureDef("islet", Islet.X, Islet.Z),
            };
            // Faroles a lo largo de los caminos de la tribu (iluminan de noche).
            float[,] lanterns =
            {
                { 178, 178 }, { 205, 205 }, { 226, 226 }, { 252, 252 }, { 150, 150 }, { 140, 190 }, { 122, 205 }, { 172, 150 },
                { 104, 262 }, { 278, 278 }, { 330, 340 }, { 476, 562 }, { 690, 748 },
            };
            for (int i = 0; i < lanterns.GetLength(0); i++)
                s.Add(new StructureDef("lantern", lanterns[i, 0] + 2.2f, lanterns[i, 1] - 2.2f, 0f));
            return s;
        }

        // ------------------------------------------------------------------ Navegación de NPC

        public static readonly List<NavNodeDef> Nodes = new List<NavNodeDef>();
        public static readonly List<KeyValuePair<string, string>> Edges = new List<KeyValuePair<string, string>>();

        private static void N(string id, float x, float z) => Nodes.Add(new NavNodeDef(id, new P2(x, z)));
        private static void N(string id, P2 p) => Nodes.Add(new NavNodeDef(id, p));
        private static void E(string a, string b) => Edges.Add(new KeyValuePair<string, string>(a, b));

        static WorldLayout()
        {
            // Plaza y asientos alrededor del fuego.
            N("plaza", Plaza);
            N("plaza_n", 165, 171.5f); N("plaza_s", 165, 158.5f); N("plaza_e", 171.5f, 165); N("plaza_w", 158.5f, 165);
            N("plaza_ne", 170, 170); N("plaza_sw", 160, 160); N("plaza_cook", 169.5f, 160.5f);
            foreach (var seat in new[] { "plaza_n", "plaza_s", "plaza_e", "plaza_w", "plaza_ne", "plaza_sw", "plaza_cook" }) E("plaza", seat);
            E("plaza_n", "plaza_ne"); E("plaza_ne", "plaza_e"); E("plaza_e", "plaza_cook"); E("plaza_cook", "plaza_s");
            E("plaza_s", "plaza_sw"); E("plaza_sw", "plaza_w"); E("plaza_w", "plaza_n");

            // Casas: puerta (exterior) e interior.
            AddHouse("gina", GinaHouse, "plaza_w");
            AddHouse("suri", SuriHouse, "plaza_sw");
            AddHouse("oren", OrenHouse, "plaza_sw");
            AddHouse("haro", HaroHouse, "plaza_s");
            AddHouse("tome", TomeHouse, "plaza_cook");
            AddHouse("lia", LiaHouse, "plaza_n");
            N("gina_garden", 127, 186); E("gina_garden", "gina_door");
            N("gina_porch", DoorPoint(GinaHouse, Plaza, 4.6f) + new P2(1.6f, -1.6f)); E("gina_porch", "gina_door");

            N("workshop", DoorPoint(Workshop, Plaza, 1.0f)); N("workshop_front", DoorPoint(Workshop, Plaza, 5f));
            E("workshop", "workshop_front"); E("workshop_front", "plaza_e");
            N("store", DoorPoint(Storehouse, Plaza, 1.5f)); N("store_front", DoorPoint(Storehouse, Plaza, 6f));
            E("store", "store_front"); E("store_front", "plaza_e"); E("store_front", "plaza_ne");

            N("path_ne1", 186, 186); N("path_ne2", 205, 205); N("gate_in", 226, 226); N("gate", Gate); N("gate_out", 240, 240);
            E("plaza_ne", "path_ne1"); E("path_ne1", "path_ne2"); E("path_ne2", "gate_in"); E("gate_in", "gate"); E("gate", "gate_out");
            E("path_ne1", "store_front");
            N("tree_seat", 211, 229); N("tree_path", 211, 216); E("path_ne2", "tree_path"); E("tree_path", "tree_seat");
            N("guard", 264, 264); N("guard_bridge", 278, 278); N("guard_dummy", 259, 268);
            E("gate_out", "guard"); E("guard", "guard_bridge"); E("guard", "guard_dummy");

            N("fields_path", 140, 190); N("fields", 116, 212); N("fields_2", 104, 222); N("fields_3", 120, 224);
            E("plaza_w", "fields_path"); E("fields_path", "fields"); E("fields", "fields_2"); E("fields", "fields_3"); E("fields_2", "fields_3");
            E("fields_path", "gina_garden");
            N("north_path", 104, 250); E("fields_2", "north_path"); N("hut_front", 112, 270); E("north_path", "hut_front");

            N("stones_path", 140, 140); N("stones", 114, 114); N("stones_2", 106, 118);
            E("plaza_sw", "stones_path"); E("stones_path", "stones"); E("stones", "stones_2");
            E("stones_path", "oren_door");

            N("kiko_play1", 182, 162); N("kiko_play2", 150, 182); N("kiko_play3", 178, 192);
            E("plaza_e", "kiko_play1"); E("plaza_n", "kiko_play2"); E("plaza_n", "kiko_play3"); E("kiko_play3", "path_ne1");

            // Prados.
            N("bridge_e", 318, 318); N("mojon", 333, 340); N("m1", 390, 410); N("m2", 430, 500);
            N("boro_camp", 474, 568); N("boro_fire", 470, 571); N("boro_tent", 462, 582); N("boro_cart", 479, 580);
            N("lake_n", 492, 556); N("lake_w", 462, 520);
            E("bridge_e", "mojon"); E("mojon", "m1"); E("m1", "m2"); E("m2", "boro_camp"); E("boro_camp", "boro_fire");
            E("boro_camp", "boro_tent"); E("boro_camp", "boro_cart"); E("boro_camp", "lake_n"); E("m2", "lake_w"); E("lake_w", "lake_n");
            N("m3", 520, 420); N("m4", 640, 430); N("hill_base", 735, 420); N("hill_top", 776, 418); N("hill_top2", 782, 424);
            N("herbs", 762, 372); N("herbs2", 748, 386); N("bc_camp", 836, 344); N("bc_fire", 840, 339); N("bc_beds", 846, 346);
            E("m1", "m3"); E("m3", "m4"); E("m4", "hill_base"); E("hill_base", "hill_top"); E("hill_top", "hill_top2");
            E("hill_base", "herbs"); E("herbs", "herbs2"); E("herbs", "bc_camp"); E("bc_camp", "bc_fire"); E("bc_camp", "bc_beds");
            N("m5", 560, 640); N("arch", 646, 694); N("ruins_edge", 684, 738); N("ruins_n", 712, 790); N("m6", 700, 600); N("m7", 860, 600);
            E("boro_camp", "m5"); E("m5", "arch"); E("arch", "ruins_edge"); E("ruins_edge", "ruins_n"); E("m5", "m6"); E("m6", "m7"); E("m4", "m6");
            E("lake_n", "m5");
        }

        private static void AddHouse(string id, P2 house, string plazaNode)
        {
            N(id + "_in", DoorPoint(house, Plaza, 1.2f));
            N(id + "_bed", house + (house - Plaza) * (1.8f / P2.Distance(house, Plaza)));
            N(id + "_door", DoorPoint(house, Plaza, 5.8f));
            E(id + "_in", id + "_door");
            E(id + "_in", id + "_bed");
            E(id + "_door", plazaNode);
        }

        public static NavNodeDef FindNode(string id)
        {
            for (int i = 0; i < Nodes.Count; i++) if (Nodes[i].Id == id) return Nodes[i];
            return null;
        }

        // ------------------------------------------------------------------ Criaturas

        public static readonly List<SpawnDef> Spawns = new List<SpawnDef>
        {
            new SpawnDef("eco_parada_a", "eco", 688, 700, 2, 7f),
            new SpawnDef("eco_parada_b", "eco", 748, 805, 2, 7f),
            new SpawnDef("eco_parada_c", "eco", 640, 785, 2, 6f),
            new SpawnDef("eco_noche_a", "eco", 405, 655, 3, 10f, false, true),
            new SpawnDef("eco_noche_b", "eco", 610, 470, 2, 8f, false, true),
            new SpawnDef("guardian_parada", "guardian", 700, 760, 1, 0f, true),
            new SpawnDef("jabali_oeste", "jabali", 420, 760, 1, 0f),
            new SpawnDef("jabali_este", "jabali", 880, 760, 1, 0f),
            new SpawnDef("corzos", "corzo", 600, 380, 5, 14f),
        };

        // ------------------------------------------------------------------ Objetos únicos, cofres y descanso

        public static readonly List<PickupDef> Pickups = new List<PickupDef>
        {
            new PickupDef("p_cuchara", "cuchara_doblada", StoneArch.X, StoneArch.Z, StoneArchHeight + 0.15f),
            new PickupDef("p_boton", "boton_nacar", 768, 446, 0.05f),
            new PickupDef("p_fragmento_mirador", "fragmento", Lookout.X + 1.2f, Lookout.Z - 0.6f, LookoutHeight + 0.1f, 2),
            new PickupDef("p_bayas_gina", "bayas", 128, 190, 0.6f, 2),
            new PickupDef("p_resina_arbol", "resina", 222, 233, 0.3f, 2),
        };

        public static readonly List<ChestDef> Chests = new List<ChestDef>
        {
            new ChestDef("c_mojon", "Caja del mojón", 338, 348, 225, new ItemAmount("venda", 2), new ItemAmount("bayas", 3)),
            new ChestDef("c_islote", "Cofre del islote", Islet.X + 1.5f, Islet.Z + 1f, 200, new ItemAmount("piedra_agujero", 1), new ItemAmount("resina", 2)),
            new ChestDef("c_refugio", "Arcón del refugio", LocalToWorld(RuinShelter, ShelterRot, -1.2f, -0.6f).X, LocalToWorld(RuinShelter, ShelterRot, -1.2f, -0.6f).Z, ShelterRot, new ItemAmount("capucha_explorador", 1), new ItemAmount("venda", 2), new ItemAmount("unguento", 1)),
            new ChestDef("c_gruta", "Arcón de los nómadas", GrottoChest.X, GrottoChest.Z, GrottoRot, new ItemAmount("peto_guardian", 1), new ItemAmount("pan_raiz", 2)),
        };

        public static readonly List<RestPointDef> RestPoints = new List<RestPointDef>
        {
            new RestPointDef("r_casa_gina", "Casa de la abuela Gina", GinaHouse.X, GinaHouse.Z, "bed"),
            new RestPointDef("r_plaza", "Fuego comunal", Plaza.X, Plaza.Z, "fire"),
            new RestPointDef("r_mojon", "Mojón del puente", Mojon.X, Mojon.Z, "fire"),
            new RestPointDef("r_boro", "Fuego de Boro", BoroCamp.X, BoroCamp.Z, "fire"),
            new RestPointDef("r_refugio", "Refugio de la Parada", RuinShelter.X, RuinShelter.Z, "shelter"),
            new RestPointDef("r_bc", "Fuego de Bruma y Copo", KolotapirCamp.X, KolotapirCamp.Z, "fire"),
        };

        public static RestPointDef FindRestPoint(string id)
        {
            for (int i = 0; i < RestPoints.Count; i++) if (RestPoints[i].Id == id) return RestPoints[i];
            return null;
        }

        // ------------------------------------------------------------------ Resonancias en el mundo

        public static readonly List<ResonanceSpotDef> ResonanceSpots = new List<ResonanceSpotDef>
        {
            new ResonanceSpotDef("res_recuerdo_daren", HutTable, 1.0f, 2.2f),
            new ResonanceSpotDef("res_sello", LocalToWorld(SealedCapsule, 0f, 0f, -3.4f), 1.1f, 2.4f, s => s.Heard("res_huellas_capsula")),
            new ResonanceSpotDef("res_boro_cuenta", BoroCart, 1.1f, 1.8f, s => !s.HasItem("cuenta_boro") && !s.Heard("res_boro_cuenta")),
            new ResonanceSpotDef("res_piedras", MigrationStones.X, MigrationStones.Z, 1.2f, 4f),
            new ResonanceSpotDef("res_arbol", GiantTree.X - 3f, GiantTree.Z - 6.5f, 0.8f, 3f),
            new ResonanceSpotDef("res_rastro_prados", 328, 334, 0.6f, 3.5f),
            new ResonanceSpotDef("res_guardian_1", 678, 742, 1.0f, 3f, s => !s.Has("guardian_resuelto")),
            new ResonanceSpotDef("res_guardian_2", 726, 772, 1.0f, 3f, s => !s.Has("guardian_resuelto")),
            new ResonanceSpotDef("res_guardian_3", 704, 789, 1.0f, 3f, s => !s.Has("guardian_resuelto")),
            new ResonanceSpotDef("res_talla_refugio", ShelterCarving, 1.4f, 2.2f, s => s.Has("recuerdo_reconstruido")),
            new ResonanceSpotDef("res_prados_risa", 448, 452, 1.2f, 3f),
            new ResonanceSpotDef("res_prados_cancion", 604, 562, 1.2f, 3f),
            new ResonanceSpotDef("res_prados_nube", 756, 516, 1.2f, 3f),
            new ResonanceSpotDef("res_prados_caravana", 860, 606, 1.2f, 3f),
            new ResonanceSpotDef("res_mirador", Lookout.X - 1f, Lookout.Z + 1f, LookoutHeight + 1f, 2.2f),
        };

        public static readonly List<ClimbDef> Climbs = new List<ClimbDef>
        {
            new ClimbDef(ArchClimb.X, ArchClimb.Z, StoneArchRot, 0f, StoneArchHeight, StoneArch),
            new ClimbDef(LookoutClimb.X, LookoutClimb.Z, 180f, 0f, LookoutHeight, Lookout),
        };

        /// <summary>Rastros de huellas que solo se ven con la Escucha de Resonancias.</summary>
        public static readonly P2[][] Trails =
        {
            new[] { new P2(94, 287), new P2(130, 285), new P2(175, 276), new P2(215, 262), new P2(250, 262), new P2(270, 270), new P2(283, 283) },
            new[] { new P2(317, 317), new P2(335, 345), new P2(380, 402), new P2(412, 470), new P2(445, 540), new P2(468, 566), new P2(520, 610), new P2(600, 660), new P2(648, 698), new P2(688, 745) },
        };
    }
}
