namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private static void E(string id, NotebookCategory cat, string title, string text, string known = null, int xp = 8)
        {
            Reg(new NotebookEntryDef { Id = id, Category = cat, Title = title, Text = text, KnownText = known, Xp = xp });
        }

        private static void AddNotebook()
        {
            // ------------------------------------------------ Personajes
            E("pj_nilo", NotebookCategory.Personajes, "Nilo", "Yo. Dieciséis años, más o menos. Pelo negro azabache imposible de peinar, ojos azul claro (los de mamá, dice Gina) y la túnica morada que me regaló la abuela. Busco a mis padres.", null, 0);
            E("pj_gina", NotebookCategory.Personajes, "Abuela Gina", "Mi abuela. Me regaló la túnica morada para que me encontraran si me perdía. Sabe las historias pequeñas que no salen en ningún archivo: los consejos, las costumbres, los remedios.", null, 0);
            E("pj_eira", NotebookCategory.Personajes, "Eira", "Mi madre. Estudiaba las Cápsulas. Desapareció hace dos inviernos junto a papá.", "Mi madre. Estudiaba las Cápsulas. Eligió ir a la Ciudad de las Mil Cápsulas con un Custodio para ver los archivos con sus propios ojos.");
            E("pj_daren", NotebookCategory.Personajes, "Daren", "Mi padre. Nació en el Reverso. Tallista. Desapareció con mamá mientras investigaban la Cápsula sellada.", "Mi padre. Fue con mamá por voluntad propia y dejó grabado que volverían por mí.");
            E("pj_oren", NotebookCategory.Personajes, "Anciano Oren", "Guardián de la memoria oral de la tribu. Conoce las canciones de la migración y cree que el silencio protege.");
            E("pj_haro", NotebookCategory.Personajes, "Haro", "Guardián del puente. Brusco, leal y mejor maestro de lo que parece.");
            E("pj_lia", NotebookCategory.Personajes, "Lía", "Recolectora. Sabe dónde crece cada hierba y cada junco del territorio.");
            E("pj_tome", NotebookCategory.Personajes, "Tomé", "Artesano del taller. Si algo se rompe, lo arregla; si no se rompe, lo mejora.");
            E("pj_suri", NotebookCategory.Personajes, "Suri", "Cocinera de la tribu y abuela de Kiko. Intercambia comida por materiales.");
            E("pj_kiko", NotebookCategory.Personajes, "Kiko", "Un niño que corre por todas partes y lo sabe todo de todos.");
            E("pj_boro", NotebookCategory.Personajes, "Boro", "Kolotapir viajero y coleccionista de objetos aparentemente inútiles. Encuentra valor en lo que otros descartan.", "Kolotapir viajero y coleccionista. Renunció voluntariamente al recuerdo del rostro de su hermana para protegerla de la Liga.");
            E("pj_bruma", NotebookCategory.Personajes, "Bruma", "Kolotapir exploradora. Está convencida de que una nube se llama Algodón Pensativo.");
            E("pj_copo", NotebookCategory.Personajes, "Copo", "Kolotapir artesano. Está convencido de que esa misma nube se llama Señora Lluvia.");
            E("pj_miga", NotebookCategory.Personajes, "Miga", "Una criatura diminuta, como una polilla de papel negra. Surgió de los fragmentos descartados junto a la Cápsula sellada y no se separa de mí. Le atraen los restos de recuerdos.");
            E("pj_custodio_gris", NotebookCategory.Personajes, "El Custodio de capa gris", "Una figura alta que aparece en los recuerdos de mis padres. Habla en nombre de la Liga.");
            E("pj_guardian", NotebookCategory.Personajes, "El Guardián de la Parada", "Un eco con forma de kolotapir anciano. Prometió cuidar la Parada hasta que volvieran los viajeros.");

            // ------------------------------------------------ Culturas
            E("cul_reverso", NotebookCategory.Culturas, "La Tribu del Reverso", "Una comunidad humana aislada del resto de Orom. Asentada desde hace generaciones, con hogares, campos, talleres y un fuego comunal. Valora la memoria oral, la autosuficiencia y la prudencia ante las Cápsulas. El aislamiento protege... y también esconde.");
            E("cul_migracion", NotebookCategory.Culturas, "La migración ancestral", "Antes de asentarse, los antepasados del Reverso fueron nómadas. Cada parada del viaje tiene una piedra en el círculo de la migración. Las canciones dicen que caminaban de noche 'para que el cielo no los viera'.", "Los antepasados paraban en lugares como la Parada Antigua de los Prados y conocían las Cápsulas antes de huir de ellas.");
            E("cul_gran_vigilia", NotebookCategory.Culturas, "La Gran Vigilia (rumor)", "Según Oren, hubo un tiempo en que 'el cielo hablaba': quizá los sueños y recuerdos de unos se filtraban en otros. Las canciones no lo explican; lo recuerdan.");
            E("cul_capsulas", NotebookCategory.Culturas, "Las Cápsulas", "Estructuras orgánicas que conservan sueños, recuerdos y experiencias posibles. Pueden estar estables, inactivas, dañadas, selladas o anómalas. No dicen automáticamente la verdad: guardan fragmentos y perspectivas de quienes las usaron.");
            E("cul_liga", NotebookCategory.Culturas, "La Liga de las Cápsulas", "Instituciones que gestionan las Cápsulas: acceso, regulación y control del relato histórico. En el Reverso, su nombre casi no se pronuncia.");
            E("cul_custodios", NotebookCategory.Culturas, "Los Custodios", "Personas encargadas de preservar y administrar las Cápsulas. Uno de ellos acompañaba a mis padres.");
            E("cul_errantes", NotebookCategory.Culturas, "Los Errantes", "Viajeros que viven al margen de las instituciones. Boro es uno de ellos: comercia, recoge y recuerda a su manera.");
            E("cul_kolotapires", NotebookCategory.Culturas, "Los Kolotapires", "Una especie inteligente de tapires humanoides, de 1,8 a 2,2 metros. Tienen lenguaje, humor, oficios y culturas regionales. No son mascotas ni enemigos: son vecinos. Sus orejas y su hocico dicen lo que sienten antes que sus palabras.");
            E("cul_orden_memoria", NotebookCategory.Culturas, "El orden kolotapir de la memoria", "Los kolotapires guardan los recuerdos en un orden: primero el agua, luego la raíz, después la piedra y al final el cielo. Las Cápsulas antiguas solo entienden ese orden.");
            E("obj_tunica", NotebookCategory.Objetos, "La túnica morada", "Regalo de la abuela Gina. Me da suerte, y no pienso quitármela. Cualquier armadura que lleve tendrá que ir encima.", null, 0);

            // ------------------------------------------------ Bestiario
            E("bes_corzo", NotebookCategory.Bestiario, "Corzo de bruma", "Herbívoro de los Prados que se mueve en manada. Huye si corres cerca o haces ruido. Al amanecer cruza los Prados hacia el agua.", "Herbívoro pacífico. Respuesta conocida: acercarse despacio y agachado.");
            E("bes_jabali", NotebookCategory.Bestiario, "Jabalí de musgo", "Defiende su territorio: resopla y escarba antes de embestir. Si te alejas, se calma. Le encantan las bayas dulces.", "Señales: resoplido y pezuña. Respuestas: alejarse, ofrecer bayas ({UsarObjeto}) o, si no queda otra, bloquear su embestida.");
            E("bes_eco", NotebookCategory.Bestiario, "Eco errante", "Restos de recuerdos que vagan sin dueño. Atacan en grupo, reaccionan al ruido y a la Escucha de Resonancias. Brillan antes de lanzarse.", "Odian la luz del fuego: no se acercan a las hogueras. El Pulso de Resonancia los aturde.");
            E("bes_guardian", NotebookCategory.Bestiario, "Guardián de Ecos", "Un eco enorme atado a la Cápsula de la Parada. Sus golpes se anuncian: alza los brazos antes de aplastar el suelo y gira antes de barrer.", "No es un monstruo: es una promesa que nadie liberó.");
            E("bes_miga", NotebookCategory.Bestiario, "Miga", "Polilla de papel negra. Se alimenta (o eso parece) de fragmentos de recuerdos olvidados. Revolotea hacia ellos si le preguntas ({Pista}).");

            // ------------------------------------------------ Misterios
            Reg(new MysteryDef
            {
                Id = "mis_padres",
                Question = "¿Qué les pasó a Eira y Daren?",
                Evidence =
                {
                    new EvidenceDef { Id = "ev_huellas", Text = "Dos rastros salen de la hondonada hacia el puente. Uno se detuvo, volvió atrás y luego siguió." },
                    new EvidenceDef { Id = "ev_pagina", Text = "Eira escribió: «La Cápsula no guarda el pasado: guarda lo que alguien quiso conservar». Hay un mapa hacia el noreste." },
                    new EvidenceDef { Id = "ev_gubia", Text = "Daren dejó su gubia junto a una talla sin terminar. Nunca se separaba de ella." },
                    new EvidenceDef { Id = "ev_gina_gubia", Text = "Gina: Daren solo dejaría su gubia si pensaba volver enseguida.", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_recuerdo_daren", Text = "Recuerdo en el taller: deciden ir juntos «porque nadie sabrá lo que el Custodio está haciendo» y volver antes del deshielo." },
                    new EvidenceDef { Id = "ev_recuerdo_custodio", Text = "Recuerdo contradictorio en la Cápsula: «Los dos vendrán con nosotros. La Liga lo ha decidido».", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_tercer_rastro", Text = "En los Prados, un tercer rastro de pasos largos acompaña al de mis padres." },
                    new EvidenceDef { Id = "ev_boro_testimonio", Text = "Boro vio a dos humanos y a un Custodio de capa gris camino de la Parada. Ella reía. No parecían prisioneros." },
                    new EvidenceDef { Id = "ev_talla_volveremos", Text = "En el refugio de la Parada, Daren grabó: «Para Nilo. Volveremos por él»." , Certainty = Knowledge.Sabe },
                    new EvidenceDef { Id = "ev_recuerdo_completo", Text = "El recuerdo completo de la Parada: el Custodio dijo que la Liga lo había decidido y Eira respondió que irían porque querían ver los archivos con sus propios ojos.", Certainty = Knowledge.Sabe },
                },
                Hypotheses =
                {
                    new HypothesisDef { Id = "h_voluntad", Text = "Se marcharon por voluntad propia, siguiendo su investigación.", SupportedBy = new[] { "ev_recuerdo_daren", "ev_huellas" } },
                    new HypothesisDef { Id = "h_secuestro", Text = "Los Custodios de la Liga se los llevaron a la fuerza.", SupportedBy = new[] { "ev_recuerdo_custodio", "ev_gubia" }, RefutedBy = new[] { "ev_boro_testimonio" } },
                    new HypothesisDef { Id = "h_ambas", Text = "Decidieron ir, pero alguien los estaba esperando y presionando.", SupportedBy = new[] { "ev_tercer_rastro", "ev_boro_testimonio", "ev_recuerdo_completo" } },
                    new HypothesisDef { Id = "h_tribu", Text = "La propia tribu les obligó a marcharse.", SupportedBy = new string[0], RefutedBy = new[] { "ev_recuerdo_daren" } },
                },
                TrueHypothesis = "h_ambas",
                ResolvingEvidence = "ev_recuerdo_completo",
                Resolution = "Eira y Daren eligieron ir a la Ciudad de las Mil Cápsulas con un Custodio de la Liga. Sabían que la Liga los esperaba, pero fueron por decisión propia, para ver los archivos con sus propios ojos. Daren dejó grabado que volverían por mí. Su destino todavía no está confirmado.",
                Xp = 60,
            });

            Reg(new MysteryDef
            {
                Id = "mis_reverso",
                Question = "¿Qué oculta la historia del Reverso?",
                Evidence =
                {
                    new EvidenceDef { Id = "ev_piedras_huecos", Text = "En el círculo de la migración faltan piedras: «paradas que decidimos olvidar», según Oren." },
                    new EvidenceDef { Id = "ev_piedras_canto", Text = "La canción de la migración habla de «casas que recuerdan» que los antepasados dejaron atrás.", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_sello", Text = "El sello de la Cápsula se abrió desde fuera y alguien lo volvió a cerrar después, con cuidado." },
                    new EvidenceDef { Id = "ev_sello_canto", Text = "Quien volvió a sellar la Cápsula cantaba una canción de la migración.", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_oren_resello", Text = "Oren volvió a sellar la Cápsula la mañana en que mis padres se fueron, y lo calló.", Certainty = Knowledge.Sabe },
                    new EvidenceDef { Id = "ev_arbol_profundo", Text = "Bajo el Árbol de los Mayores, los antepasados enterraron una semilla de Cápsula: «Que nadie recuerde lo que fuimos... hasta que haga falta».", Certainty = Knowledge.Sabe },
                    new EvidenceDef { Id = "ev_cancion_compartida", Text = "En los Prados, un eco canta la melodía de la migración del Reverso en lengua kolotapir." },
                    new EvidenceDef { Id = "ev_guardian_viajeros", Text = "El Guardián de la Parada recuerda a «los viajeros del Reverso» que le dejaron sus Cápsulas." },
                    new EvidenceDef { Id = "ev_marcas_parada", Text = "En la Parada Antigua hay marcas del Reverso junto a símbolos de Cápsulas: los antepasados paraban allí y las usaban.", Certainty = Knowledge.Sabe },
                },
                Hypotheses =
                {
                    new HypothesisDef { Id = "hr_huida", Text = "Huyeron de las Cápsulas por miedo y nunca las usaron.", SupportedBy = new[] { "ev_piedras_huecos" }, RefutedBy = new[] { "ev_guardian_viajeros", "ev_marcas_parada" } },
                    new HypothesisDef { Id = "hr_usaban", Text = "Usaron Cápsulas antes de huir y borraron esa parte de su historia.", SupportedBy = new[] { "ev_piedras_canto", "ev_arbol_profundo", "ev_cancion_compartida", "ev_marcas_parada" } },
                    new HypothesisDef { Id = "hr_nada", Text = "No hay nada oculto: son solo canciones viejas.", RefutedBy = new[] { "ev_oren_resello", "ev_piedras_huecos" } },
                },
                TrueHypothesis = "hr_usaban",
                ResolvingEvidence = "ev_marcas_parada",
                Resolution = "Los antepasados del Reverso conocían y usaban las Cápsulas. La Parada Antigua fue una de sus paradas. Eligieron aislarse y borrar esa parte de su historia para proteger a sus descendientes: la tradición protegió, pero el silencio también impidió comprender.",
                Xp = 50,
            });

            Reg(new MysteryDef
            {
                Id = "mis_capsula",
                Question = "¿Por qué zumba la Cápsula sellada?",
                Evidence =
                {
                    new EvidenceDef { Id = "ev_zumbido", Text = "Gina: la Cápsula zumbó anoche igual que la noche en que mis padres se fueron.", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_capsula_anomala", Text = "La Cápsula está anómala: su resonancia es irregular y mezcla experiencias que no deberían mezclarse." },
                    new EvidenceDef { Id = "ev_conexion_parada", Text = "La voz del Custodio que oí en la hondonada pertenece al recuerdo de la Parada: las dos Cápsulas están conectadas.", Certainty = Knowledge.Sabe },
                },
                Hypotheses =
                {
                    new HypothesisDef { Id = "hc_mensaje", Text = "Mis padres intentan enviarme un mensaje.", SupportedBy = new[] { "ev_zumbido" } },
                    new HypothesisDef { Id = "hc_conexion", Text = "Otra Cápsula, lejos de aquí, está despertando y arrastra a esta.", SupportedBy = new[] { "ev_capsula_anomala", "ev_conexion_parada" } },
                    new HypothesisDef { Id = "hc_tribu", Text = "Alguien de la tribu la ha manipulado.", SupportedBy = new[] { "ev_sello" }, RefutedBy = new[] { "ev_conexion_parada" } },
                },
                TrueHypothesis = "hc_conexion",
                ResolvingEvidence = "ev_conexion_parada",
                Resolution = "La Cápsula sellada y la de la Parada estaban conectadas. Al despertar la de la Parada, la del Reverso dejó escapar fragmentos de su recuerdo, sin contexto: por eso se contradecían.",
                Xp = 40,
            });

            Reg(new MysteryDef
            {
                Id = "mis_liga",
                Question = "¿Por qué la Liga esperaba a Eira y Daren?",
                Evidence =
                {
                    new EvidenceDef { Id = "ev_custodio_esperaba", Text = "El Custodio de capa gris les dijo: «Os esperábamos».", Certainty = Knowledge.Sabe },
                    new EvidenceDef { Id = "ev_caravanas", Text = "Bruma y Copo: las caravanas de la Liga recorren el Camino del Este con faroles azules.", Certainty = Knowledge.Sospecha },
                    new EvidenceDef { Id = "ev_primer_custodio", Text = "Un eco de caravana en los Prados: «Más deprisa. El Primer Custodio no espera».", Certainty = Knowledge.Sospecha },
                },
                Hypotheses =
                {
                    new HypothesisDef { Id = "hl_archivos", Text = "Querían lo que Eira sabía sobre las Cápsulas.", SupportedBy = new[] { "ev_custodio_esperaba" } },
                    new HypothesisDef { Id = "hl_reverso", Text = "Buscaban a alguien del Reverso: el pueblo que huyó.", SupportedBy = new[] { "ev_custodio_esperaba" } },
                    new HypothesisDef { Id = "hl_primer", Text = "Alguien muy poderoso en la Liga —el Primer Custodio— los mandó llamar.", SupportedBy = new[] { "ev_primer_custodio" } },
                },
                TrueHypothesis = null,
                ResolvingEvidence = null,
                Resolution = "Pendiente. Continúa en la Ciudad de las Mil Cápsulas.",
                Xp = 0,
            });
        }
    }
}
