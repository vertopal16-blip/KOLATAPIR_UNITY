using System.Collections.Generic;

namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private static ScheduleEntry S(float start, float end, Activity a, string node, params string[] route) => new ScheduleEntry(start, end, a, node, route);

        private static void AddNpcs()
        {
            // ================================================= Tribu del Reverso (humanos)
            var gina = new NpcDef
            {
                Id = "gina", Name = "Abuela Gina", Species = Species.Humano, Role = "Memoria cotidiana de la tribu", Culture = "Reverso",
                VoicePitch = 1.12f, WalkSpeed = 1.05f, HomeNode = "gina_in", NotebookEntry = "pj_gina", QuestCritical = true,
                Look = new Appearance { Height = 1.55f, Skin = "#9C6B4E", Hair = "#D9D4CC", HairStyle = "mono", Cloth = "#8E3B46", Cloth2 = "#5A3A2E", Accent = "#C9A14A", Pants = "#4A3B33", Shawl = true },
            };
            gina.Schedule.Add(S(6f, 8f, Activity.Casa, "gina_in"));
            gina.Schedule.Add(S(8f, 12f, Activity.Recolectar, "gina_garden"));
            gina.Schedule.Add(S(12f, 14f, Activity.Comer, "plaza_w"));
            gina.Schedule.Add(S(14f, 18f, Activity.Sentarse, "gina_porch"));
            gina.Schedule.Add(S(18f, 21f, Activity.Contar, "plaza_n"));
            gina.Schedule.Add(S(21f, 6f, Activity.Dormir, "gina_bed"));
            gina.Override = s =>
            {
                if (!s.Has("gina_intro_done")) return S(0f, 24f, Activity.Casa, "gina_in");
                if (s.AtStep("mq_silencio", "despedida")) return S(0f, 24f, Activity.Vigilar, "gate_in");
                return null;
            };
            gina.SelectDialogue = s =>
            {
                if (!s.Has("gina_intro_done")) return "gina_intro";
                if (s.AtStep("mq_silencio", "despedida")) return "gina_despedida";
                if (s.Has("gina_despedida") && s.Chapter >= 2 && !s.Has("gina_regreso_visto")) return "gina_regreso";
                if (!s.Has("gina_despedida")) return "gina_capsula";
                return "gina_general";
            };
            gina.Barks.Add(new AmbientLine("Abrígate, que en la hondonada hace frío.", s => !s.Has("gina_despedida")));
            gina.Barks.Add(new AmbientLine("Esa túnica te sienta de maravilla."));
            gina.Barks.Add(new AmbientLine("¿Has comido? Seguro que no."));
            gina.Barks.Add(new AmbientLine("Has vuelto... y con los ojos llenos de cosas.", s => s.Chapter >= 2));
            Reg(gina);

            var oren = new NpcDef
            {
                Id = "oren", Name = "Anciano Oren", Species = Species.Humano, Role = "Guardián de la memoria oral", Culture = "Reverso",
                VoicePitch = 0.82f, WalkSpeed = 0.95f, HomeNode = "oren_in", NotebookEntry = "pj_oren", QuestCritical = true,
                Look = new Appearance { Height = 1.72f, Skin = "#5C3A24", Hair = "#E8E4DC", HairStyle = "largo", Beard = true, Staff = true, Cloth = "#56684A", Cloth2 = "#3E4A36", Accent = "#A8442E", Pants = "#3A3A30" },
            };
            oren.Schedule.Add(S(8f, 12f, Activity.Meditar, "stones"));
            oren.Schedule.Add(S(12f, 14f, Activity.Comer, "plaza_sw"));
            oren.Schedule.Add(S(14f, 18f, Activity.Sentarse, "tree_seat"));
            oren.Schedule.Add(S(18f, 22f, Activity.Contar, "plaza_s"));
            oren.Schedule.Add(S(22f, 8f, Activity.Dormir, "oren_bed"));
            oren.SelectDialogue = s =>
            {
                if (!s.Has("oren_capsula_done") && !s.Heard("res_huellas_capsula")) return "oren_capsula";
                if (s.Has("pista_sello") && !s.Has("oren_sello_confrontado")) return "oren_sello";
                if (s.Has("dec_compartir") && s.Has("gina_regreso_visto") && !s.Has("oren_reaccion")) return "oren_reaccion";
                return "oren_general";
            };
            oren.Barks.Add(new AmbientLine("Las piedras también escuchan."));
            oren.Barks.Add(new AmbientLine("Una canción no explica: recuerda."));
            oren.Barks.Add(new AmbientLine("El río nunca dice dos veces lo mismo."));
            Reg(oren);

            var haro = new NpcDef
            {
                Id = "haro", Name = "Haro", Species = Species.Humano, Role = "Guardián del puente", Culture = "Reverso",
                VoicePitch = 0.78f, WalkSpeed = 1.4f, HomeNode = "haro_in", NotebookEntry = "pj_haro", QuestCritical = true,
                Look = new Appearance { Height = 1.82f, Build = 1.25f, Skin = "#7A4A2C", Hair = "#2A1C12", HairStyle = "corto", Beard = true, Cloth = "#6B4A33", Cloth2 = "#4A3324", Accent = "#8C2F2F", Pants = "#3E3328" },
            };
            haro.Schedule.Add(S(6f, 20f, Activity.Patrullar, "guard", "guard", "guard_bridge", "gate_out", "guard_dummy"));
            haro.Schedule.Add(S(20f, 22f, Activity.Sentarse, "plaza_e"));
            haro.Schedule.Add(S(22f, 6f, Activity.Dormir, "haro_bed"));
            haro.Override = s =>
            {
                if (s.AtStep("mq_silencio", "haro") || s.AtStep("mq_silencio", "entrenar")) return S(0f, 24f, Activity.Vigilar, "guard_dummy");
                return null;
            };
            haro.SelectDialogue = s =>
            {
                if (s.AtStep("mq_silencio", "haro")) return "haro_espada";
                if (s.AtStep("mq_silencio", "entrenar")) return "haro_entrenar";
                if (s.AtStep("mq_silencio", "despedida")) return "haro_espera";
                if (!s.Has("haro_espada_dada")) return "haro_bloqueo";
                return "haro_general";
            };
            haro.Barks.Add(new AmbientLine("Ojo con el borde, chico."));
            haro.Barks.Add(new AmbientLine("El puente aguanta. Las piernas, a veces no."));
            haro.Barks.Add(new AmbientLine("Los ecos odian el fuego. Recuérdalo.", s => s.Chapter >= 2));
            Reg(haro);

            var lia = new NpcDef
            {
                Id = "lia", Name = "Lía", Species = Species.Humano, Role = "Recolectora", Culture = "Reverso",
                VoicePitch = 1.18f, WalkSpeed = 1.3f, HomeNode = "lia_in", NotebookEntry = "pj_lia",
                Look = new Appearance { Height = 1.66f, Skin = "#B07A55", Hair = "#1B1311", HairStyle = "trenzas", Cloth = "#C7B48D", Cloth2 = "#8A7550", Accent = "#4F7D6A", Pants = "#5A4A3A", Basket = true },
            };
            lia.Schedule.Add(S(7f, 12f, Activity.Recolectar, "fields", "fields", "fields_2", "fields_3"));
            lia.Schedule.Add(S(12f, 13f, Activity.Trabajar, "store"));
            lia.Schedule.Add(S(13f, 14f, Activity.Comer, "plaza_n"));
            lia.Schedule.Add(S(14f, 18f, Activity.Recolectar, "fields_3", "fields_3", "fields_2", "fields"));
            lia.Schedule.Add(S(18f, 21f, Activity.Conversar, "plaza_ne"));
            lia.Schedule.Add(S(21f, 7f, Activity.Dormir, "lia_bed"));
            lia.SelectDialogue = s => "lia_general";
            lia.Barks.Add(new AmbientLine("¡Cuidado con los brotes!"));
            lia.Barks.Add(new AmbientLine("Los juncos de la orilla del campo están perfectos hoy."));
            Reg(lia);

            var tome = new NpcDef
            {
                Id = "tome", Name = "Tomé", Species = Species.Humano, Role = "Artesano", Culture = "Reverso",
                VoicePitch = 0.95f, WalkSpeed = 1.25f, HomeNode = "tome_in", NotebookEntry = "pj_tome",
                Look = new Appearance { Height = 1.75f, Skin = "#C08B66", Hair = "#3A2A1E", HairStyle = "rizado", Cloth = "#9A7B55", Cloth2 = "#6A5238", Accent = "#4F6A8C", Pants = "#4A4038", Apron = true },
            };
            tome.Schedule.Add(S(7f, 12f, Activity.Trabajar, "workshop"));
            tome.Schedule.Add(S(12f, 14f, Activity.Comer, "plaza_e"));
            tome.Schedule.Add(S(14f, 19f, Activity.Trabajar, "workshop"));
            tome.Schedule.Add(S(19f, 21f, Activity.Conversar, "plaza_e"));
            tome.Schedule.Add(S(21f, 7f, Activity.Dormir, "tome_bed"));
            tome.SelectDialogue = s =>
            {
                if (!s.Quests.IsKnown("sq_tome")) return "tome_intro";
                if (s.AtStep("sq_tome", "materiales")) return "tome_entrega";
                return "tome_general";
            };
            tome.Barks.Add(new AmbientLine("Si se rompe, se arregla. Si no se rompe, se mejora."));
            Reg(tome);

            var suri = new NpcDef
            {
                Id = "suri", Name = "Suri", Species = Species.Humano, Role = "Cocinera", Culture = "Reverso",
                VoicePitch = 1.0f, WalkSpeed = 1.1f, HomeNode = "suri_in", NotebookEntry = "pj_suri", MerchantId = "suri",
                Look = new Appearance { Height = 1.6f, Skin = "#4A2E1F", Hair = "#151010", HairStyle = "panuelo", Cloth = "#B5523B", Cloth2 = "#7A3526", Accent = "#E0B85A", Pants = "#3A2E28", Apron = true, Hat = true },
            };
            suri.Schedule.Add(S(6f, 11f, Activity.Trabajar, "store"));
            suri.Schedule.Add(S(11f, 14f, Activity.Cocinar, "plaza_cook"));
            suri.Schedule.Add(S(14f, 17f, Activity.Recolectar, "fields_3"));
            suri.Schedule.Add(S(17f, 21f, Activity.Cocinar, "plaza_cook"));
            suri.Schedule.Add(S(21f, 6f, Activity.Dormir, "suri_bed"));
            suri.SelectDialogue = s => "suri_general";
            suri.Barks.Add(new AmbientLine("¡Hay estofado para quien lo pida!"));
            suri.Barks.Add(new AmbientLine("Kiko, ¡deja de correr con la cuchara!"));
            Reg(suri);

            var kiko = new NpcDef
            {
                Id = "kiko", Name = "Kiko", Species = Species.Humano, Role = "Niño", Culture = "Reverso",
                VoicePitch = 1.45f, WalkSpeed = 2.3f, HomeNode = "suri_in", NotebookEntry = "pj_kiko",
                Look = new Appearance { Height = 1.15f, Skin = "#8D5A3B", Hair = "#1A1410", HairStyle = "despeinado", Cloth = "#D8C7A0", Cloth2 = "#A0805A", Accent = "#B5523B", Pants = "#7A5A3A" },
            };
            kiko.Schedule.Add(S(8f, 12f, Activity.Jugar, "kiko_play1", "kiko_play1", "kiko_play2", "kiko_play3", "plaza_n"));
            kiko.Schedule.Add(S(12f, 14f, Activity.Comer, "plaza_sw"));
            kiko.Schedule.Add(S(14f, 19f, Activity.Jugar, "tree_path", "tree_path", "fields_3", "kiko_play3"));
            kiko.Schedule.Add(S(19f, 21f, Activity.Sentarse, "plaza_w"));
            kiko.Schedule.Add(S(21f, 8f, Activity.Dormir, "suri_bed"));
            kiko.SelectDialogue = s => "kiko_general";
            kiko.Barks.Add(new AmbientLine("¡Te echo una carrera hasta el árbol!"));
            kiko.Barks.Add(new AmbientLine("¡Nilo! ¿Me enseñas la espada?", s => s.Has("haro_espada_dada")));
            Reg(kiko);

            // ================================================= Prados que Recuerdan (kolotapires)
            var boro = new NpcDef
            {
                Id = "boro", Name = "Boro", Species = Species.Kolotapir, Role = "Viajero y coleccionista", Culture = "Errantes",
                VoicePitch = 0.72f, WalkSpeed = 1.05f, HomeNode = "boro_tent", NotebookEntry = "pj_boro", MerchantId = "boro", QuestCritical = true,
                Look = new Appearance { Height = 2.0f, Build = 1.3f, Skin = "#4E4A4F", Cloth = "#C9B79C", Cloth2 = "#8C6E4E", Accent = "#B5523B", Pants = "#5A4A3E", Backpack = true, Hat = true },
            };
            boro.Schedule.Add(S(7f, 11f, Activity.Trabajar, "boro_cart"));
            boro.Schedule.Add(S(11f, 16f, Activity.Pasear, "lake_n", "lake_n", "m5", "lake_w", "boro_camp"));
            boro.Schedule.Add(S(16f, 21f, Activity.Sentarse, "boro_fire"));
            boro.Schedule.Add(S(21f, 7f, Activity.Dormir, "boro_tent"));
            boro.SelectDialogue = s =>
            {
                if (!s.Has("boro_conocido")) return "boro_intro";
                if (s.Has("boro_cuenta_sin_permiso") && !s.Has("boro_dolido_visto")) return "boro_dolido";
                if (s.QuestActive("sq_boro") && s.HasItem("cuchara_doblada") && s.HasItem("boton_nacar") && s.HasItem("piedra_agujero")) return "boro_entrega";
                if (s.AtStep("mq_prados", "testimonio") || (!s.Has("boro_testimonio") && s.QuestActive("mq_prados"))) return "boro_testimonio";
                return "boro_general";
            };
            boro.Barks.Add(new AmbientLine("Una tuerca sin tornillo... preciosa."));
            boro.Barks.Add(new AmbientLine("¿Eso que pisas es un botón? Ah, no. Qué pena."));
            boro.Barks.Add(new AmbientLine("Hmmf. Hmmf. Huele a lluvia y a recuerdos."));
            Reg(boro);

            var bruma = new NpcDef
            {
                Id = "bruma", Name = "Bruma", Species = Species.Kolotapir, Role = "Exploradora", Culture = "Kolotapires de los Prados",
                VoicePitch = 0.9f, WalkSpeed = 1.15f, HomeNode = "bc_beds", NotebookEntry = "pj_bruma",
                Look = new Appearance { Height = 1.85f, Skin = "#5A5258", Cloth = "#7E9C8A", Cloth2 = "#55705F", Accent = "#D08A3C", Pants = "#4A4440" },
            };
            bruma.Schedule.Add(S(7f, 12f, Activity.Recolectar, "herbs", "herbs", "herbs2"));
            bruma.Schedule.Add(S(12f, 17f, Activity.Mirar, "hill_top"));
            bruma.Schedule.Add(S(17f, 21f, Activity.Sentarse, "bc_fire"));
            bruma.Schedule.Add(S(21f, 7f, Activity.Dormir, "bc_beds"));
            bruma.SelectDialogue = SelectCloudDialogue;
            bruma.Barks.Add(new AmbientLine("¡Algodón Pensativo!", s => !s.Has("nube_nombrada")));
            bruma.Barks.Add(new AmbientLine("Mira cómo piensa esa nube...", s => !s.Has("nube_nombrada")));
            Reg(bruma);

            var copo = new NpcDef
            {
                Id = "copo", Name = "Copo", Species = Species.Kolotapir, Role = "Artesano", Culture = "Kolotapires de los Prados",
                VoicePitch = 0.8f, WalkSpeed = 1.1f, HomeNode = "bc_beds", NotebookEntry = "pj_copo",
                Look = new Appearance { Height = 1.9f, Build = 1.15f, Skin = "#6A5E58", Cloth = "#D8CBB0", Cloth2 = "#A0907A", Accent = "#9E3B2E", Pants = "#5A4E44", Apron = true },
            };
            copo.Schedule.Add(S(7f, 12f, Activity.Recolectar, "herbs2", "herbs2", "herbs"));
            copo.Schedule.Add(S(12f, 17f, Activity.Mirar, "hill_top2"));
            copo.Schedule.Add(S(17f, 21f, Activity.Cocinar, "bc_fire"));
            copo.Schedule.Add(S(21f, 7f, Activity.Dormir, "bc_beds"));
            copo.SelectDialogue = SelectCloudDialogue;
            copo.Barks.Add(new AmbientLine("¡Señora Lluvia!", s => !s.Has("nube_nombrada")));
            copo.Barks.Add(new AmbientLine("Esa nube tiene tripa. Digan lo que digan.", s => !s.Has("nube_nombrada")));
            Reg(copo);

            AddConversations();
        }

        private static string SelectCloudDialogue(GameState s)
        {
            if (s.Has("nube_nombrada")) return "nube_post";
            if (!s.Quests.IsKnown("sq_nube")) return "nube_intro";
            if (s.Has("nube_observada")) return "nube_propuesta";
            return "nube_esperando";
        }

        private static AmbientConversation Conv(string id, string a, string b, System.Func<GameState, bool> cond, params ResonanceLine[] lines)
        {
            var c = new AmbientConversation { Id = id, NpcA = a, NpcB = b, Condition = cond };
            c.Lines.AddRange(lines);
            Reg(c);
            return c;
        }

        private static void AddConversations()
        {
            Conv("conv_gina_oren", "gina", "oren", s => !s.Has("dec_compartir"),
                L("Abuela Gina", "La Cápsula volvió a zumbar, Oren."),
                L("Anciano Oren", "Las Cápsulas zumban. Los perros ladran. No todo es una señal."),
                L("Abuela Gina", "Eso mismo dijiste hace dos inviernos."),
                L("Anciano Oren", "..."));
            Conv("conv_gina_oren_2", "gina", "oren", s => s.Has("dec_compartir"),
                L("Abuela Gina", "Se fueron porque quisieron, Oren."),
                L("Anciano Oren", "Lo sé. Y volverán si quieren. O no. Pero ya no lo esconderemos."));
            Conv("conv_suri_lia", "suri", "lia", null,
                L("Suri", "¿Otra vez juncos? Vas a tejer el asentamiento entero."),
                L("Lía", "Algún día haremos una red tan grande que podremos pescar nubes."),
                L("Suri", "Ni se te ocurra decírselo a Kiko."));
            Conv("conv_tome_lia", "tome", "lia", null,
                L("Tomé", "Mi abuelo decía que, antes de llegar aquí, la tribu construía casas que se podían plegar."),
                L("Lía", "¿Y por qué dejamos de hacerlo?"),
                L("Tomé", "Porque dejamos de huir, supongo."));
            Conv("conv_haro_tome", "haro", "tome", null,
                L("Haro", "Si Nilo cruza, ¿quién arreglará el tejado de Gina?"),
                L("Tomé", "Yo. Como siempre. Pero no se lo digas a ella."));
            Conv("conv_oren_kiko", "oren", "kiko", null,
                L("Kiko", "Abuelo Oren, ¿por qué faltan piedras en el círculo?"),
                L("Anciano Oren", "Porque hay paradas que decidimos olvidar."),
                L("Kiko", "¿Y si alguien se acuerda sin querer?"),
                L("Anciano Oren", "...Ve a ayudar a Suri, anda."));
            Conv("conv_bruma_copo", "bruma", "copo", s => !s.Has("nube_nombrada"),
                L("Bruma", "Algodón Pensativo."),
                L("Copo", "Señora Lluvia."),
                L("Bruma", "Algodón."),
                L("Copo", "Lluvia."));
            Conv("conv_bruma_copo_2", "bruma", "copo", s => s.Has("nube_nombrada"),
                L("Bruma", "Ya ha cambiado de forma."),
                L("Copo", "Ahora parece una tetera."),
                L("Bruma", "No empieces."));
        }
    }
}
