namespace Kolotopir.Core
{
    public static partial class GameDb
    {
        private const string Boro = "Boro";
        private const string Bruma = "Bruma";
        private const string Copo = "Copo";

        private static void AddPradosDialogues()
        {
            // ================================================= BORO
            var b = new DialogueBuilder("boro_intro");
            b.Say("start", Boro, "¡Oh! Un humano. Y morado. Nunca había visto un humano morado.", Emotion.Sorprendido)
             .Enter(ctx => ctx.Command("encuadre:boro"))
             .Choice("Es una túnica.", "tunica")
             .Choice("Me llamo Nilo. Vengo del Reverso.", "reverso");
            b.Say("tunica", Boro, "¡Ah! Mucho mejor. Los humanos morados me ponían nervioso.", Emotion.Alegre).Next("presenta");
            b.Say("reverso", Boro, "¿Del Reverso? Creía que los del Reverso no salían nunca. Como las raíces.", Emotion.Curioso).Next("presenta");
            b.Say("presenta", Boro, "Soy Boro. Viajero, coleccionista y experto en objetos que nadie quiere. Una cuchara doblada, por ejemplo, es una cuchara que ha vivido.", Emotion.Alegre)
             .Enter(ctx =>
             {
                 ctx.SetFlag("boro_conocido");
                 ctx.AddNotebook("pj_boro", Knowledge.Observado);
                 ctx.AddNotebook("cul_kolotapires", Knowledge.Observado);
                 ctx.AddNotebook("cul_errantes", Knowledge.Sospecha);
             })
             .Choice("Busco a dos humanos, Eira y Daren. Pasaron por aquí hace dos inviernos.", "busca")
             .Choice("¿Objetos que nadie quiere?", "objetos");
            b.Say("busca", Boro, "Dos inviernos... Mi memoria es como mi carro: está llena, pero no siempre encuentro lo que busco.", Emotion.Triste).Next("trato");
            b.Say("objetos", Boro, "¡Exacto! Me faltan tres para completar mi colección de este invierno: una cuchara doblada, un botón de nácar y una piedra con agujero. Lo inútil es lo que más cuesta encontrar.", Emotion.Alegre).Next("trato");
            b.Say("trato", Boro, "Te propongo algo: ayúdame con mi colección y yo rebusco en mi memoria. Los recuerdos, como las cucharas, aparecen cuando dejas de buscarlos.")
             .Enter(ctx => ctx.StartQuest("sq_boro")).End();
            Reg(b.Build());

            b = new DialogueBuilder("boro_testimonio");
            b.Say("start", Boro, "¿Has encontrado mis tesoros? ¿O has venido a mirar mi carro con esos ojos tan azules?", Emotion.Curioso);
            BoroTestimonyTopics(b);
            b.Say("pagina", Boro, "Esta letra... ¡la conozco! Una mujer me cambió un catalejo roto por una canción, y lo apuntaba todo con esta letra apretada.", Emotion.Sorprendido)
             .Enter(ctx => ctx.ChangeAffinity("boro", 5, "compartio_pruebas")).Next("cuenta");
            b.Say("amistad", Boro, "Hmm. Me lo pides con los ojos de alguien que ha perdido algo. Conozco esos ojos.", Emotion.Triste).Next("cuenta");
            b.Say("gracias", Boro, "Me has devuelto tres cosas. Te debo al menos un recuerdo.", Emotion.Alegre).Next("cuenta");
            b.Say("cuenta", Boro, "Los vi. Dos humanos y una figura alta con capa gris: un Custodio. Iban hacia la Parada Antigua, al noreste. Ella reía. Él cargaba con todo. No parecían prisioneros... pero el Custodio no les quitaba ojo.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.SetFlag("boro_testimonio");
                 ctx.AddEvidence("ev_boro_testimonio");
                 ctx.AddNotebook("cul_custodios", Knowledge.Observado);
                 ctx.AddNotebook("loc_parada", Knowledge.Sospecha);
             });
            b.Say("cuenta2", Boro, "Desde entonces la Parada está llena de ecos, y hay un guardián que no deja pasar a nadie. Si vas, ve con cuidado.", Emotion.Preocupado);
            b.Say("orden", Boro, "Ah, y cuando estés allí, mira la inscripción del arco de piedra. Los kolotapires guardamos los recuerdos en orden: primero el agua, luego la raíz, después la piedra y al final el cielo. Las Cápsulas viejas solo entienden ese orden.")
             .Enter(ctx => { ctx.SetFlag("conoce_orden_kolotapir"); ctx.AddNotebook("cul_orden_memoria", Knowledge.Sabe); }).End();
            b.Say("trueque", Boro, "¡Siempre! A ver qué inutilidades traes.").Enter(ctx => ctx.Command("open_barter:boro")).End();
            b.Say("adios", Boro, "Aquí estaré. O por ahí. Los Errantes nunca estamos del todo en un sitio.").End();
            Reg(b.Build());

            b = new DialogueBuilder("boro_entrega");
            b.Say("start", Boro, "¡Una cuchara doblada! ¡Un botón de nácar! ¡Y una piedra con agujero! Nilo, eres el humano más inútilmente útil que conozco.", Emotion.Alegre)
             .Enter(ctx =>
             {
                 ctx.TakeItem("cuchara_doblada", 1);
                 ctx.TakeItem("boton_nacar", 1);
                 ctx.TakeItem("piedra_agujero", 1);
                 ctx.SetFlag("boro_coleccion_completa");
                 ctx.ChangeAffinity("boro", 20, "ayudo_coleccion");
                 ctx.GiveItem("boton_boro", 1);
                 ctx.GiveItem("tonico_bruma", 2);
             });
            b.Say("b2", Boro, "Toma: mi botón de la suerte. Ve cosas pequeñas. Y dos tónicos, que te veo flaco.");
            b.Say("b3", Boro, "Te contaré un secreto, ya que me has devuelto tres cosas. Hace tiempo renuncié a un recuerdo: el rostro de mi hermana. Lo guardé en esta cuenta para que la Liga no pudiera encontrarla a través de mí.", Emotion.Triste);
            b.Say("b4", Boro, "No he vuelto a mirarlo. Si quieres... puedes escucharlo. Te doy permiso. A veces una memoria pesa menos si otro la sostiene un rato.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.SetFlag("boro_ofrece_cuenta");
                 ctx.SetFlag("consent_boro_cuenta");
                 ctx.GiveItem("cuenta_boro", 1);
             })
             .Choice("Gracias por confiar en mí. La escucharé con cuidado.", "acepta")
             .Choice("Es tuya, Boro. No me corresponde escucharla.", "rechaza")
             .Choice("Boro... ya la escuché. En tu carro. Sin pedirte permiso.", "confiesa", s => s.Has("boro_cuenta_sin_permiso"));
            b.Say("acepta", Boro, "Escúchala cuando quieras: desde el inventario. Y luego cuéntame si sonreía. Yo ya no me acuerdo.", Emotion.Triste)
             .Enter(ctx => ctx.AddNotebook("pj_boro", Knowledge.Sabe)).Next("final");
            b.Say("rechaza", Boro, "Hmm. Nadie había rechazado nunca uno de mis tesoros. Quédatela de todos modos: quizá algún día sepas qué hacer con ella.", Emotion.Sorprendido)
             .Enter(ctx =>
             {
                 ctx.ChangeAffinity("boro", 10, "respeto_memoria");
                 ctx.SetFlag("boro_cuenta_rechazada");
                 ctx.RecordDecision("dec_boro_cuenta", "No escuchada", "Respetaste el recuerdo de Boro aunque te dio permiso.", ConsequenceScale.Local);
                 ctx.AddNotebook("pj_boro", Knowledge.Sabe);
             }).Next("final");
            b.Say("confiesa", Boro, "...Lo sé. Lo noté: huele a humano. Gracias por decírmelo tú. No se puede des-escuchar algo, pero se puede pedir perdón.", Emotion.Triste)
             .Enter(ctx => { ctx.ChangeAffinity("boro", 15, "pidio_perdon"); ctx.SetFlag("boro_dolido_visto"); }).Next("final");
            b.Say("final", Boro, "Bueno. Basta de cosas tristes. ¿Comerciamos?")
             .Choice("Veamos qué tienes.", "trueque")
             .Choice("Otro día.", "adios");
            b.Say("trueque", Boro, "¡A ver qué inutilidades traes!").Enter(ctx => ctx.Command("open_barter:boro")).End();
            b.Say("adios", Boro, "Hmmf. Hasta otro día, humano morado.").End();
            Reg(b.Build());

            b = new DialogueBuilder("boro_dolido");
            b.Say("start", Boro, "Escuchaste mi cuenta. Lo noto: huele a humano. No te lo pedí, Nilo.", Emotion.Enfadado)
             .Enter(ctx => ctx.SetFlag("boro_dolido_visto"))
             .Choice("Lo siento. No debí hacerlo.", "perdon")
             .Choice("Solo quería ayudar.", "excusa");
            b.Say("perdon", Boro, "...Lo sé. Pero no se puede des-escuchar algo. Tendré que acostumbrarme a que lo sepas.", Emotion.Triste)
             .Enter(ctx => ctx.ChangeAffinity("boro", 8, "pidio_perdon")).End();
            b.Say("excusa", Boro, "Ayudar no es lo mismo que decidir por otro.", Emotion.Enfadado)
             .Enter(ctx => ctx.ChangeAffinity("boro", -5, "se_justifico")).End();
            Reg(b.Build());

            b = new DialogueBuilder("boro_general");
            b.Say("start", Boro, "¡Nilo! ¿Comerciamos? Doy cosas útiles por cosas inútiles. Es un buen negocio... para los dos.", Emotion.Alegre);
            BoroTopics(b);
            b.Say("mas", Boro, "¿Algo más?");
            BoroTopics(b);
            b.Say("trueque", Boro, "¡A ver qué inutilidades traes!").Enter(ctx => ctx.Command("open_barter:boro")).End();
            b.Say("banco", Boro, "Mi banco es tu banco. Literalmente: me lo encontré.").Enter(ctx => ctx.Command("open_crafting:mesa")).End();
            b.Say("kolo", Boro, "En las ciudades, cada colonia tiene sus costumbres, sus ropas y sus manías. Aquí, en los Prados, nos gusta discutir con las nubes. ¿No conoces a Bruma y Copo? Al sureste, en la colina.")
             .Enter(ctx => { ctx.AddNotebook("cul_kolotapires", Knowledge.Sabe); ctx.AddNotebook("loc_colina", Knowledge.Sospecha); }).Next("mas");
            b.Say("capsulas", Boro, "Casas. Casas donde vive lo que pasó. Algunas tienen la puerta cerrada por buenos motivos... y otras porque alguien perdió la llave a propósito.", Emotion.Curioso).Next("mas");
            b.Say("liga", Boro, "La Liga ordena, archiva y decide qué se recuerda. Los Errantes vivimos al margen: no les gustamos, porque recordamos a nuestra manera.")
             .Enter(ctx => { ctx.AddNotebook("cul_errantes", Knowledge.Sabe); ctx.AddNotebook("cul_liga", Knowledge.Observado); }).Next("mas");
            b.Say("adios", Boro, "Hasta luego, humano morado.").End();
            Reg(b.Build());

            // ================================================= BRUMA Y COPO — "El nombre de la nube"
            b = new DialogueBuilder("nube_intro");
            b.Say("start", Bruma, "¡Es Algodón Pensativo! ¡Mira cómo se queda quieta, pensando!", Emotion.Enfadado);
            b.Say("c2", Copo, "¡Es la Señora Lluvia! Tiene la tripa gris de las nubes que van a llover.", Emotion.Enfadado);
            b.Say("c3", Bruma, "Las nubes no tienen tripa, Copo.");
            b.Say("c4", Copo, "¡Esta sí!", Emotion.Enfadado);
            b.Say("c5", Bruma, "¡Un humano! Morado, pero humano. Tú decides: ¿cómo se llama esa nube?", Emotion.Sorprendido)
             .Enter(ctx => { ctx.AddNotebook("pj_bruma", Knowledge.Observado); ctx.AddNotebook("pj_copo", Knowledge.Observado); ctx.AddNotebook("cul_kolotapires", Knowledge.Observado); });
            CloudChoices(b, true);
            b.Say("menu", Bruma, "Bueno, ¿qué? ¿Cómo se llama?");
            CloudChoices(b, false);
            b.Say("importa", Copo, "Porque lo que tiene nombre se recuerda. Y lo que se recuerda... a veces vuelve.", Emotion.Neutral);
            b.Say("importa2", Bruma, "Copo, no te pongas profundo, que me mareo.", Emotion.Alegre).Next("menu");
            b.Say("observar", Bruma, "¡Un humano científico! Usa ese tubo que llevas ({Catalejo}) y mírala bien desde lo alto de la colina.", Emotion.Alegre)
             .Enter(ctx => ctx.StartQuest("sq_nube"));
            b.Say("observar2", Copo, "Y fíjate en la tripa.").End();
            AddCloudVerdicts(b);
            Reg(b.Build());

            b = new DialogueBuilder("nube_esperando");
            b.Say("start", Copo, "¿Ya la has mirado con el tubo? Desde lo alto de la colina se le ve mejor la tripa.").End();
            Reg(b.Build());

            b = new DialogueBuilder("nube_propuesta");
            b.Say("start", Bruma, "¿Y bien, humano científico? ¿Qué has visto?", Emotion.Curioso)
             .Choice("Tiene forma de kolotapir dormido con la panza gris. ¿Y si se llama «Tapir que Sueña Lluvia»?", "compromiso")
             .Choice("Algodón Pensativo.", "elige_bruma")
             .Choice("Señora Lluvia.", "elige_copo")
             .Choice("Todavía estoy pensando.", "pensando");
            b.Say("compromiso", Bruma, "...Es ridículo.", Emotion.Sorprendido);
            b.Say("compromiso2", Copo, "Es perfecto.", Emotion.Alegre);
            b.Say("compromiso3", Bruma, "Es perfectamente ridículo. Me encanta.", Emotion.Alegre)
             .Enter(ctx =>
             {
                 ctx.SetFlag("nube_nombrada");
                 ctx.SetFlag("nube_compromiso");
                 ctx.ChangeAffinity("bruma", 15, "nombre_compartido");
                 ctx.ChangeAffinity("copo", 15, "nombre_compartido");
                 ctx.RecordDecision("dec_nube", "Tapir que Sueña Lluvia", "Propusiste un nombre que une las dos ideas, basado en lo que observaste. Bruma y Copo están encantados.", ConsequenceScale.Local);
             });
            b.Say("compromiso4", Copo, "Toma, por resolver la Gran Discusión de este mes. Y un consejo: no te acerques al Camino del Este cuando pasen las caravanas de la Liga. Llevan faroles azules y no saludan.")
             .Enter(ctx => { ctx.GiveItem("tonico_bruma", 1); ctx.GiveItem("estofado", 1); ctx.AddEvidence("ev_caravanas"); }).End();
            b.Say("pensando", Copo, "Piensa rápido, que se deshace.").End();
            AddCloudVerdicts(b);
            Reg(b.Build());

            b = new DialogueBuilder("nube_post");
            b.Say("start", Copo, "Ahora parece una tetera. No se lo digas a Bruma.", Emotion.Alegre)
             .Choice("¿Habéis visto pasar caravanas por aquí?", "caravanas", s => !s.HasEvidence("ev_caravanas"))
             .Choice("Hasta pronto.", "adios");
            b.Say("caravanas", Bruma, "Las de la Liga, con faroles azules, por el Camino del Este. No saludan.")
             .Enter(ctx => ctx.AddEvidence("ev_caravanas")).End();
            b.Say("adios", Bruma, "¡Vuelve cuando quieras discutir de algo!").End();
            Reg(b.Build());
        }

        private static void CloudChoices(DialogueBuilder b, bool withWhy)
        {
            b.Choice("Algodón Pensativo.", "elige_bruma")
             .Choice("Señora Lluvia.", "elige_copo")
             .Choice("Dejadme observarla con calma antes de decidir.", "observar");
            if (withWhy) b.Choice("¿Por qué es tan importante el nombre?", "importa");
        }

        private static void AddCloudVerdicts(DialogueBuilder b)
        {
            b.Say("elige_bruma", Bruma, "¡Ja! ¡Lo sabía!", Emotion.Alegre);
            b.Say("elige_bruma2", Copo, "Traidor morado.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.StartQuest("sq_nube");
                 ctx.SetFlag("nube_nombrada");
                 ctx.ChangeAffinity("bruma", 10, "le_dio_la_razon");
                 ctx.ChangeAffinity("copo", -5, "no_le_dio_la_razon");
                 ctx.RecordDecision("dec_nube", "Algodón Pensativo", "Le diste la razón a Bruma. Copo no está contento.", ConsequenceScale.Local);
                 ctx.GiveItem("bayas", 3);
             }).End();
            b.Say("elige_copo", Copo, "¡Lo ves! ¡Tripa!", Emotion.Alegre);
            b.Say("elige_copo2", Bruma, "Humano con poco gusto.", Emotion.Triste)
             .Enter(ctx =>
             {
                 ctx.StartQuest("sq_nube");
                 ctx.SetFlag("nube_nombrada");
                 ctx.ChangeAffinity("copo", 10, "le_dio_la_razon");
                 ctx.ChangeAffinity("bruma", -5, "no_le_dio_la_razon");
                 ctx.RecordDecision("dec_nube", "Señora Lluvia", "Le diste la razón a Copo. Bruma no está contenta.", ConsequenceScale.Local);
                 ctx.GiveItem("bayas", 3);
             }).End();
        }

        private static void BoroTestimonyTopics(DialogueBuilder b)
        {
            b.Choice("Mira esta página. Es la letra de mi madre.", "pagina", s => s.HasItem("pagina_eira"))
             .Choice("Ya tienes tu colección completa.", "gracias", s => s.Has("boro_coleccion_completa"))
             .Choice("Por favor, Boro. Es todo lo que tengo.", "amistad", s => s.Affinity("boro") >= 15, null, "Boro todavía no te conoce lo bastante (afinidad 15).")
             .Choice("¿Comerciamos?", "trueque")
             .Choice("Volveré.", "adios");
        }

        private static void BoroTopics(DialogueBuilder b)
        {
            b.Choice("Veamos qué tienes.", "trueque")
             .Choice("¿Puedo usar tu banco de trabajo?", "banco", s => s.Has("receta_basica"), null, "No sabes usar una mesa de trabajo (Tomé puede enseñarte).")
             .Choice("Háblame de los kolotapires.", "kolo")
             .Choice("¿Qué son las Cápsulas para ti?", "capsulas")
             .Choice("¿Sabes algo de la Liga?", "liga")
             .Choice("Hasta luego, Boro.", "adios");
        }

        private static void AddExamines()
        {
            Reg(new ExamineDef
            {
                Id = "ex_pagina", Title = "Mesa del taller", Prompt = "Examinar la mesa", Pos = WorldLayout.HutTable, YOffset = 0.85f, Visual = "papel",
                Text = "Papeles, tinta seca y una página arrancada del cuaderno de mamá. Su letra apretada: «La Cápsula no guarda el pasado: guarda lo que alguien quiso conservar». Debajo, un mapa a medio dibujar hacia el noreste.",
                Active = s => !s.Has("pista_pagina"), ConsumeOnce = true,
                OnExamine = ctx => { ctx.GiveItem("pagina_eira", 1); ctx.SetFlag("pista_pagina"); ctx.AddEvidence("ev_pagina"); ctx.AddNotebook("loc_taller", Knowledge.Observado); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_gubia", Title = "Estantería", Prompt = "Examinar la estantería", Pos = WorldLayout.HutShelf, YOffset = 1.0f, Visual = "gubia",
                Text = "Herramientas ordenadas con cuidado. Entre ellas, la gubia de papá, junto a un tapir de madera a medio tallar. Él nunca se separaba de ella.",
                Active = s => !s.Has("pista_gubia"), ConsumeOnce = true,
                OnExamine = ctx => { ctx.GiveItem("gubia_daren", 1); ctx.SetFlag("pista_gubia"); ctx.AddEvidence("ev_gubia"); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_piedras", Title = "Piedras de la Migración", Pos = WorldLayout.MigrationStones, YOffset = 1.2f,
                Text = "Piedras en círculo, cada una con el símbolo de una parada del viaje. Entre algunas hay huecos donde la hierba crece distinta: allí hubo piedras que alguien quitó.",
                OnExamine = ctx => { ctx.AddNotebook("cul_migracion", Knowledge.Observado); ctx.AddEvidence("ev_piedras_huecos"); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_marcas_parada", Title = "Columna marcada", Pos = WorldLayout.RuinPillar(WorldLayout.MarkedPillar), YOffset = 1.4f, Visual = "marca",
                Text = "Junto a los símbolos kolotapir de las Cápsulas hay otra marca, más tosca: el rombo del Reverso. El mismo de tu amuleto. Tus antepasados estuvieron aquí... y usaban Cápsulas.",
                OnExamine = ctx => { ctx.AddEvidence("ev_marcas_parada"); ctx.AddNotebook("cul_migracion", Knowledge.Sabe); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_inscripcion_arco", Title = "Inscripción del arco", Pos = WorldLayout.ArchInscription, YOffset = 1.0f, Visual = "marca",
                Text = "Letras kolotapir talladas en la base. Cuatro símbolos se repiten siempre en el mismo orden: una onda (agua), unas raíces, una piedra y un círculo abierto (cielo).",
                OnExamine = ctx => { ctx.SetFlag("conoce_orden_kolotapir"); ctx.AddNotebook("cul_orden_memoria", Knowledge.Observado); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_letrero_este", Title = "Poste del Camino del Este", Pos = new P2(WorldLayout.EastGate.X - 3f, WorldLayout.EastGate.Z + 4f), YOffset = 1.2f,
                Text = "Un poste de piedra con una flecha y el símbolo de la Liga. Más allá, el puente natural se pierde en la bruma, hacia las cúpulas de la Ciudad de las Mil Cápsulas.",
                OnExamine = ctx => ctx.AddNotebook("vista_ciudad", Knowledge.Observado),
            });
            Reg(new ExamineDef
            {
                Id = "ex_talla", Title = "Grabado en la pared", Pos = WorldLayout.ShelterCarving, YOffset = 1.4f, Visual = "marca",
                Text = "Unas letras rayadas en la piedra, casi borradas. Con lo que ahora sabes, se leen: «PARA NILO. VOLVEREMOS POR ÉL». Es la letra de papá.",
                Active = s => s.Has("recuerdo_reconstruido"),
                OnExamine = ctx => ctx.AddEvidence("ev_talla_volveremos"),
            });
            Reg(new ExamineDef
            {
                Id = "ex_talla_borrosa", Title = "Grabado en la pared", Pos = WorldLayout.ShelterCarving, YOffset = 1.4f, Visual = "marca",
                Text = "Unas letras rayadas en la piedra, casi borradas. Distingues una «N»... y poco más.",
                Active = s => !s.Has("recuerdo_reconstruido"),
            });
            Reg(new ExamineDef
            {
                Id = "ex_pinturas", Title = "Pinturas de la gruta", Pos = WorldLayout.GrottoPainting, YOffset = 1.2f, Visual = "marca",
                Text = "Figuras pintadas con ceniza: gente que camina de noche bajo un cielo lleno de ojos. Una de ellas lleva una Cápsula a la espalda.",
                OnExamine = ctx => { ctx.AddNotebook("cul_gran_vigilia", Knowledge.Observado); ctx.AddNotebook("loc_gruta", Knowledge.Sabe); ctx.AddXp(25, "Descubrimiento"); },
            });
            Reg(new ExamineDef
            {
                Id = "ex_mojon", Title = "Mojón del puente", Pos = new P2(WorldLayout.Mojon.X - 1.5f, WorldLayout.Mojon.Z - 1.5f), YOffset = 1.0f,
                Text = "Piedras apiladas por viajeros: cada uno deja una. Hay una con el rombo del Reverso, muy gastada. Alguien de la tribu pasó por aquí hace mucho... o no tanto.",
                OnExamine = ctx => ctx.AddNotebook("loc_mojon", Knowledge.Sabe),
            });
        }
    }
}
