using System.Collections.Generic;

namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private static void AddItems()
        {
            // ------------------------------------------------ Consumibles (curación inicial: vendas, ungüentos y descanso)
            Reg(new ItemDef
            {
                Id = "venda", Name = "Venda de lino", Category = ItemCategory.Consumible, MaxStack = 10, Value = 4,
                Description = "Curación moderada para expediciones. Vendarse lleva un momento y te deja vulnerable.",
                HealAmount = 25f, HealDuration = 1.5f, Color = "#E8E0CF", Glyph = "✚", Origin = "Tejida en el Reverso",
            });
            Reg(new ItemDef
            {
                Id = "unguento", Name = "Ungüento de raíz", Category = ItemCategory.Consumible, MaxStack = 5, Value = 8,
                Description = "Pasta espesa de hierba de sanación y resina. Cura más, pero tarda más en aplicarse.",
                HealAmount = 45f, HealDuration = 2.5f, Color = "#7E9C5A", Glyph = "✚", Origin = "Receta del Reverso",
            });
            Reg(new ItemDef
            {
                Id = "tonico_bruma", Name = "Tónico de bruma", Category = ItemCategory.Consumible, MaxStack = 5, Value = 14,
                Description = "Preparado kolotapir que sabe a niebla. Recuperación rápida a cambio de un recurso escaso.",
                HealAmount = 35f, HealDuration = 0.5f, Color = "#9FC6D8", Glyph = "◆", Origin = "Trueque con Boro",
            });
            Reg(new ItemDef
            {
                Id = "pan_raiz", Name = "Pan de raíz", Category = ItemCategory.Consumible, MaxStack = 10, Value = 3, IsFood = true,
                Description = "Pan denso y dulzón. Recuperación gradual.", RegenPerSecond = 1.5f, RegenDuration = 20f,
                Color = "#B88A4E", Glyph = "●", Origin = "Horno de Suri",
            });
            Reg(new ItemDef
            {
                Id = "estofado", Name = "Estofado de Suri", Category = ItemCategory.Consumible, MaxStack = 5, Value = 6, IsFood = true,
                Description = "Recuperación gradual y más aguante durante un rato. Suri jura que la receta tiene doce generaciones.",
                RegenPerSecond = 2.5f, RegenDuration = 24f, StaminaRegenBuff = 0.15f, Color = "#A0522D", Glyph = "●", Origin = "Cocina comunal",
            });
            Reg(new ItemDef
            {
                Id = "bayas", Name = "Bayas dulces", Category = ItemCategory.Consumible, MaxStack = 15, Value = 2, IsFood = true,
                Description = "Un bocado rápido. A los jabalíes de musgo les encantan: ofrecerlas puede calmarlos.",
                RegenPerSecond = 1.25f, RegenDuration = 8f, Color = "#8E2F55", Glyph = "●", Origin = "Matorrales",
            });

            // ------------------------------------------------ Materiales
            Reg(new ItemDef { Id = "fibra", Name = "Fibra de junco", Category = ItemCategory.Material, MaxStack = 30, Value = 1, Description = "Tallos flexibles. Sirven para vendas, cuerdas y correas.", Color = "#C9C27A", Glyph = "≈", Origin = "Orillas y bordes de los campos" });
            Reg(new ItemDef { Id = "hierba", Name = "Hierba de sanación", Category = ItemCategory.Material, MaxStack = 30, Value = 2, Description = "Hojas de olor amargo que cierran heridas.", Color = "#6E9B4A", Glyph = "♣", Origin = "Junto a piedras con musgo" });
            Reg(new ItemDef { Id = "resina", Name = "Resina de árbol", Category = ItemCategory.Material, MaxStack = 30, Value = 2, Description = "Pegajosa y dorada.", Color = "#D9A23C", Glyph = "◖", Origin = "Troncos de árboles grandes" });
            Reg(new ItemDef { Id = "lasca", Name = "Lasca de piedra", Category = ItemCategory.Material, MaxStack = 30, Value = 1, Description = "Piedra dura de borde afilado.", Color = "#8C8A86", Glyph = "▲", Origin = "Rocas sueltas" });
            Reg(new ItemDef { Id = "cuero", Name = "Cuero de jabalí", Category = ItemCategory.Material, MaxStack = 20, Value = 4, Description = "Grueso y resistente. Base de las piezas de explorador.", Color = "#7A5232", Glyph = "■", Origin = "Jabalíes de musgo" });
            Reg(new ItemDef { Id = "musgo", Name = "Musgo de lomo", Category = ItemCategory.Material, MaxStack = 20, Value = 3, Description = "Musgo esponjoso que crece en el lomo de los jabalíes. Amortigua golpes.", Color = "#5E7D3A", Glyph = "♠", Origin = "Jabalíes de musgo" });
            Reg(new ItemDef { Id = "fragmento", Name = "Fragmento de cápsula", Category = ItemCategory.Material, MaxStack = 20, Value = 6, Description = "Un trozo de cápsula antigua. Vibra un poco si lo acercas al oído.", Color = "#B79BD9", Glyph = "◇", Origin = "Ruinas y ecos" });
            Reg(new ItemDef { Id = "polvo_eco", Name = "Polvo de eco", Category = ItemCategory.Material, MaxStack = 30, Value = 3, Description = "Lo que queda cuando un eco se deshace. Brilla en la oscuridad.", Color = "#7FA8E8", Glyph = "✦", Origin = "Ecos errantes" });

            // ------------------------------------------------ Arma del primer prototipo: la espada
            var espada = Reg(new ItemDef
            {
                Id = "espada_reverso", Name = "Espada corta del Reverso", Category = ItemCategory.Arma, Slot = EquipSlot.Arma, Value = 30, Unique = true,
                Description = "Arma equilibrada para aprender los fundamentos. Fue del padre de Haro: no es bonita, pero no se rompe.",
                Origin = "Regalo de Haro", Color = "#A9B0B8", Glyph = "†",
            });
            espada.Mods.Attack = 12f;
            espada.Upgrades.Add(new UpgradeDef
            {
                Id = "filo_templado", Name = "Filo templado", Description = "Más daño por golpe.",
                Cost = new List<ItemAmount> { new ItemAmount("lasca", 3), new ItemAmount("resina", 1) },
                Mods = new StatMods { Attack = 4f },
            });
            espada.Upgrades.Add(new UpgradeDef
            {
                Id = "empunadura_ligera", Name = "Empuñadura ligera", Description = "Golpes más rápidos y menos agotadores.",
                Cost = new List<ItemAmount> { new ItemAmount("cuero", 1), new ItemAmount("fibra", 2) },
                Mods = new StatMods { AttackSpeed = 0.12f, StaminaCost = -0.15f },
            });

            // ------------------------------------------------ Armaduras (cabeza, torso, brazos, piernas)
            var capucha = Reg(new ItemDef { Id = "capucha_explorador", Name = "Capucha de explorador", Category = ItemCategory.Armadura, Slot = EquipSlot.Cabeza, Value = 18, Description = "Equipo de explorador: protección práctica que no tapa los oídos.", Origin = "Refugio de la Parada o mesa de trabajo", Color = "#6E5A44", Glyph = "∩" });
            capucha.Mods.Defense = 3f; capucha.Mods.Attr(AttributeType.Percepcion, 1);

            var chaleco = Reg(new ItemDef { Id = "chaleco_explorador", Name = "Chaleco de explorador", Category = ItemCategory.Armadura, Slot = EquipSlot.Torso, Value = 20, Description = "Se lleva sobre la túnica morada sin taparla del todo.", Origin = "Mesa de trabajo", Color = "#7A5232", Glyph = "▣" });
            chaleco.Mods.Defense = 5f; chaleco.Mods.StaminaRegen = 0.05f;

            var peto = Reg(new ItemDef { Id = "peto_guardian", Name = "Peto de guardián", Category = ItemCategory.Armadura, Slot = EquipSlot.Torso, Value = 40, Description = "Armadura de guardián de la época nómada. Mayor defensa a costa de movilidad.", Origin = "Gruta de los Nómadas", Color = "#8A7F70", Glyph = "▣" });
            peto.Mods.Defense = 9f; peto.Mods.MoveSpeed = -0.05f;

            var brazales = Reg(new ItemDef { Id = "brazales_cuero", Name = "Brazales de cuero curtido", Category = ItemCategory.Armadura, Slot = EquipSlot.Brazos, Value = 15, Description = "Los ecos muerden los brazos primero, dice Haro.", Origin = "Regalo de Haro", Color = "#6B4A33", Glyph = "‖" });
            brazales.Mods.Defense = 4f;
            brazales.Upgrades.Add(new UpgradeDef { Id = "refuerzo_musgo", Name = "Refuerzo de musgo", Description = "Más defensa.", Cost = new List<ItemAmount> { new ItemAmount("musgo", 2), new ItemAmount("fibra", 1) }, Mods = new StatMods { Defense = 3f } });
            brazales.Upgrades.Add(new UpgradeDef { Id = "correas_flexibles", Name = "Correas flexibles", Description = "Más velocidad de ataque.", Cost = new List<ItemAmount> { new ItemAmount("cuero", 1), new ItemAmount("fibra", 2) }, Mods = new StatMods { AttackSpeed = 0.06f } });

            var grebas = Reg(new ItemDef { Id = "grebas_explorador", Name = "Grebas de explorador", Category = ItemCategory.Armadura, Slot = EquipSlot.Piernas, Value = 16, Description = "Ligeras, con suela de corteza.", Origin = "Mesa de trabajo o trueque", Color = "#5D4630", Glyph = "‖" });
            grebas.Mods.Defense = 3f; grebas.Mods.MoveSpeed = 0.03f;

            var yelmo = Reg(new ItemDef { Id = "yelmo_parada", Name = "Yelmo antiguo de la Parada", Category = ItemCategory.Armadura, Slot = EquipSlot.Cabeza, Value = 45, Unique = true, Description = "Armadura antigua vinculada a la historia de la Parada. Lo llevaba el Guardián.", Origin = "Guardián de Ecos (derrotado)", Color = "#9C8E6E", Glyph = "∩" });
            yelmo.Mods.Defense = 6f; yelmo.Mods.ResistResonance = 0.1f;

            // ------------------------------------------------ Accesorios
            var amuleto = Reg(new ItemDef { Id = "amuleto_familiar", Name = "Amuleto familiar", Category = ItemCategory.Accesorio, Slot = EquipSlot.Accesorio1, Value = 0, Unique = true, Description = "Medallón con el rombo de la familia. Eira lo llevaba cuando estudiaba la Cápsula.", Origin = "Familia de Nilo", Function = "Afina la Escucha de Resonancias.", Color = "#C9A14A", Glyph = "◈" });
            amuleto.Mods.Attr(AttributeType.Resonancia, 1);

            var botonBoro = Reg(new ItemDef { Id = "boton_boro", Name = "Botón de la suerte de Boro", Category = ItemCategory.Accesorio, Slot = EquipSlot.Accesorio1, Value = 10, Unique = true, Description = "Boro dice que trae buena conversación. Y que ve cosas pequeñas.", Origin = "Regalo de Boro", Color = "#E8E2D0", Glyph = "◎" });
            botonBoro.Mods.Attr(AttributeType.Percepcion, 1);

            var colgante = Reg(new ItemDef { Id = "colgante_fragmento", Name = "Colgante de fragmento", Category = ItemCategory.Accesorio, Slot = EquipSlot.Accesorio1, Value = 22, Description = "Equipo de Resonancia: un fragmento de cápsula atado con junco. Amortigua los ecos.", Origin = "Mesa de trabajo", Color = "#B79BD9", Glyph = "◇" });
            colgante.Mods.ResistResonance = 0.15f;

            var cuentaG = Reg(new ItemDef { Id = "cuenta_guardian", Name = "Cuenta del Guardián", Category = ItemCategory.Accesorio, Slot = EquipSlot.Accesorio1, Value = 50, Unique = true, Description = "El Guardián la dejó al descansar. Late como un corazón lento.", Origin = "Guardián de Ecos (calmado)", Color = "#8FB8E8", Glyph = "◉" });
            cuentaG.Mods.Attr(AttributeType.Resonancia, 2); cuentaG.Mods.ResistResonance = 0.1f;

            // ------------------------------------------------ Herramientas (lámina de Nilo: cuaderno, navaja, catalejo)
            Reg(new ItemDef { Id = "cuaderno", Name = "Cuaderno de campo", Category = ItemCategory.Herramienta, Unique = true, Value = 0, Description = "La memoria práctica de Nilo. Ayuda a recordar sin resolver los misterios por él.", Origin = "Regalo de Eira", Function = "Atlas, Bestiario, Culturas, Personajes, Resonancias, Misterios y Objetos ({Cuaderno}).", Color = "#8B5A2B", Glyph = "▤" });
            Reg(new ItemDef { Id = "navaja", Name = "Navaja multiusos", Category = ItemCategory.Herramienta, Unique = true, Value = 0, Description = "Corta juncos, cuerdas y enredaderas.", Origin = "Taller de Tomé", Function = "Abre caminos tapados por enredaderas.", Color = "#B0B6BC", Glyph = "⌐" });
            Reg(new ItemDef { Id = "catalejo", Name = "Catalejo", Category = ItemCategory.Herramienta, Unique = true, Value = 0, Description = "Latón abollado y una lente limpia. Acerca lo lejano.", Origin = "Era de Daren", Function = "Observar lugares lejanos y criaturas ({Catalejo}).", Color = "#B08D57", Glyph = "⊸" });

            // ------------------------------------------------ Objetos narrativos y culturales
            Reg(new ItemDef { Id = "pagina_eira", Name = "Página del cuaderno de Eira", Category = ItemCategory.Narrativo, Unique = true, Description = "«La Cápsula no guarda el pasado: guarda lo que alguien quiso conservar.» Debajo, un mapa a medio dibujar hacia el noreste.", Origin = "Taller de Eira y Daren", Function = "Alguien que conociera a Eira podría reconocer su letra.", Color = "#E8DCC0", Glyph = "▭" });
            Reg(new ItemDef { Id = "gubia_daren", Name = "Gubia de Daren", Category = ItemCategory.Narrativo, Unique = true, Description = "La herramienta de tallar de tu padre, junto a un tapir de madera sin terminar.", Origin = "Taller de Eira y Daren", Function = "Daren nunca se separaba de ella.", Color = "#9C7A54", Glyph = "⌐" });
            Reg(new ItemDef { Id = "cuenta_boro", Name = "Cuenta de memoria de Boro", Category = ItemCategory.Narrativo, Unique = true, Description = "Una pequeña cápsula del tamaño de una cereza. Dentro hay un recuerdo al que Boro renunció.", Origin = "Boro", Function = "Memoria privada: escucharla requiere su consentimiento.", Color = "#C7A6E0", Glyph = "◉" });
            Reg(new ItemDef { Id = "cuchara_doblada", Name = "Cuchara doblada", Category = ItemCategory.Cultural, Unique = true, Value = 1, Description = "Para cualquiera, basura. Para Boro, una cuchara que ha vivido.", Origin = "Lo alto del arco de piedra", Function = "Parte de la colección de Boro.", Color = "#B8B8B0", Glyph = "ϒ" });
            Reg(new ItemDef { Id = "boton_nacar", Name = "Botón de nácar", Category = ItemCategory.Cultural, Unique = true, Value = 1, Description = "Brilla con todos los colores a la vez. Nadie sabe de qué prenda cayó.", Origin = "Colina de las Nubes", Function = "Parte de la colección de Boro.", Color = "#F0EDE6", Glyph = "◎" });
            Reg(new ItemDef { Id = "piedra_agujero", Name = "Piedra con agujero", Category = ItemCategory.Cultural, Unique = true, Value = 1, Description = "Si miras a través del agujero, el mundo parece más pequeño y más ordenado.", Origin = "Islote del Lago de Boro", Function = "Parte de la colección de Boro.", Color = "#8C8A86", Glyph = "◯" });
        }

        private static void AddSkills()
        {
            // Superviviente: salud, resistencia, defensa, curación y combate.
            Reg(new SkillDef { Id = "piel_curtida", Name = "Piel curtida", Tree = "Superviviente", Tier = 1, Description = "+15 % de defensa del equipo y +5 % de resistencia física." });
            Reg(new SkillDef { Id = "vendaje_rapido", Name = "Vendaje rápido", Tree = "Superviviente", Tier = 1, Description = "Las curaciones se aplican un 40 % más deprisa y puedes moverte con normalidad mientras tanto." });
            Reg(new SkillDef { Id = "contraataque", Name = "Contraataque", Tree = "Superviviente", Tier = 2, Description = "Tras un bloqueo perfecto, tu siguiente golpe hace un 150 % de daño." });
            Reg(new SkillDef { Id = "segundo_aliento", Name = "Segundo aliento", Tree = "Superviviente", Tier = 3, Cost = 2, Description = "Una vez cada 3 minutos, un golpe mortal te deja con 1 punto de salud.", Cooldown = 180f });
            // Explorador: movilidad, secretos, recursos, rastreo.
            Reg(new SkillDef { Id = "paso_ligero", Name = "Paso ligero", Tree = "Explorador", Tier = 1, Description = "Correr gasta un 30 % menos de aguante." });
            Reg(new SkillDef { Id = "ojo_rastreador", Name = "Ojo de rastreador", Tree = "Explorador", Tier = 1, Description = "Detectas recursos, rastros y objetos ocultos al doble de distancia." });
            Reg(new SkillDef { Id = "esquiva_agil", Name = "Esquiva ágil", Tree = "Explorador", Tier = 2, Description = "Esquivar gasta un 30 % menos de aguante y protege durante más tiempo." });
            Reg(new SkillDef { Id = "recolector", Name = "Recolector", Tree = "Explorador", Tier = 2, Description = "30 % de probabilidad de obtener un material extra al recolectar." });
            // Resonancia: Cápsulas, fenómenos anómalos y habilidades únicas.
            Reg(new SkillDef { Id = "oido_fino", Name = "Oído fino", Tree = "Resonancia", Tier = 1, Description = "+50 % de alcance de la Escucha y escuchas más deprisa." });
            Reg(new SkillDef { Id = "pulso_resonancia", Name = "Pulso de Resonancia", Tree = "Resonancia", Tier = 2, Active = true, Cooldown = 25f, Description = "Habilidad activa ({Habilidad}): una onda que aturde a los ecos cercanos. 25 s de recuperación." });
            Reg(new SkillDef { Id = "eco_protector", Name = "Eco protector", Tree = "Resonancia", Tier = 2, Description = "+25 % de resistencia al daño de Resonancia." });
            Reg(new SkillDef { Id = "lectura_profunda", Name = "Lectura profunda", Tree = "Resonancia", Tier = 3, Cost = 2, Description = "Permite escuchar Resonancias profundas aunque tu atributo de Resonancia sea bajo." });
        }

        private static void AddRecipes()
        {
            Reg(new RecipeDef { Id = "r_venda", Name = "Venda de lino", Station = "campo", Inputs = { new ItemAmount("fibra", 2) }, Output = new ItemAmount("venda", 2), Description = "Se puede hacer en cualquier parte." });
            Reg(new RecipeDef { Id = "r_unguento", Name = "Ungüento de raíz", Station = "campo", RequiredFlag = "receta_basica", Inputs = { new ItemAmount("hierba", 2), new ItemAmount("resina", 1) }, Output = new ItemAmount("unguento", 1), Description = "Tomé te enseña la proporción." });
            Reg(new RecipeDef { Id = "r_colgante", Name = "Colgante de fragmento", Station = "mesa", RequiredFlag = "receta_basica", Inputs = { new ItemAmount("fragmento", 2), new ItemAmount("fibra", 2) }, Output = new ItemAmount("colgante_fragmento", 1) });
            Reg(new RecipeDef { Id = "r_chaleco", Name = "Chaleco de explorador", Station = "mesa", RequiredFlag = "receta_armadura", Inputs = { new ItemAmount("cuero", 3), new ItemAmount("fibra", 3) }, Output = new ItemAmount("chaleco_explorador", 1) });
            Reg(new RecipeDef { Id = "r_capucha", Name = "Capucha de explorador", Station = "mesa", RequiredFlag = "receta_armadura", Inputs = { new ItemAmount("cuero", 2), new ItemAmount("fibra", 2) }, Output = new ItemAmount("capucha_explorador", 1) });
            Reg(new RecipeDef { Id = "r_grebas", Name = "Grebas de explorador", Station = "mesa", RequiredFlag = "receta_armadura", Inputs = { new ItemAmount("cuero", 2), new ItemAmount("fibra", 2), new ItemAmount("resina", 1) }, Output = new ItemAmount("grebas_explorador", 1) });
        }

        private static void AddMerchants()
        {
            var suri = new MerchantDef { NpcId = "suri", Name = "Cocina de Suri", Greeting = "Comida a cambio de lo que traigas. Aquí nadie pasa hambre, pero tampoco se regala la sal." };
            suri.Offers.Add(new TradeOffer { ItemId = "pan_raiz", Count = 1, Stock = 6 });
            suri.Offers.Add(new TradeOffer { ItemId = "estofado", Count = 1, Stock = 3 });
            suri.Offers.Add(new TradeOffer { ItemId = "bayas", Count = 3, Stock = 4 });
            suri.Offers.Add(new TradeOffer { ItemId = "venda", Count = 2, Stock = 2 });
            suri.Appreciation[ItemCategory.Material] = 1.2f;
            Reg(suri);

            var boro = new MerchantDef { NpcId = "boro", Name = "Carro de Boro", Greeting = "Cosas útiles a cambio de cosas que tú creías inútiles. Todo el mundo gana." };
            boro.Offers.Add(new TradeOffer { ItemId = "tonico_bruma", Count = 1, Stock = 3 });
            boro.Offers.Add(new TradeOffer { ItemId = "cuero", Count = 2, Stock = 2 });
            boro.Offers.Add(new TradeOffer { ItemId = "lasca", Count = 3, Stock = 3 });
            boro.Offers.Add(new TradeOffer { ItemId = "fragmento", Count = 1, Stock = 2 });
            boro.Offers.Add(new TradeOffer { ItemId = "grebas_explorador", Count = 1, Stock = 1 });
            boro.Offers.Add(new TradeOffer { ItemId = "venda", Count = 2, Stock = 3 });
            boro.Appreciation[ItemCategory.Material] = 1.1f;
            boro.Appreciation[ItemCategory.Consumible] = 0.8f;
            Reg(boro);
        }
    }
}
