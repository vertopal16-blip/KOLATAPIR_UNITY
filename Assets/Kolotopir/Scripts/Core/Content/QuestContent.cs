using System;

namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private static QuestStep Step(string id, string text, Func<GameState, bool> done, int xp, string target, params string[] hints)
        {
            return new QuestStep { Id = id, Text = text, Done = done, Xp = xp, HintTarget = target, Hints = hints };
        }

        private static int Count(params bool[] values)
        {
            int c = 0;
            foreach (var v in values) if (v) c++;
            return c;
        }

        private static void AddQuests()
        {
            // ------------------------------------------------ Capítulo I — Tribu del Reverso
            var mq1 = new QuestDef
            {
                Id = "mq_silencio",
                Title = "El silencio del Reverso",
                Chapter = "Capítulo I — Tribu del Reverso",
                Kind = QuestKind.Principal,
                Summary = "La Cápsula sellada ha vuelto a zumbar, como la noche en que Eira y Daren desaparecieron. Es hora de dejar de fingir que no se oye.",
                CompletionXp = 80,
            };
            mq1.Steps.Add(Step("hablar_gina", "Habla con la abuela Gina.", s => s.Has("gina_intro_done"), 20, "npc:gina",
                "Gina está en casa, a tu lado.", "Busca a una anciana con chal junto al hogar.", "Mira a Gina y pulsa {Interactuar}."));
            mq1.Steps.Add(Step("oren", "Pregunta al anciano Oren por el zumbido de la Cápsula.", s => s.Has("oren_capsula_done") || s.Heard("res_huellas_capsula"), 25, "npc:oren",
                "Oren pasa las mañanas junto a las Piedras de la Migración y las tardes bajo el gran árbol.",
                "Las Piedras de la Migración están al suroeste del fuego comunal.",
                "Sigue el camino de tierra desde la plaza hacia el suroeste. (También puedes ir directamente a la Cápsula.)"));
            mq1.Steps.Add(Step("capsula", "Investiga la Cápsula sellada en la hondonada del norte.", s => s.Heard("res_huellas_capsula"), 40, "loc:loc_hondonada",
                "La hondonada está al norte, pasados los campos de cultivo.",
                "Acércate a la Cápsula. Cuando veas el destello y oigas el zumbido, mantén {Escuchar}.",
                "Ponte junto a la Cápsula, mírala y mantén {Escuchar} hasta que la Resonancia se complete."));
            var taller = Step("taller", "Examina el taller de Eira y Daren: tres pistas y dos recuerdos.",
                s => s.Has("pista_pagina") && s.Has("pista_gubia") && s.Has("pista_sello") && s.Heard("res_recuerdo_daren") && s.Heard("res_recuerdo_custodio"), 60, "loc:loc_taller",
                "Hay objetos en el taller y señales en la propia Cápsula.",
                "Pistas: una página, una herramienta y el sello. Recuerdos: en la mesa del taller y, otra vez, en la Cápsula.",
                "Examina la mesa (página), la estantería (gubia) y la Cápsula (sello). Escucha la mesa del taller y vuelve a escuchar la Cápsula.");
            taller.Detail = s => $"Pistas {Count(s.Has("pista_pagina"), s.Has("pista_gubia"), s.Has("pista_sello"))}/3 · Recuerdos {Count(s.Heard("res_recuerdo_daren"), s.Heard("res_recuerdo_custodio"))}/2";
            mq1.Steps.Add(taller);
            mq1.Steps.Add(Step("hipotesis", "Anota en el cuaderno qué crees que les pasó a tus padres.", s => s.Hypothesis("mis_padres") != null, 20, null,
                "Abre el cuaderno ({Cuaderno}) en la pestaña Misterios.",
                "Elige la hipótesis que te parezca más probable. Podrás cambiarla si aparecen pruebas nuevas.",
                "Cuaderno → Misterios → «¿Qué les pasó a Eira y Daren?» → elige una hipótesis."));
            mq1.Steps.Add(Step("haro", "Habla con Haro en el puesto del puente.", s => s.Has("haro_espada_dada"), 20, "npc:haro",
                "El puesto de Haro está al noreste, pasada la puerta del asentamiento.",
                "Sigue el camino hacia el puente colgante.", "Haro vigila junto al muñeco de práctica, antes del puente."));
            var entrenar = Step("entrenar", "Entrena con el muñeco de práctica: tres golpes (uno fuerte) y un bloqueo.", s => s.Has("entrenamiento_hecho"), 40, "loc:loc_puesto",
                "Equipa la espada en el inventario ({Inventario}) si aún no lo has hecho.",
                "Golpe rápido: {Atacar}. Golpe fuerte: mantén {Atacar}. Bloquea con {Bloquear} cuando el brazo del muñeco gire hacia ti.",
                "Da tres golpes al muñeco, al menos uno cargado, y bloquea su contragolpe justo cuando brille.");
            entrenar.Detail = s => $"Golpes {Math.Min(3, s.Flags.Get("dummy_hits"))}/3 · Golpe fuerte {(s.Flags.Get("dummy_heavy") > 0 ? "sí" : "no")} · Bloqueo {(s.Flags.Get("dummy_blocks") > 0 ? "sí" : "no")}";
            mq1.Steps.Add(entrenar);
            mq1.Steps.Add(Step("despedida", "Despídete de la abuela Gina en la puerta del asentamiento.", s => s.Has("gina_despedida"), 30, "npc:gina",
                "Gina te espera en la puerta, bajo los amuletos.", "La puerta está en el camino entre la plaza y el puesto de Haro.", "Habla con Gina en la puerta del asentamiento."));
            mq1.Steps.Add(Step("cruzar", "Cruza el puente hacia los Prados que Recuerdan.", s => s.Has("cruzo_puente"), 50, "loc:loc_puente",
                "El puente colgante es el límite del territorio.", "Camina por el puente sin miedo: aguanta.", "Cruza el puente hasta el otro lado del abismo."));
            mq1.OnComplete = ctx =>
            {
                ctx.Command("chapter:2");
                ctx.StartQuest("mq_prados");
                ctx.Command("autosave");
            };
            Reg(mq1);

            // ------------------------------------------------ Capítulo II — Prados que Recuerdan
            var mq2 = new QuestDef
            {
                Id = "mq_prados",
                Title = "Los Prados que Recuerdan",
                Chapter = "Capítulo II — Prados que Recuerdan",
                Kind = QuestKind.Principal,
                Summary = "Primer viaje fuera del Reverso. Las huellas de Eira y Daren siguen aquí, si sabes escucharlas.",
                CompletionXp = 150,
            };
            mq2.Steps.Add(Step("rastro", "Sigue el rastro de tus padres por los Prados (usa la Escucha).", s => s.Heard("res_rastro_prados") || s.Has("boro_conocido"), 30, "loc:loc_mojon",
                "Junto al mojón del puente quedan ecos de pasos.", "Mantén {Escuchar} cerca del mojón para ver las huellas.", "Escucha junto al mojón y sigue las huellas luminosas hacia el norte, hacia el lago."));
            mq2.Steps.Add(Step("boro", "Encuentra a alguien que haya visto a Eira y Daren.", s => s.Has("boro_conocido"), 25, "npc:boro",
                "Las huellas llevan hacia el agua.", "Hay un campamento con un carro en la orilla norte del lago.", "Habla con el kolotapir del carro: Boro."));
            mq2.Steps.Add(Step("testimonio", "Consigue que Boro te cuente lo que vio.", s => s.Has("boro_testimonio"), 40, "npc:boro",
                "Boro quiere completar su colección... o quizá reconozca algo de tu madre.",
                "Ayúdale con su colección, o enséñale algo escrito por Eira.",
                "Habla con Boro llevando la página del cuaderno de Eira, o entrégale la cuchara, el botón y la piedra."));
            mq2.Steps.Add(Step("parada", "Llega a la Parada Antigua.", s => s.Knows("loc_parada"), 20, "loc:loc_parada",
                "Boro señaló al noreste.", "Pasa bajo el arco de piedra y sigue subiendo.", "Las ruinas están al noreste del lago, más allá del arco."));
            mq2.Steps.Add(Step("guardian", "Resuelve el problema del Guardián de Ecos: combate... o calma su memoria.", s => s.Has("guardian_resuelto"), 80, "loc:loc_parada",
                "El Guardián es un eco: quizá tenga algo que recordar.",
                "Hay tres ecos del Guardián alrededor de la Parada (oeste, este y norte). Escúchalos y luego escúchalo a él... o lucha.",
                "Escucha los tres ecos y mantén {Escuchar} frente al Guardián cuando se detenga; o derrótalo con la espada (alza los brazos antes de golpear: esquiva o bloquea)."));
            mq2.Steps.Add(Step("reparar", "Repara la grieta de la Cápsula dañada.", s => s.Has("cap_parada_reparada"), 30, "loc:loc_parada",
                "Una Cápsula dañada necesita una fuente o una pieza que encaje.", "Los fragmentos de cápsula brillan entre las ruinas y en la Peña del Mirador; los ecos también los sueltan.",
                "Lleva un fragmento de cápsula y usa la Cápsula: «Encajar un fragmento»."));
            mq2.Steps.Add(Step("recuerdo", "Reconstruye el recuerdo de la Cápsula de la Parada.", s => s.Has("recuerdo_reconstruido"), 80, "loc:loc_parada",
                "Piensa en cómo empieza y termina un viaje.", "Recuerda el orden kolotapir de la memoria (inscripción del arco, Boro).", "Agua, raíz, piedra, cielo."));
            mq2.Steps.Add(Step("decision", "Decide qué hacer con el recuerdo.", s => s.Has("decision_recuerdo"), 30, "loc:loc_parada",
                "Compartir, ocultar, conservar o dejar inaccesible: no hay respuesta correcta.", "Cada opción tiene un alcance distinto: local, comunitario o global.", "Usa la Cápsula de la Parada y elige."));
            mq2.Steps.Add(Step("este", "Toma el Camino del Este hacia la Ciudad de las Mil Cápsulas.", s => s.Has("fin_capitulo_2"), 60, "loc:loc_este",
                "Las cúpulas de la Ciudad se ven en el horizonte, al este.", "Sigue el camino de tierra hacia el este desde el lago.", "Llega al puente natural del extremo este de los Prados."));
            mq2.OnComplete = ctx => ctx.Command("end_chapter_2");
            Reg(mq2);

            // ------------------------------------------------ Secundarias
            var sqBoro = new QuestDef
            {
                Id = "sq_boro", Title = "La colección de Boro", Kind = QuestKind.Secundaria, CompletionXp = 60,
                Summary = "A Boro le faltan tres objetos 'inútiles' para completar su colección de este invierno.",
            };
            var objetos = Step("objetos", "Encuentra una cuchara doblada, un botón de nácar y una piedra con agujero.",
                s => s.HasItem("cuchara_doblada") && s.HasItem("boton_nacar") && s.HasItem("piedra_agujero"), 20, "npc:boro",
                "Lo inútil suele estar en lugares difíciles de alcanzar.",
                "Uno está en lo alto del arco de piedra, otro entre las flores de la Colina de las Nubes y otro en el islote del lago.",
                "Trepa la enredadera del arco y cruza por arriba con cuidado; busca un brillo al sur de la Colina; nada hasta el islote del Lago de Boro.");
            objetos.Detail = s => $"Cuchara {(s.HasItem("cuchara_doblada") ? "sí" : "no")} · Botón {(s.HasItem("boton_nacar") ? "sí" : "no")} · Piedra {(s.HasItem("piedra_agujero") ? "sí" : "no")}";
            sqBoro.Steps.Add(objetos);
            sqBoro.Steps.Add(Step("entregar", "Entrega la colección a Boro.", s => s.Has("boro_coleccion_completa"), 0, "npc:boro",
                "Boro estará en su campamento o paseando por la orilla.", "Habla con Boro.", "Habla con Boro con los tres objetos encima."));
            Reg(sqBoro);

            var sqNube = new QuestDef
            {
                Id = "sq_nube", Title = "El nombre de la nube", Kind = QuestKind.Secundaria, CompletionXp = 50,
                Summary = "Bruma y Copo llevan días discutiendo cómo se llama una nube. Quizá un humano morado pueda ayudar.",
            };
            sqNube.Steps.Add(Step("observar", "Observa la nube con el catalejo ({Catalejo}) desde la Colina de las Nubes.", s => s.Has("nube_observada") || s.Has("nube_nombrada"), 20, "loc:loc_colina",
                "La nube flota sobre la Colina de las Nubes.", "Mantén {Catalejo} y mira la nube un momento.", "Desde lo alto de la colina, apunta el catalejo hacia la nube grande con la panza gris."));
            sqNube.Steps.Add(Step("proponer", "Propón un nombre a Bruma y Copo.", s => s.Has("nube_nombrada"), 0, "npc:bruma",
                "Bruma y Copo miran el cielo por la tarde en la colina.", "Habla con cualquiera de los dos.", "Habla con Bruma o con Copo y elige un nombre."));
            Reg(sqNube);

            var sqTome = new QuestDef
            {
                Id = "sq_tome", Title = "El taller de Tomé", Kind = QuestKind.Secundaria, CompletionXp = 40,
                Summary = "Tomé te enseñará a usar la mesa de trabajo si le traes materiales.",
            };
            var mats = Step("materiales", "Lleva a Tomé tres fibras de junco y una resina.", s => s.Has("receta_basica"), 20, "npc:tome",
                "Lía sabe dónde crecen los juncos.",
                "Los juncos crecen en los bordes de los campos de cultivo; la resina, en el tronco del Árbol de los Mayores.",
                "Recoge juncos junto a los campos (noroeste de la plaza) y resina en el Árbol de los Mayores; luego habla con Tomé.");
            mats.Detail = s => $"Fibra {Math.Min(3, s.Inventory.Count("fibra"))}/3 · Resina {Math.Min(1, s.Inventory.Count("resina"))}/1";
            sqTome.Steps.Add(mats);
            sqTome.Steps.Add(Step("fabricar", "Fabrica algo en una mesa de trabajo.", s => s.Has("primer_crafteo"), 20, "loc:loc_reverso",
                "La mesa está en el taller de Tomé.", "Interactúa con la mesa de trabajo.", "Usa la mesa del taller y elige una receta para la que tengas materiales."));
            Reg(sqTome);
        }
    }
}
